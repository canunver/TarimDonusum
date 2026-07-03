using Microsoft.AspNetCore.Mvc;
using TarimDonusum.FrameWork.Captcha;

namespace TarimDonusum.Controllers
{
    public class CaptchaController : Controller
    {
        private readonly CaptchaGenerator _captcha;

        public CaptchaController(CaptchaGenerator captcha)
        {
            _captcha = captcha;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Resim()
        {
            _captcha.Yenile(HttpContext);
            var bytes = _captcha.Create(HttpContext);
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            return File(bytes, "image/png");
        }
    }
}
