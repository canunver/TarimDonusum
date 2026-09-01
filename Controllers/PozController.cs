using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class PozController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuru;
        private readonly TanimIsKurallari _tanim;

        public PozController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer,
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

        [HttpGet]
        public async Task<IActionResult> Listele() => Json(await _tanim.PozlariListeleAsync(await KullaniciAsync()));

        [HttpPost]
        public async Task<IActionResult> Kaydet([FromBody] Poz model) => Json(await _tanim.PozKaydetAsync(model, await KullaniciAsync()));

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ExcelYukle(IFormFile? dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return Json(new Sonuc<object> { hatalar = { "Excel dosyası seçilmelidir." } });
            string uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
            if (uzanti is not ".xlsx" and not ".xls")
                return Json(new Sonuc<object> { hatalar = { "Yalnızca .xlsx veya .xls dosyası yüklenebilir." } });
            await using Stream stream = dosya.OpenReadStream();
            return Json(await _tanim.PozExcelYukleAsync(stream, await KullaniciAsync()));
        }
    }
}
