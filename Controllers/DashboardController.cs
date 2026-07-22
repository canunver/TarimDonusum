using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class DashboardController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuru;
        private readonly TanimIsKurallari _tanim;
        public DashboardController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuru, TanimIsKurallari tanim) : base(loggerFactory, localizer)
        { _basvuru = basvuru; _tanim = tanim; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
            if (SadeceBasvuruKullanicisiMi(kullanici)) return RedirectToAction("Index", "Basvuru");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SayfaVerisi()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
            Sonuc<object> sonuc = new();
            if (kullanici == null || SadeceBasvuruKullanicisiMi(kullanici))
            {
                sonuc.HataEkle("Gösterge panelini görüntüleme yetkiniz bulunmuyor.");
                return Json(sonuc);
            }
            Sonuc<List<Donem>> donemler = await _basvuru.DonemleriListeleAsync();
            Sonuc<List<Birim>> birimler = await _tanim.DashboardBirimleriListeleAsync(kullanici);
            foreach (string hata in donemler.hatalar.Concat(birimler.hatalar)) sonuc.HataEkle(hata);
            if (sonuc.basarili)
                sonuc.nesne = new { donemler = donemler.nesne ?? new(), birimler = birimler.nesne ?? new() };
            return Json(sonuc);
        }

        [HttpGet]
        public async Task<IActionResult> Veriler(int donemId, int birimId)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
            if (kullanici == null || SadeceBasvuruKullanicisiMi(kullanici))
            {
                Sonuc<object> yetkisiz = new(); yetkisiz.HataEkle("Gösterge panelini görüntüleme yetkiniz bulunmuyor.");
                return Json(yetkisiz);
            }
            if (donemId <= 0 || birimId <= 0)
            {
                Sonuc<object> eksik = new(); eksik.HataEkle("Dönem ve birim seçilmelidir."); return Json(eksik);
            }
            Sonuc<List<Donem>> donemler = await _basvuru.DonemleriListeleAsync();
            if (donemler.nesne?.Any(x => x.id == donemId) != true)
            {
                Sonuc<object> gecersiz = new(); gecersiz.HataEkle("Seçilen dönem bulunamadı."); return Json(gecersiz);
            }
            Sonuc<List<Birim>> birimler = await _tanim.DashboardBirimleriListeleAsync(kullanici);
            Birim? seciliBirim = birimler.nesne?.FirstOrDefault(x => x.id == birimId);
            if (!birimler.basarili || seciliBirim == null)
            {
                Sonuc<object> yetkisizBirim = new(); yetkisizBirim.HataEkle("Seçilen birim için görüntüleme yetkiniz bulunmuyor."); return Json(yetkisizBirim);
            }
            Sonuc<List<Basvuru>> kaynak = await _basvuru.TumunuListeleAsync();
            Sonuc<object> sonuc = new();
            foreach (string hata in kaynak.hatalar) sonuc.HataEkle(hata);
            if (!sonuc.basarili) return Json(sonuc);
            List<Basvuru> liste = (kaynak.nesne ?? new())
                .Where(x => x.basvuruFirma.donemId == donemId)
                .Where(x => seciliBirim.birimTuru == enumBirimTuru.Merkez
                    || (seciliBirim.ilKod.HasValue && x.basvuruFirma.il.kod == seciliBirim.ilKod.Value))
                .ToList();
            sonuc.nesne = new
            {
                toplam = liste.Count,
                onBasvuru = liste.Count(x => x.durum == enumBasvuruDurum.OnBasvuruDurumu),
                basvuru = liste.Count(x => x.durum == enumBasvuruDurum.BasvuruDurumu),
                kabul = liste.Count(x => x.durum == enumBasvuruDurum.KabulEdildiDurumu),
                iptal = liste.Count(x => x.durum == enumBasvuruDurum.IptalDurumu),
                iller = liste.GroupBy(x => string.IsNullOrWhiteSpace(x.basvuruFirma.il.ad) ? "Belirtilmemiş" : x.basvuruFirma.il.ad)
                    .Select(x => new { ad = x.Key, sayi = x.Count() }).OrderByDescending(x => x.sayi).Take(6)
            };
            return Json(sonuc);
        }

        private static bool SadeceBasvuruKullanicisiMi(Kullanici? kullanici) =>
            kullanici?.Yetkiler.Any(y => y.Rol == KullaniciRol.BasvuruKullanicisi) == true;
    }
}
