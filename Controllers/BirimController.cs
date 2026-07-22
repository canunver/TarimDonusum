using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class BirimController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuruIsKurallari;
        private readonly TanimIsKurallari _tanimIsKurallari;

        public BirimController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuruIsKurallari,
            TanimIsKurallari tanimIsKurallari)
            : base(loggerFactory, localizer)
        {
            _basvuruIsKurallari = basvuruIsKurallari;
            _tanimIsKurallari = tanimIsKurallari;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SayfaVerisi()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            Sonuc<List<Birim>> birimler = await _tanimIsKurallari.BirimleriListeleAsync(kullanici);
            if (!birimler.basarili)
                return Json(birimler);

            Sonuc<List<Il>> iller = await _basvuruIsKurallari.IlleriListeleAsync();
            Sonuc<object> sonuc = new Sonuc<object>
            {
                nesne = new
                {
                    birimler = birimler.nesne ?? new List<Birim>(),
                    iller = iller.nesne ?? new List<Il>()
                }
            };

            foreach (string hata in iller.hatalar)
                sonuc.HataEkle(hata);

            return Json(sonuc);
        }

        [HttpGet]
        public async Task<IActionResult> Listele()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            Sonuc<List<Birim>> sonuc = await _tanimIsKurallari.BirimleriListeleAsync(kullanici);
            return Json(sonuc);
        }

        [HttpPost]
        public async Task<IActionResult> Kaydet([FromBody] Birim birim)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            Sonuc<int> sonuc = await _tanimIsKurallari.BirimKaydetAsync(birim, kullanici);
            return Json(sonuc);
        }

        [HttpPost]
        public async Task<IActionResult> PasifYap([FromBody] Birim birim)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            Sonuc sonuc = await _tanimIsKurallari.BirimPasifYapAsync(birim.id, kullanici);
            return Json(sonuc);
        }
    }
}
