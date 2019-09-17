using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using RendleLabs.PointlessService.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using Configuration = AngleSharp.Configuration;

namespace RendleLabs.PointlessService.Controllers
{
    [Route("pointless")]
    public class PointlessController : Controller
    {
        private static readonly DiagnosticSource Diagnostics = new DiagnosticListener(typeof(PointlessController).FullName);

        private readonly ILogger<PointlessController> _logger;

        public PointlessController(IHttpClientFactory http, ILogger<PointlessController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Well, that was pointless.");
            return Ok(new PointlessModel {Whatever = "Whatever"});
        }
    }
}