using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class DonemController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuruIsKurallari;
        private readonly TanimIsKurallari _tanimIsKurallari;

        public DonemController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuruIsKurallari, TanimIsKurallari tanimIsKurallari)
            : base(loggerFactory, localizer)
        {
            _basvuruIsKurallari = basvuruIsKurallari;
            _tanimIsKurallari = tanimIsKurallari;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici?.Yetkiler.Any(x => x.Rol == KullaniciRol.SistemYoneticisi) != true)
                return Forbid();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Listele()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            return Json(await _tanimIsKurallari.DonemleriListeleAsync(kullanici));
        }

        [HttpPost]
        public async Task<IActionResult> Kaydet([FromBody] Donem donem)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            return Json(await _tanimIsKurallari.DonemKaydetAsync(donem, kullanici));
        }
    }
}
