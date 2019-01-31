using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Browser;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using RendleLabs.MetaImage.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using Configuration = AngleSharp.Configuration;

namespace RendleLabs.MetaImage.Controllers
{
    [Route("image")]
    public class ImageController : Controller
    {
        private static readonly Regex MetaOgImageRegex = new Regex("<meta property=\"og:image\" content=\"(.*?)\".*?>");
        private static readonly Regex MetaTwitterImageRegex = new Regex("<meta property=\"twitter:image\" content=\"(.*?)\".*?>");
        private static readonly DiagnosticSource Diagnostics = new DiagnosticListener(typeof(ImageController).FullName);

        private readonly IHttpClientFactory _http;
        private readonly ILogger<ImageController> _logger;

        public ImageController(IHttpClientFactory http, ILogger<ImageController> logger)
        {
            _http = http;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery(Name = "u")] string uri, [FromQuery(Name = "w")] int? width,
            [FromQuery(Name = "h")] int? height)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return BadRequest();
            }

            var html = await GetHtmlAsync(uri);
            if (html == null) return NotFound();

            var metaImageUri = GetImageUri(uri, html);

            if (metaImageUri == null) return NotFound();

            _logger.LogInformation($"GET '{metaImageUri}'");
            HttpResponseMessage imageResponse;
            using (var http = _http.CreateClient())
            {
                imageResponse = await http.GetAsync(metaImageUri);
            }

            if (!imageResponse.IsSuccessStatusCode) return NotFound();

            try
            {
                var image = Image.Load(await imageResponse.Content.ReadAsStreamAsync());


                ResizeImage(image, width.GetValueOrDefault(500), height.GetValueOrDefault(375));

                var stream = EncodeImage(image);
                Response.RegisterForDispose(stream);

                var result = new FileStreamResult(stream, "image/jpeg");

                Response.Headers["Cache-Control"] = new CacheControlHeaderValue
                {
                    MaxAge = TimeSpan.FromDays(1),
                    MustRevalidate = false,
                    Private = false
                }.ToString();

                Diagnostics.IfEnabled("ImageResize")?.Write(new {
                    originalSize = imageResponse.Content.Headers.ContentLength.GetValueOrDefault(),
                    newSize = stream.Length
                });

                // if (Diagnostics.IsEnabled("ImageResize"))
                // {
                //     long originalSize = imageResponse.Content.Headers.ContentLength.GetValueOrDefault();
                //     long newSize = stream.Length;
                //     Diagnostics.Write("ImageResize", new {originalSize, newSize});
                // }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Resize failed for '{metaImageUri}'", ex);
                return NotFound();
            }
        }

        private static MemoryStream EncodeImage(Image<Rgba32> image)
        {
            var stream = new MemoryStream();
            var encoder = new JpegEncoder {Quality = 60};
            image.SaveAsJpeg(stream, encoder);
            stream.Position = 0;
            return stream;
        }

        private static void ResizeImage(Image<Rgba32> image, int width, int height)
        {
            var size = new Size(width, height);
            image.Mutate(context =>
            {
                context.Resize(new ResizeOptions
                    {
                        Size = size,
                        Mode = ResizeMode.Max,
                        Position = AnchorPositionMode.Center
                    })
                    .Resize(new ResizeOptions
                    {
                        Size = size,
                        Mode = ResizeMode.Crop,
                        Position = AnchorPositionMode.Center
                    });
            });
        }

        private string GetImageUri(string path, string html)
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);
            var metaImage = document.QuerySelector("head > meta[property='og:image']") ??
                            document.QuerySelector("head > meta[property='twitter:image']");

            string imageUrl;
            if (metaImage != null)
            {
                if (!string.IsNullOrWhiteSpace(imageUrl = metaImage?.GetAttribute("content")))
                {
                    return imageUrl;
                }
            }

            imageUrl = TryRegex(html, MetaOgImageRegex) ?? TryRegex(html, MetaTwitterImageRegex);

            if (imageUrl != null)
            {
                return imageUrl;
            }

            _logger.LogWarning($"No image meta tag found on '{path}'");
            return null;
        }

        private static string TryRegex(string html, Regex regex)
        {
            var match = regex.Match(html);
            if (match.Success && match.Groups.Count == 2)
            {
                var value = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private async Task<string> GetHtmlAsync(string uri)
        {
            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
            var document = await context.OpenAsync(uri);
            try
            {
                _logger.LogInformation($"Requesting '{uri}'");
                HttpResponseMessage response;
                using (var http = _http.CreateClient())
                {
                    response = await http.GetAsync(uri);
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                _logger.LogError($"No page found for '{uri}'");
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
    }
}