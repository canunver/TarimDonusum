using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TarimDonusum.Framework;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    public class HomeController : BMYController
    {
        public static string RastgeleResim()
        {
            string klasor = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "img",
                "home");

            var dosyalar = Directory.GetFiles(klasor, "*.jpg");
            var secilen = dosyalar[Random.Shared.Next(dosyalar.Length)];
            return "/img/home/" + Path.GetFileName(secilen);
        }

        public HomeController(ILoggerFactory loggerFactory)
       : base(loggerFactory)
        {
        }

        public IActionResult Index()
        {
            Log(LogLevel.Information, FrameWork.Logging.BMYEventID.Yok, null, "Ana sayfa açýldý.");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
