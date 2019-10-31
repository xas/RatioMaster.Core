using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RatioMaster.Core.WebApp.Hubs;
using RatioMaster.Core.WebApp.Models;

namespace RatioMaster.Core.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RatioHostedService _ratioService;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, RatioHostedService service)
        {
            _logger = logger;
            _ratioService = service;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IList<IFormFile> files)
        {
            if (files != null && files.Any())
            {
                IFormFile source = files.First();
                string filename = ContentDispositionHeaderValue.Parse(source.ContentDisposition).FileName.Trim('"');
                filename = EnsureCorrectFilename(filename);
                using (FileStream output = System.IO.File.Create(GetPathAndFilename(filename)))
                {
                    await source.CopyToAsync(output);
                }
                CancellationToken cancellationToken;
                await _ratioService.UploadFile(cancellationToken, GetPathAndFilename(filename));
            }
            return View("Index");
        }

        [HttpPost]
        public IActionResult Start()
        {
            CancellationToken cancellationToken;
            _ratioService.StartAsync(cancellationToken);
            return View("Index");
        }

        [HttpPost]
        public IActionResult Stop()
        {
            CancellationToken cancellationToken;
            _ratioService.StopAsync(cancellationToken);
            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string EnsureCorrectFilename(string filename)
        {
            if (filename.Contains("\\"))
                filename = filename.Substring(filename.LastIndexOf("\\") + 1);

            return filename;
        }

        private string GetPathAndFilename(string filename)
        {
            return _env.WebRootPath + "\\uploads\\" + filename;
        }
    }
}
