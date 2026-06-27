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
            var bytes = _captcha.Create(HttpContext);
            return File(bytes, "image/png");
        }
    }
}
