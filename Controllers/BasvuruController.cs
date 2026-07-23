using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Reflection;
using TarimDonusum.Araclar;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.ViewModels.Basvuru;

namespace TarimDonusum.Controllers
{
    public class BasvuruController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuruIsKurallari;

        public BasvuruController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuruIsKurallari)
            : base(loggerFactory, localizer)
        {
            _basvuruIsKurallari = basvuruIsKurallari;
        }

        [OturumKontrol]
        public async Task<IActionResult> Index()
        {
            Sonuc<List<Basvuru>> sonuc;
            ; try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                ViewBag.YeniBasvuruYetkisi = BasvuruKullanicisiMi(kullanici);
                sonuc = await _basvuruIsKurallari.KullaniciBasvurulariniListeleAsync(kullanici);
            }
            catch (Exception ex)
            {
                sonuc = new Sonuc<List<Basvuru>>();
                sonuc.HataEkle("Başvuru kayıtları listelenemedi.");
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
                    TempData["Mesaj"] = "Yeni ön başvuru yalnızca başvuru kullanıcıları tarafından oluşturulabilir.";
                    return RedirectToAction(nameof(Index));
                }

                return View("Form", await FormViewModelHazirlaAsync(YeniBasvuru(), kullanici));
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Yeni başvuru ekranı açılamadı.");
                return View("Form", HataModeli(YeniBasvuru(), 1, "Yeni başvuru ekranı açılamadı."));
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
                TempData["Mesaj"] = "Başvuru kaydı okunamadı.";
                return RedirectToAction(nameof(Index));
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
                BasvuruBolumTanim? bolumTanim = BasvuruBolumleri.Bul(bolum, model.DenetciGorunumu);
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
                    sonuc.HataEkle("Başvuru sahibi bilgileri okunamadı.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Uygulama adresleri listelenemedi.");
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
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc<BasvuruUygulamaAdresi> sonuc = await _basvuruIsKurallari.UygulamaAdresiKaydetAsync(adres, kullanici);
                if (!sonuc.basarili || sonuc.nesne == null)
                    return Json(sonuc);
                sonuc.mesaj = "Uygulama adresi kaydedildi.";
                return Json(sonuc);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Uygulama adresi kaydet action tamamlanamadı.");
                return Json(new { basarili = false, mesaj = "Uygulama adresi kaydedilemedi." });
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
                sonuc.mesaj = "Uygulama adresi silindi.";
            }
            catch (Exception ex)
            {
                sonuc = new Sonuc();
                sonuc.HataEkle("Uygulama adresi silinemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
                return Json(sonuc);
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                sonuc.HataEkle("Ortaklık bilgileri kaydedilemedi.");
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
                sonuc.HataEkle("Ortak/pay sahibi bilgileri kaydedilemedi.");
                return Json(sonuc);
            }

            return Json(sonuc);
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
                    sonuc.HataEkle("Oturum süresi doldu.");
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
                sonuc.HataEkle("Doküman paketi yüklenemedi.");
            }

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
                    sonuc.HataEkle("Oturum süresi doldu.");
                    return Json(sonuc);
                }

                byte[] icerik = await DosyaIcerigiOkuAsync(dosya);
                sonuc = await _basvuruIsKurallari.TaahhutDosyasiKaydetAsync(basvuruId, dosya?.FileName ?? "", icerik, aciklama, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Taahhüt dosyası yükleme action tamamlanamadı.");
                sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                sonuc.HataEkle("Taahhüt dosyası yüklenemedi.");
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
                    sonuc.HataEkle("Oturum süresi doldu.");
                    return Json(sonuc);
                }

                byte[] icerik = await DosyaIcerigiOkuAsync(dosya);
                sonuc = await _basvuruIsKurallari.DenetimDosyasiKaydetAsync(basvuruId, dosya?.FileName ?? "", icerik, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Bağımsız denetim dosyası yükleme action tamamlanamadı.");
                sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                sonuc.HataEkle("Bağımsız denetim dosyası yüklenemedi.");
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
                    sonuc.HataEkle("Oturum süresi doldu.");
                    return Json(sonuc);
                }

                byte[] icerik = await DosyaIcerigiOkuAsync(dosya);
                sonuc = await _basvuruIsKurallari.BasvuruDosyasiKaydetAsync(basvuruId, formAd, dosyaNo, dosya?.FileName ?? "", icerik, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Ortaklık dosyası yükleme action tamamlanamadı.");
                sonuc = new Sonuc<BasvuruDosyaYuklemeSonucu>();
                sonuc.HataEkle("Ortaklık dosyası yüklenemedi.");
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
                sonuc.HataEkle("Adli sicil kişileri kaydedilemedi.");
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
                return NotFound("Dosya indirilemedi.");
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
                    sonuc.HataEkle("Firma bulunamadı");
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Firma sorgula action tamamlanamadı.");
                sonuc = new Sonuc<Firma>();
                sonuc.HataEkle("Firma sorgula action tamamlanamadı.");
            }
            return Json(sonuc);
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FirmaKaydet(Firma firma)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                sonuc = await _basvuruIsKurallari.FirmaEkleGuncelleAsync(firma, kullanici);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Firma ekle action tamamlanamadı.");
                sonuc = new Sonuc<int>();
                sonuc.nesne = -1;
                sonuc.HataEkle("Firma kaydedilemedi.");
            }
            return Json(sonuc);
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
            bool duzenlenebilirDurum = basvuru.durum == enumBasvuruDurum.OnBasvuruDurumu;

            BasvuruFormViewModel model = new BasvuruFormViewModel
            {
                Basvuru = basvuru,
                SaltOkunur = !basvuruKullanicisi || !duzenlenebilirDurum,
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

        //private static Basvuru BasvuruHazirla(Basvuru basvuru)
        //{
        //    if (basvuru.Durum == enumBasvuruDurum.Tanimsiz)
        //    {
        //        basvuru.Durum = enumBasvuruDurum.OnBasvuruDurumu;
        //    }

        //    basvuru.FirmaId = basvuru.Firma.Id;
        //    basvuru.TicaretUnvani = basvuru.Firma.TicaretUnvani;
        //    basvuru.VergiKimlikNo = basvuru.Firma.VergiKimlikNo;
        //    basvuru.DonemId = basvuru.Donem.Id;
        //    basvuru.BasvuruDonemi = basvuru.Donem.Ad;

        //    basvuru.IlId = basvuru.Il.Id;
        //    basvuru.IlAdi = basvuru.Il.Ad;

        //    basvuru.YatirimAdresSayisi = basvuru.YatirimAdresleri.Count;

        //    return basvuru;
        //}
    }
}


/*



    string[] belgeler = Items("Basvuru.Options.DocumentGroups");

        document.addEventListener('DOMContentLoaded', () => {

        let locked = @(kilitli ? "true" : "false");
        const lockedMessage = @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Js.LockedMessage")));
        const unsavedMessage = @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Js.UnsavedMessage")));
        const form = document.getElementById('basvuruForm');

        const activeInput = document.getElementById('aktifBolum');
        const stepHelpMessages = {
            1: [
    @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Section1.Description"))),
    @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Flow.Note")))
            ]
        };
        const firmaId = document.getElementById('firmaId');
        const firmaAdi = document.getElementById('firmaAdi');
        const vergiInput = document.querySelector('input[name="Basvuru.BasvuruFirma.VergiKimlikNo"]');
        const firmaModalEl = document.getElementById('firmaModal');
        const firmaModal = modalOlustur(firmaModalEl);
        active = @Model.AktifBolum;
        let basvuruId = @b.Id;
        const paraLocale = @Html.Raw(JsonSerializer.Serialize(CultureInfo.CurrentUICulture.Name));
        const paraFormatter = new Intl.NumberFormat(paraLocale || undefined, { maximumFractionDigits: 0 });
        const basvuruIlAdi = @Html.Raw(JsonSerializer.Serialize(basvuruIlAdi));
        const ilceler = @Html.Raw(JsonSerializer.Serialize(Model.Ilceler.Select(ilce => new
        {
            ilce.Id,
            ilce.Ad
        })));


        async function basvuruKaydetAjax() {
            if (!form) return;

            const submitButtons = Array.from(form.querySelectorAll('button[type="submit"]'));
            submitButtons.forEach(button => button.disabled = true);

            try {
                paraInputlariniNormalizeEt();
                if (activeInput) activeInput.value = active;

                const response = await fetch(form.action, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: basvuruKaydetFormDataOlustur()
                });

                const contentType = response.headers.get('content-type') || '';
                if (!contentType.includes('application/json')) {
                    basvuruMesajGoster('Başvuru kaydedilemedi.', false);
                    return;
                }

                const result = await response.json();
                if (!result.basarili) {
                    basvuruMesajGoster(result.mesaj || 'Başvuru kaydedilemedi.', false);
                    if (result.redirectUrl) {
                        window.location.href = result.redirectUrl;
                    }
                    return;
                }

                const yeniId = Number(result.id || 0);
                if (yeniId > 0) {
                    basvuruId = yeniId;
                    const idInput = form.querySelector('input[name="Basvuru.Id"]');
                    if (idInput) idInput.value = String(yeniId);
                    const firmaBasvuruIdInput = form.querySelector('input[name="Basvuru.BasvuruFirma.BasvuruId"]');
                    if (firmaBasvuruIdInput) firmaBasvuruIdInput.value = String(yeniId);
                    locked = false;
                    stepSelect?.querySelectorAll('option').forEach(option => option.disabled = false);
                    if (result.url) {
                        window.history.replaceState({}, '', result.url);
                    }
                }

                dirty = false;
                basvuruMesajGoster(result.mesaj || 'Başvuru kaydedildi.');
            } catch {
                basvuruMesajGoster('Başvuru kaydedilemedi.', false);
            } finally {
                submitButtons.forEach(button => button.disabled = false);
                form.querySelectorAll('.money-integer').forEach(input => paraFormatla(input));
            }
        }

        function basvuruKaydetFormDataOlustur() {
            if (Number(active) !== 1) {
                return new FormData(form);
            }

            const data = new FormData();
            const token = antiForgeryToken();
            if (token) {
                data.set('__RequestVerificationToken', token);
            }

            const deger = (name) => form.querySelector(`[name="${name}"]`)?.value ?? '';
        }


        dirty = false;

        document.getElementById('stepHelpBtn')?.addEventListener('click', () => {
            const messages = stepHelpMessages[active] || [document.querySelector(`.basvuru-panel[data-panel="${active}"] h2`)?.textContent || ''];
            basvuruMesajGoster(messages.filter(Boolean).join('\n\n'));
        });


        function firmaDeger(firma, alan) {
            if (!firma) return '';
            const camel = alan.charAt(0).toLowerCase() + alan.slice(1);
            return firma[camel] || firma[alan] || '';
        }


        function antiForgeryToken() {
            return form?.querySelector('input[name="__RequestVerificationToken"]')?.value ||
                document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
                '';
        }

        function basvuruMesajGoster(mesaj, basarili = true) {
            if (typeof PopupMesajGoster === 'function') {
                PopupMesajGoster(mesaj, basarili);
                return;
            }

            alert(mesaj);
        }

        if (typeof PopupMesajIlklendir === 'function') {
            PopupMesajIlklendir();
        }

        function enumLabel(labels, value) {
            const index = Number(value || 0) - 1;
            return index >= 0 && index < labels.length ? labels[index] : '';
        }

        function ilceAdiBul(ilceId) {
            const id = Number(ilceId || 0);
            if (!id) return '';
            const ilce = ilceler.find(x => Number(x.id || x.Id) === id);
            return ilce?.ad || ilce?.Ad || '';
        }

        function hiddenInput(name, value) {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = name;
            input.value = value || '';
            return input;
        }

        document.querySelectorAll('.uygulama-adres-modal-kapat').forEach(btn => {
            btn.addEventListener('click', () => uygulamaAdresModal?.hide());
        });


    });


*/
