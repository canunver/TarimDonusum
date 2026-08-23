using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using TarimDonusum.Araclar;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Raporlar;
using TarimDonusum.ViewModels.Basvuru;

namespace TarimDonusum.Controllers
{
    public class BasvuruController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuruIsKurallari;
        private readonly IWebHostEnvironment _environment;

        public BasvuruController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuruIsKurallari,
            IWebHostEnvironment environment)
            : base(loggerFactory, localizer)
        {
            _basvuruIsKurallari = basvuruIsKurallari;
            _environment = environment;
        }

        [OturumKontrol]
        public async Task<IActionResult> Index(string tur = "on-basvuru")
        {
            Sonuc<List<Basvuru>> sonuc;
            ; try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                ViewBag.YeniBasvuruYetkisi = BasvuruKullanicisiMi(kullanici);
                sonuc = await _basvuruIsKurallari.KullaniciBasvuruVersiyonlariniListeleAsync(kullanici);
                enumBasvuruKayitTuru kayitTuru = string.Equals(tur, "basvuru", StringComparison.OrdinalIgnoreCase)
                    ? enumBasvuruKayitTuru.Basvuru
                    : enumBasvuruKayitTuru.OnBasvuru;
                ViewBag.ListeTuru = kayitTuru;

                if (sonuc.nesne != null)
                {
                    sonuc.nesne = sonuc.nesne
                        .Where(x => x.kayitTuru == kayitTuru)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                sonuc = new Sonuc<List<Basvuru>>();
                sonuc.HataEkle(L["Basvuru.Message.ListFailed"].ToString());
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru liste ekranı açılamadı.");
            }
            return View(sonuc.nesne ?? new List<Basvuru>());
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> Yeni()
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (!BasvuruKullanicisiMi(kullanici))
                {
                    TempData["Mesaj"] = L["Basvuru.Message.NewApplicantOnly"].ToString();
                    return RedirectToAction(nameof(Index));
                }

                return View("Form", await FormViewModelHazirlaAsync(YeniBasvuru(), kullanici));
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Yeni başvuru ekranı açılamadı.");
                return View("Form", HataModeli(YeniBasvuru(), 1, L["Basvuru.Message.NewScreenFailed"].ToString()));
            }
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> Duzenle(int id)
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
                if (!sonuc.basarili || sonuc.nesne == null)
                {
                    TempData["Mesaj"] = sonuc.hatalar.Count > 0
                        ? string.Join(" ", sonuc.hatalar)
                        : L["Basvuru.Message.NotFound"].ToString();

                    return RedirectToAction(nameof(Index));
                }

                return View("Form", await FormViewModelHazirlaAsync(sonuc.nesne, kullanici));
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru düzenleme ekranı açılamadı. BasvuruId: {BasvuruId}", id);
                TempData["Mesaj"] = L["Basvuru.Message.ReadFailed"].ToString();
                return RedirectToAction(nameof(Index));
            }
        }

        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> BasvuruSahibiYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_BasvuruSahibi(_environment.ContentRootPath), "Başvuru sahibi bilgileri yazdırılmadan önce kaydedilmelidir.");

        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> OrtaklikYetkiYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_OrtaklikYetki(_environment.ContentRootPath), "Ortaklık ve yetki bilgileri yazdırılmadan önce kaydedilmelidir.");
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> YatirimBilgileriYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_YatirimBilgileri(_environment.ContentRootPath), "Yatırım bilgileri yazdırılmadan önce kaydedilmelidir.");
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> OnBasvuruSahibiYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_BasvuruSahibi(_environment.ContentRootPath, true), "Ön başvuru sahibi bilgileri yazdırılmadan önce kaydedilmelidir.");
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> ProjeButcesiYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_ProjeButcesi(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.ProjeButcesi"].ToString());
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> YatirimOzetiYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_YatirimOzeti(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.YatirimOzeti"].ToString());
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> IsletmeGideriYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_IsletmeGideri(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.IsletmeGideri"].ToString());
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> OnBasvuruTamlikYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_OnBasvuruTamlik(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.OnBasvuruTamlik"].ToString(), true);
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> OnBasvuruUygunlukYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_OnBasvuruUygunluk(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.OnBasvuruUygunluk"].ToString(), true);
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> OnBilgilerYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_OnBilgiler(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.OnBilgiler"].ToString());
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> MakineEkipmanYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_MakineEkipman(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.MakineEkipman"].ToString());
        [OturumKontrol]
        [HttpGet]
        public Task<IActionResult> BinaListesiYazdir(int id) =>
            RaporYazdirAsync(id, new RPROB_BinaListesi(_environment.ContentRootPath), L["Basvuru.Report.SaveFirst.BinaListesi"].ToString());

        private async Task<IActionResult> RaporYazdirAsync(int id, IRPROB rapor, string kayitUyarisi, bool denetciRaporu = false)
        {
            if (id <= 0) return BadRequest(kayitUyarisi);
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null) return Unauthorized();
            if (denetciRaporu && BasvuruKullanicisiMi(kullanici)) return Forbid();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : L["Basvuru.Message.NotFound"].ToString());

            if (denetciRaporu) _basvuruIsKurallari.DenetimListeleriniIlkDegerle(sonuc.nesne);
            try
            {
                RaporDosyasi dosya = rapor.Olustur(sonuc.nesne, id);
                return File(dosya.Icerik, RaporDosyasi.ExcelMimeTuru, dosya.DosyaAdi);
            }
            catch (FileNotFoundException ex) when (
                !string.IsNullOrWhiteSpace(ex.FileName)
                && string.Equals(
                    Path.GetFullPath(ex.FileName),
                    Path.GetFullPath(rapor.SablonDosyasi),
                    StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(L["Basvuru.Report.TemplateNotFound", Path.GetFullPath(rapor.SablonDosyasi)].ToString());
            }
            catch (FileNotFoundException ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex,
                    "Rapor oluşturulurken bir bağımlılık yüklenemedi. Eksik bileşen: {EksikBilesen}",
                    ex.FileName ?? ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Rapor oluşturulamadı. Eksik uygulama bileşeni: {ex.FileName ?? ex.Message}");
            }
        }

        private static bool BasvuruKullanicisiMi(Kullanici? kullanici)
        {
            return kullanici?.Yetkiler.Any(y => y.Rol == KullaniciRol.BasvuruKullanicisi) == true;
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> BolumGetir(int basvuruId, enumBasvuruBolum bolum)
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return Unauthorized();

                Basvuru basvuru = YeniBasvuru();
                if (basvuruId > 0)
                {
                    Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(basvuruId, kullanici);
                    if (!sonuc.basarili || sonuc.nesne == null)
                        return NotFound();

                    basvuru = sonuc.nesne;
                }
                else if (bolum != enumBasvuruBolum.Firma)
                {
                    return BadRequest();
                }

                BasvuruFormViewModel model = await FormViewModelHazirlaAsync(basvuru, kullanici);
                if (model.DenetciGorunumu)
                    _basvuruIsKurallari.DenetimListeleriniIlkDegerle(model.Basvuru);
                BasvuruBolumTanim? bolumTanim = BasvuruBolumleri.Bul(bolum, model.DenetciGorunumu, model.Basvuru.kayitTuru);
                if (bolumTanim == null)
                    return BadRequest();

                return PartialView(bolumTanim.PartialView, model);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru bölüm partial yüklenemedi. BasvuruId: {BasvuruId}, Bolum: {Bolum}", basvuruId, bolum);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetBasvuruSahibi([FromBody] Basvuru? model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                if (model == null || !ModelState.IsValid)
                {
                    List<string> modelStateHatalari = ModelStateHatalariniOku();
                    string hataDetayi = modelStateHatalari.Count > 0
                        ? string.Join(" | ", modelStateHatalari)
                        : "Request body bos ya da Basvuru modeline donusturulemedi.";

                    Log(LogLevel.Warning, BMYEventID.Yok, null, "KaydetBasvuruSahibi model binding hatası. Hatalar: {Hatalar}", hataDetayi);

                    sonuc = new Sonuc<int>();
                    sonuc.HataEkle(L["Basvuru.Message.OwnerReadFailed"].ToString());
                    foreach (string hata in modelStateHatalari.Take(10))
                        sonuc.HataEkle(hata);

                    return Json(sonuc);
                }

                sonuc = await _basvuruIsKurallari.KaydetBasvuruSahibiAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru sahibi kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetUygunHarcama([FromBody] BasvuruUygunHarcama model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetUygunHarcamaAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Uygun harcama kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetFirmaBasvuru([FromBody] BasvuruFirma model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetFirmaBasvuru(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetIrtibat([FromBody] BasvuruIrtibat model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetIrtibatAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetYatirimBilgileri([FromBody] BasvuruYatirim model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                sonuc = await _basvuruIsKurallari.KaydetYatirimBilgileriAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> UygulamaAdresiListele(int basvuruId)
        {
            Sonuc<List<BasvuruUygulamaAdresi>> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                sonuc = await _basvuruIsKurallari.UygulamaAdresiListeleAsync(basvuruId, kullanici);
            }
            catch (Exception)
            {
                sonuc = new Sonuc<List<BasvuruUygulamaAdresi>>();
                sonuc.HataEkle(L["Basvuru.Message.AddressListFailed"].ToString());
            }
            return Json(sonuc);
        }


        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UygulamaAdresiKaydet([FromBody] BasvuruUygulamaAdresi adres)
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return Json(new { basarili = false, mesaj = L["Basvuru.Message.SessionExpired"].ToString() });

                Sonuc<BasvuruUygulamaAdresi> sonuc = await _basvuruIsKurallari.UygulamaAdresiKaydetAsync(adres, kullanici);
                if (!sonuc.basarili || sonuc.nesne == null)
                    return Json(sonuc);
                sonuc.mesaj = L["Basvuru.Message.AddressSaved"].ToString();
                return Json(sonuc);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Uygulama adresi kaydet action tamamlanamadı.");
                return Json(new { basarili = false, mesaj = L["Basvuru.Message.AddressSaveFailed"].ToString() });
            }
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UygulamaAdresiSil([FromBody] BasvuruUygulamaAdresi adres)
        {
            Sonuc sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);

                sonuc = await _basvuruIsKurallari.UygulamaAdresiSilAsync(adres.id, kullanici);
                sonuc.mesaj = L["Basvuru.Message.AddressDeleted"].ToString();
            }
            catch (Exception ex)
            {
                sonuc = new Sonuc();
                sonuc.HataEkle(L["Basvuru.Message.AddressDeleteFailed"].ToString());
                Log(LogLevel.Error, BMYEventID.Yok, ex, sonuc.hatalar[0]);
            }
            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetFinans([FromBody] BasvuruFinans model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = new Sonuc<int>();
                sonuc = await _basvuruIsKurallari.KaydetFinansAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetMali([FromBody] BasvuruMali model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = new Sonuc<int>();
                sonuc = await _basvuruIsKurallari.KaydetMaliAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetYatirimOzeti([FromBody] BasvuruYatirimOzeti model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetYatirimOzetiAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Yatırım özeti kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetDbCtpTeknikProje([FromBody] BasvuruDbCtpTeknikProje model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetDbCtpTeknikProjeAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "DB C-TP Teknik Proje kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
            }
            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetCevreselSosyal([FromBody] BasvuruCevreselSosyal model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetCevreselSosyalAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Çevresel-sosyal anket kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetDegerZinciri([FromBody] BasvuruYatirim model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                sonuc = await _basvuruIsKurallari.KaydetDegerZinciriAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru değer zinciri kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.SaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetOrtaklik([FromBody] BasvuruOrtaklik model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                sonuc = await _basvuruIsKurallari.KaydetOrtaklikAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Ortaklık bilgileri kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.PartnershipSaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetOrtaklar([FromBody] BasvuruOrtaklik model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                sonuc = await _basvuruIsKurallari.KaydetOrtaklarAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Ortak/pay sahibi bilgileri kaydet action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.HataEkle(L["Basvuru.Message.StakeholderSaveFailed"].ToString());
                return Json(sonuc);
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetBasvuruBagliOrtak([FromBody] BasvuruOrtak model)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null) return RedirectToAction("Index", "Home");
            Sonuc<int> sonuc = await _basvuruIsKurallari.KaydetBasvuruBagliOrtakAsync(model, kullanici);
            if (sonuc.basarili) sonuc.mesaj = L["kayitBasarili"];
            return Json(sonuc);
        }
        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrtakSil([FromBody] BasvuruOrtakSilModel model)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null) return RedirectToAction("Index", "Home");
            return Json(await _basvuruIsKurallari.OrtakSilAsync(model.basvuruId, model.ortakId, kullanici));
        }
        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BelgePaketiYukle(
            int basvuruId,
            string aciklama,
            string belgeBeyani,
            List<string> belgeGruplari,
            List<string> seciliBelgeGruplari,
            IFormFile dosya)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                {
                    sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                    sonuc.HataEkle(L["Basvuru.Message.SessionExpired"].ToString());
                    return Json(sonuc);
                }

                byte[] icerik = await DosyaIcerigiOkuAsync(dosya);
                sonuc = await _basvuruIsKurallari.BelgePaketiKaydetAsync(
                    basvuruId,
                    dosya?.FileName ?? "",
                    icerik,
                    aciklama,
                    belgeBeyani,
                    belgeGruplari,
                    seciliBelgeGruplari,
                    kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Doküman paketi yükleme action tamamlanamadı.");
                sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                sonuc.HataEkle(L["Basvuru.Message.DocumentPackageUploadFailed"].ToString());
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetTaahhutBeyanlari([FromBody] BasvuruTaahhutBeyanlar model)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null) return RedirectToAction("Index", "Home");
            Sonuc<int> sonuc = await _basvuruIsKurallari.KaydetTaahhutBeyanlariAsync(model, kullanici);
            if (sonuc.basarili) sonuc.mesaj = L["kayitBasarili"];
            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaahhutDosyasiYukle(int basvuruId, string aciklama, IFormFile dosya)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                {
                    sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                    sonuc.HataEkle(L["Basvuru.Message.SessionExpired"].ToString());
                    return Json(sonuc);
                }

                byte[] icerik = await DosyaIcerigiOkuAsync(dosya);
                sonuc = await _basvuruIsKurallari.TaahhutDosyasiKaydetAsync(basvuruId, dosya?.FileName ?? "", icerik, aciklama, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Taahhüt dosyası yükleme action tamamlanamadı.");
                sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                sonuc.HataEkle(L["Basvuru.Message.CommitmentUploadFailed"].ToString());
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenetimDosyasiYukle(int basvuruId, IFormFile dosya)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                {
                    sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                    sonuc.HataEkle(L["Basvuru.Message.SessionExpired"].ToString());
                    return Json(sonuc);
                }

                byte[] icerik = await DosyaIcerigiOkuAsync(dosya);
                sonuc = await _basvuruIsKurallari.DenetimDosyasiKaydetAsync(basvuruId, dosya?.FileName ?? "", icerik, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Bağımsız denetim dosyası yükleme action tamamlanamadı.");
                sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                sonuc.HataEkle(L["Basvuru.Message.AuditUploadFailed"].ToString());
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasvuruDosyasiYukle(int basvuruId, string formAd, int dosyaNo, IFormFile dosya)
        {
            Sonuc<BasvuruDosyaYuklemeSonucu> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                {
                    sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                    sonuc.HataEkle(L["Basvuru.Message.SessionExpired"].ToString());
                    return Json(sonuc);
                }

                byte[] icerik = await DosyaIcerigiOkuAsync(dosya);
                sonuc = await _basvuruIsKurallari.BasvuruDosyasiKaydetAsync(basvuruId, formAd, dosyaNo, dosya?.FileName ?? "", icerik, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Ortaklık dosyası yükleme action tamamlanamadı.");
                sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                sonuc.HataEkle(L["Basvuru.Message.PartnershipFileUploadFailed"].ToString());
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetAdliSicilKisileri([FromBody] BasvuruAdliSicilKayitModel model)
        {
            Sonuc<List<BasvuruAdliSicilKisi>> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                sonuc = await _basvuruIsKurallari.KaydetAdliSicilKisileriAsync(model.basvuruId, model.kisiler, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Adli sicil kişileri kaydet action tamamlanamadı.");
                sonuc = new Sonuc<List<BasvuruAdliSicilKisi>>();
                sonuc.HataEkle(L["Basvuru.Message.CriminalRecordSaveFailed"].ToString());
            }

            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> OrtaklikDosyasiYukle(int basvuruId, string formAd, int dosyaNo, IFormFile dosya)
        {
            return BasvuruDosyasiYukle(basvuruId, formAd, dosyaNo, dosya);
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> DosyaIndir(int id)
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                Sonuc<Dosya> sonuc = await _basvuruIsKurallari.DosyaIndirAsync(id, kullanici);
                if (!sonuc.basarili || sonuc.nesne == null)
                    return NotFound(sonuc.hataStr);

                string dosyaAdi = string.IsNullOrWhiteSpace(sonuc.nesne.DosyaAdi)
                    ? $"dosya-{id}"
                    : sonuc.nesne.DosyaAdi;

                return File(sonuc.nesne.Icerik, "application/octet-stream", dosyaAdi);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Dosya indirme action tamamlanamadı. DosyaId: {DosyaId}", id);
                return NotFound(L["Basvuru.Message.FileDownloadFailed"].ToString());
            }
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> FirmaSorgula(string vergiKimlikNo, int id)
        {
            Sonuc<Firma> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);

                sonuc = await _basvuruIsKurallari.FirmaVergiNoIleOkuAsync(kullanici, id, vergiKimlikNo);
                if (sonuc.nesne == null)
                {
                    sonuc.HataEkle(L["Basvuru.Message.CompanyNotFound"].ToString());
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Firma sorgula action tamamlanamadı.");
                sonuc = new Sonuc<Firma>();
                sonuc.HataEkle(L["Basvuru.Message.CompanyQueryFailed"].ToString());
            }
            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FirmaKaydet([FromBody] Firma? firma)
        {
            Sonuc<int> sonuc;
            try
            {
                if (!ModelState.IsValid || firma == null)
                {
                    string modelHatalari = string.Join(" | ", ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e =>
                            $"{(string.IsNullOrWhiteSpace(x.Key) ? "Firma" : x.Key)}: {e.ErrorMessage}{e.Exception?.Message}")));

                    if (string.IsNullOrWhiteSpace(modelHatalari))
                        modelHatalari = "İstek gövdesinden firma nesnesi oluşturulamadı.";

                    Log(LogLevel.Warning, BMYEventID.Yok, null,
                        "Başvuru firma popup modeli bağlanamadı. ModelState: {ModelState}", modelHatalari);

                    sonuc = new Sonuc<int>();
                    sonuc.HataEkle($"Firma bilgileri okunamadı: {modelHatalari}");
                    return Json(sonuc);
                }

                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                sonuc = await _basvuruIsKurallari.FirmaEkleGuncelleAsync(firma, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Firma ekle action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.nesne = -1;
                sonuc.HataEkle(L["Basvuru.Message.CompanySaveFailed"].ToString());
            }
            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncelemeyeGonder(int basvuruId)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized(new { basarili = false, mesaj = L["Business.Session.Expired"].ToString() });

            Sonuc sonuc = await _basvuruIsKurallari.IncelemeyeGonderAsync(basvuruId, kullanici);
            return Json(new
            {
                basarili = sonuc.basarili,
                mesaj = sonuc.basarili ? sonuc.mesaj : string.Join(" ", sonuc.hatalar)
            });
        }

        private List<string> ModelStateHatalariniOku()
        {
            return ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e =>
                {
                    string mesaj = !string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? e.ErrorMessage
                        : e.Exception?.Message ?? "Bilinmeyen model binding hatası.";

                    return $"{x.Key}: {mesaj}";
                }))
                .ToList();
        }

        private async Task<BasvuruFormViewModel> FormViewModelHazirlaAsync(Basvuru basvuru, Kullanici? kullanici)
        {
            bool basvuruKullanicisi = BasvuruKullanicisiMi(kullanici);
            bool duzenlenebilirDurum =
                ((basvuru.durum == enumBasvuruDurum.OnBasvuruDurumu ||
                  basvuru.durum == enumBasvuruDurum.OnBasvuruDuzeltmeDurumu) ||
                 (basvuru.durum == enumBasvuruDurum.BasvuruDurumu && basvuru.kayitTuru == enumBasvuruKayitTuru.Basvuru)) &&
                basvuru.basvuruFirma.siraNo == 0;

            BasvuruFormViewModel model = new BasvuruFormViewModel
            {
                Basvuru = basvuru,
                SaltOkunur = !basvuruKullanicisi || !duzenlenebilirDurum,
                DenetciGorunumu = kullanici != null && !basvuruKullanicisi,
            };

            await ReferansListeleriYukleAsync(model);
            return model;
        }

        private async Task<Sonuc<List<Donem>>> DonemleriOkuAsync()
        {
            Sonuc<List<Donem>> sonuc = await _basvuruIsKurallari.DonemleriListeleAsync();
            return sonuc;
        }

        private async Task<Sonuc<List<Il>>> IlleriOkuAsync()
        {
            Sonuc<List<Il>> sonuc = await _basvuruIsKurallari.IlleriListeleAsync();
            return sonuc;
        }

        private async Task<Sonuc<List<Ilce>>> IlceleriOkuAsync(int? ilId)
        {
            Sonuc<List<Ilce>> sonuc = await _basvuruIsKurallari.IlceleriListeleAsync(ilId);
            return sonuc;
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> DegerZinciriAsamalariListele(int zincirId, int basvuruId)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            Sonuc<List<DegerZinciriAsama>> degerZinciriAsamalari = await _basvuruIsKurallari.DegerZinciriAsamalariListeleAsync(kullanici, zincirId, basvuruId);

            return Json(degerZinciriAsamalari);
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> DegerZincirleriListele(int ilId, int basvuruId)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            var degerZincirleri = await _basvuruIsKurallari.DegerZincirleriListeleAsync(kullanici, ilId, basvuruId);
            return Json(degerZincirleri);
        }

        private async Task ReferansListeleriYukleAsync(BasvuruFormViewModel model)
        {
            model.Donemler = (await DonemleriOkuAsync()).nesne;
            model.Iller = (await IlleriOkuAsync()).nesne;
            model.Ilceler = (await IlceleriOkuAsync(model.Basvuru.basvuruFirma.il.id)).nesne;
        }

        private static BasvuruFormViewModel HataModeli(Basvuru basvuru, int aktifBolum, string mesaj)
        {
            return new BasvuruFormViewModel
            {
                Basvuru = basvuru,
                Hatalar = new List<string> { mesaj }
            };
        }

        private static Basvuru YeniBasvuru()
        {
            return new Basvuru();
        }

        private static async Task<byte[]> DosyaIcerigiOkuAsync(IFormFile? dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return [];

            await using MemoryStream stream = new MemoryStream();
            await dosya.CopyToAsync(stream);
            return stream.ToArray();
        }
    }
}
