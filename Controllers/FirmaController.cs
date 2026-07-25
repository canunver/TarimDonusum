using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class FirmaController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuru;
        private readonly FirmaIsKurallari _firma;

        public FirmaController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuru, FirmaIsKurallari firma) : base(loggerFactory, localizer)
        {
            _basvuru = basvuru;
            _firma = firma;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> Ara([FromQuery] FirmaArama arama)
            => Json(await _firma.AraAsync(arama, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpGet]
        public async Task<IActionResult> Oku(int id)
            => Json(await _firma.OkuAsync(id, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Kaydet([FromBody] Firma firma)
            => Json(await _firma.KaydetAsync(firma, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpGet]
        public async Task<IActionResult> BasvuranAra(string aramaMetni)
            => Json(await _firma.BasvuranAraAsync(aramaMetni, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BasvuranEkle(int firmaId, int kullaniciId)
            => Json(await _firma.BasvuranEkleAsync(firmaId, kullaniciId, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BasvuranCikar(int firmaId, int kullaniciId)
            => Json(await _firma.BasvuranCikarAsync(firmaId, kullaniciId, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpGet]
        public async Task<IActionResult> Loglar(int firmaId)
            => Json(await _firma.LoglariOkuAsync(firmaId, await OturumKullanicisiOkuAsync(_basvuru)));
    }
}
