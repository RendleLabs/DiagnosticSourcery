using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Traffic
{
    class Program
    {
        private static readonly string[] Urls =
        {
            "https://en.wikipedia.org/wiki/Intel_80286",
            "https://arstechnica.com/gadgets/2018/11/apple-walks-ars-through-the-ipad-pros-a12x-system-on-a-chip",
            "https://www.cultofmac.com/440737/apple-history-newton-messagepad-launch",
            "https://www.tomshardware.com/news/ibm-7nm-silicon-germanium-transistors,29546.html",
            "https://physicsworld.com/a/vaporized-electrons-in-graphene-boost-signals-into-the-terahertz-range/",
            "https://scitechdaily.com/mit-engineers-develop-programmable-nanophotonic-processor/",
            "https://www.extremetech.com/extreme/232190-how-mits-new-biological-computer-works-and-what-it-could-do-in-the-future",
            "https://www.wired.co.uk/article/quantum-computing-explained",
        };
        
        static async Task Main()
        {
            var random = new Random();
            var http = new HttpClient();
            http.BaseAddress = new Uri("http://localhost:5000");

            var stopwatch = Stopwatch.StartNew();
            int count = 0;

            Console.Write("Making requests");

            while (stopwatch.ElapsedMilliseconds < 60000)
            {
                Console.Write(".");
                var url = Uri.EscapeDataString(Urls[random.Next(Urls.Length)]);
                using (await http.GetAsync($"/image?u={url}&w=500&h=375"))
                {
                    await Task.Delay(random.Next(500));
                }
                if (++count % 10 == 0)
                {
                    System.Console.Write($"{count}");
                }
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Done.");
        }
    }
}
