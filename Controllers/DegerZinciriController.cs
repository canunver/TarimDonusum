using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class DegerZinciriController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuru;
        private readonly TanimIsKurallari _tanim;
        public DegerZinciriController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuru, TanimIsKurallari tanim) : base(loggerFactory, localizer)
        { _basvuru = basvuru; _tanim = tanim; }

        private Task<Kullanici?> KullaniciAsync() => OturumKullanicisiOkuAsync(_basvuru);

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Kullanici? kullanici = await KullaniciAsync();
            if (kullanici?.Yetkiler.Any(x => x.Rol == KullaniciRol.SistemYoneticisi) != true) return Forbid();
            return View();
        }
        [HttpGet] public async Task<IActionResult> Listele() => Json(await _tanim.DegerZincirleriniListeleAsync(await KullaniciAsync()));
        [HttpPost] public async Task<IActionResult> Kaydet([FromBody] DegerZinciri model) => Json(await _tanim.DegerZinciriKaydetAsync(model, await KullaniciAsync()));
        [HttpGet] public async Task<IActionResult> IlListele(int degerZinciriId) => Json(await _tanim.DegerZinciriIlleriniListeleAsync(degerZinciriId, await KullaniciAsync()));
        [HttpPost] public async Task<IActionResult> IlEkle([FromBody] DegerZinciriIl model) => Json(await _tanim.DegerZinciriIlEkleAsync(model.DegerZinciriId, model.IlId, await KullaniciAsync()));
        [HttpPost] public async Task<IActionResult> IlSil([FromBody] DegerZinciriIl model) => Json(await _tanim.DegerZinciriIlSilAsync(model.DegerZinciriId, model.IlId, await KullaniciAsync()));
    }
}
