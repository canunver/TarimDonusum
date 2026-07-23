using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class IlController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuru;
        private readonly TanimIsKurallari _tanim;

        public IlController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuru, TanimIsKurallari tanim) : base(loggerFactory, localizer)
        {
            _basvuru = basvuru;
            _tanim = tanim;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
            if (kullanici?.Yetkiler.Any(x => x.Rol == KullaniciRol.SistemYoneticisi) != true)
                return Forbid();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Listele()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
            return Json(await _tanim.IlleriIlceleriyleListeleAsync(kullanici));
        }

        [HttpPost]
        public async Task<IActionResult> Kaydet([FromBody] Il il)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
            return Json(await _tanim.IlKaydetAsync(il, kullanici));
        }
    }
}
