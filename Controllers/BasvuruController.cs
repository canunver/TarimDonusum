using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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

        public async Task<IActionResult> Index()
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                {
                    if (AjaxIstegi())
                        return Json(new { basarili = false, mesaj = "Oturum süresi doldu.", redirectUrl = Url.Action("Index", "Home") });

                    return RedirectToAction("Index", "Home");
                }

                Sonuc<List<Basvuru>> sonuc = await _basvuruIsKurallari.KullaniciBasvurulariniListeleAsync(kullaniciId);
                if (!sonuc.Basarili)
                    TempData["Mesaj"] = HataMesaji(sonuc, "Başvuru kayıtları listelenemedi.");

                return View(sonuc.Nesne ?? new List<Basvuru>());
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru liste ekranı açılamadı.");
                TempData["Mesaj"] = "Başvuru kayıtları listelenemedi.";
                return View(new List<Basvuru>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Yeni()
        {
            try
            {
                if (OturumKullaniciId() <= 0)
                    return RedirectToAction("Index", "Home");

                return View("Form", await FormViewModelHazirlaAsync(YeniBasvuru(), 1));
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Yeni başvuru ekranı açılamadı.");
                return View("Form", HataModeli(YeniBasvuru(), 1, "Yeni başvuru ekranı açılamadı."));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Duzenle(int id, int bolum = 1)
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                    return RedirectToAction("Index", "Home");

                Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullaniciId);
                if (!sonuc.Basarili || sonuc.Nesne == null)
                {
                    TempData["Mesaj"] = sonuc.Hatalar.Count > 0
                        ? string.Join(" ", sonuc.Hatalar)
                        : L["Basvuru.Message.NotFound"].ToString();

                    return RedirectToAction(nameof(Index));
                }

                return View("Form", await FormViewModelHazirlaAsync(BasvuruHazirla(sonuc.Nesne), Math.Clamp(bolum, 1, 7)));
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru düzenleme ekranı açılamadı. BasvuruId: {BasvuruId}", id);
                TempData["Mesaj"] = "Başvuru kaydı okunamadı.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kaydet(BasvuruFormViewModel model)
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                    return RedirectToAction("Index", "Home");

                model.Basvuru.AktifBolum = Math.Clamp(model.AktifBolum, 1, 7);
                Sonuc<int> sonuc = model.Basvuru.AktifBolum switch
                {
                    1 => await _basvuruIsKurallari.KaydetAsama1Async(model.Basvuru, kullaniciId),
                    2 => await _basvuruIsKurallari.KaydetAsama2Async(model.Basvuru, kullaniciId),
                    3 => await _basvuruIsKurallari.KaydetAsama3Async(model.Basvuru, kullaniciId),
                    4 => await _basvuruIsKurallari.KaydetAsama4Async(model.Basvuru, kullaniciId),
                    5 => await _basvuruIsKurallari.KaydetAsama5Async(model.Basvuru, kullaniciId),
                    6 => await _basvuruIsKurallari.KaydetAsama6Async(model.Basvuru, kullaniciId),
                    7 => await _basvuruIsKurallari.KaydetAsama7Async(model.Basvuru, kullaniciId),
                    _ => await _basvuruIsKurallari.KaydetAsama1Async(model.Basvuru, kullaniciId)
                };

                if (!sonuc.Basarili)
                {
                    if (AjaxIstegi())
                        return Json(new { basarili = false, mesaj = HataMesaji(sonuc, "Başvuru kaydedilemedi."), hatalar = sonuc.Hatalar });

                    model.Hatalar = sonuc.Hatalar;
                    model.Basvuru = BasvuruHazirla(model.Basvuru);
                    await ReferansListeleriYukleAsync(model);
                    return View("Form", model);
                }

                Log(LogLevel.Information, BMYEventID.Yok, null, "Başvuru kaydedildi. BasvuruId: {BasvuruId}", sonuc.Nesne);
                TempData["Mesaj"] = L["Basvuru.Message.Saved"].ToString();

                if (AjaxIstegi())
                {
                    return Json(new
                    {
                        basarili = true,
                        mesaj = L["Basvuru.Message.Saved"].ToString(),
                        id = sonuc.Nesne,
                        bolum = model.AktifBolum,
                        url = Url.Action(nameof(Duzenle), new { id = sonuc.Nesne, bolum = model.AktifBolum })
                    });
                }

                return RedirectToAction(nameof(Duzenle), new { id = sonuc.Nesne, bolum = model.AktifBolum });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Başvuru kaydet action tamamlanamadı.");
                if (AjaxIstegi())
                    return Json(new { basarili = false, mesaj = "Başvuru kaydedilemedi." });

                model.Hatalar = new List<string> { "Başvuru kaydedilemedi." };
                model.Basvuru = BasvuruHazirla(model.Basvuru);
                await ReferansListeleriYukleAsync(model);
                return View("Form", model);
            }
        }

        private bool AjaxIstegi()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                Request.Headers["Accept"].Any(x => x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        }

        [HttpGet]
        public async Task<IActionResult> DegerZinciriAsamalari(int degerZinciriId)
        {
            try
            {
                Sonuc<List<DegerZinciriAsama>> sonuc = await _basvuruIsKurallari.DegerZinciriAsamalariListeleAsync(degerZinciriId);
                if (!sonuc.Basarili)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.Hatalar) });

                return Json(new
                {
                    basarili = true,
                    asamalar = (sonuc.Nesne ?? new List<DegerZinciriAsama>())
                        .OrderBy(a => a.SiraNo)
                        .ThenBy(a => a.Ad)
                        .Select(a => new
                        {
                            a.Id,
                            a.Ad,
                            a.Aciklama
                        })
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Değer zinciri aşamaları action tamamlanamadı. DegerZinciriId: {DegerZinciriId}", degerZinciriId);
                return Json(new { basarili = false, mesaj = "Değer zinciri aşamaları listelenemedi." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UygulamaAdresiKaydet(BasvuruUygulamaAdresi adres)
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc<BasvuruUygulamaAdresi> sonuc = await _basvuruIsKurallari.UygulamaAdresiKaydetAsync(adres, kullaniciId);
                if (!sonuc.Basarili || sonuc.Nesne == null)
                    return Json(new { basarili = false, mesaj = HataMesaji(sonuc, "Uygulama adresi kaydedilemedi.") });

                return Json(new
                {
                    basarili = true,
                    mesaj = "Uygulama adresi kaydedildi.",
                    adres = UygulamaAdresiJson(sonuc.Nesne)
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Uygulama adresi kaydet action tamamlanamadı.");
                return Json(new { basarili = false, mesaj = "Uygulama adresi kaydedilemedi." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UygulamaAdresiSil(int basvuruId, int adresId)
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc sonuc = await _basvuruIsKurallari.UygulamaAdresiSilAsync(basvuruId, adresId, kullaniciId);
                if (!sonuc.Basarili)
                    return Json(new { basarili = false, mesaj = HataMesaji(sonuc, "Uygulama adresi silinemedi.") });

                return Json(new
                {
                    basarili = true,
                    mesaj = "Uygulama adresi silindi."
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Uygulama adresi sil action tamamlanamadı.");
                return Json(new { basarili = false, mesaj = "Uygulama adresi silinemedi." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FirmaSorgula(string vergiKimlikNo)
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc<Firma> sonuc = await _basvuruIsKurallari.FirmaVergiNoIleOkuAsync(vergiKimlikNo, kullaniciId);
                if (!sonuc.Basarili)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.Hatalar) });

                if (sonuc.Nesne == null)
                    return Json(new { basarili = true, bulundu = false, mesaj = L["Basvuru.Firma.NotFound"].ToString() });

                Firma firma = sonuc.Nesne;
                return Json(new
                {
                    basarili = true,
                    bulundu = true,
                    firma = FirmaJson(firma)
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Firma sorgula action tamamlanamadı.");
                return Json(new { basarili = false, mesaj = "Firma sorgulanamadı." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FirmaEkle(Firma firma)
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc<int> sonuc = await _basvuruIsKurallari.FirmaEkleAsync(firma, kullaniciId);
                if (!sonuc.Basarili)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.Hatalar) });

                firma.Id = sonuc.Nesne;

                return Json(new
                {
                    basarili = true,
                    firma = FirmaJson(firma)
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Firma ekle action tamamlanamadı.");
                return Json(new { basarili = false, mesaj = "Firma kaydedilemedi." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FirmaGuncelle(Firma firma)
        {
            try
            {
                int kullaniciId = OturumKullaniciId();
                if (kullaniciId <= 0)
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc<Firma> sonuc = await _basvuruIsKurallari.FirmaGuncelleAsync(firma, kullaniciId);
                if (!sonuc.Basarili || sonuc.Nesne == null)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.Hatalar) });

                return Json(new
                {
                    basarili = true,
                    firma = FirmaJson(sonuc.Nesne)
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Firma güncelle action tamamlanamadı.");
                return Json(new { basarili = false, mesaj = "Firma güncellenemedi." });
            }
        }

        private int OturumKullaniciId()
        {
            return OrtakFonksiyonlar.Int32Yap(HttpContext.Session.GetString("KULLANICI_ID"));
        }

        private static object FirmaJson(Firma firma)
        {
            return new
            {
                firma.Id,
                firma.VergiKimlikNo,
                firma.TicaretUnvani,
                firma.TicaretSicilNo,
                KurulusTarihi = firma.KurulusTarihi?.ToString("yyyy-MM-dd"),
                firma.MersisNo,
                firma.NaceKodu,
                firma.WebSitesi,
                firma.Telefon,
                firma.KepAdresi,
                firma.Eposta,
                firma.FaaliyetKonusu,
                firma.Adres,
                Basvuranlar = firma.Basvuranlar.Select(b => new
                {
                    b.KullaniciId,
                    b.AdSoyad,
                    b.Eposta,
                    b.Telefon,
                    b.Aktif,
                    IliskiTarihi = b.IliskiTarihi.ToString("yyyy-MM-dd HH:mm")
                }).ToList()
            };
        }

        private static object UygulamaAdresiJson(BasvuruUygulamaAdresi adres)
        {
            return new
            {
                adres.Id,
                adres.BasvuruId,
                adres.SiraNo,
                adres.IlceId,
                adres.IlId,
                adres.IlKod,
                adres.IlAdi,
                adres.IlceAdi,
                adres.TamAdres,
                YatirimYeriStatusu = adres.YatirimYeriStatusu.HasValue ? (int)adres.YatirimYeriStatusu.Value : (int?)null,
                adres.KiraVeyaTahsisSuresi,
                KiraTahsisBitisTarihi = adres.KiraTahsisBitisTarihi?.ToString("yyyy-MM-dd"),
                YapiRuhsatiDurumu = adres.YapiRuhsatiDurumu.HasValue ? (int)adres.YapiRuhsatiDurumu.Value : (int?)null
            };
        }

        private async Task<BasvuruFormViewModel> FormViewModelHazirlaAsync(Basvuru basvuru, int aktifBolum)
        {
            BasvuruFormViewModel model = new BasvuruFormViewModel
            {
                Basvuru = basvuru,
                AktifBolum = aktifBolum
            };

            await ReferansListeleriYukleAsync(model);
            return model;
        }

        private async Task<List<Donem>> DonemleriOkuAsync()
        {
            Sonuc<List<Donem>> sonuc = await _basvuruIsKurallari.DonemleriListeleAsync();
            return sonuc.Nesne ?? new List<Donem>();
        }

        private async Task<List<Il>> IlleriOkuAsync()
        {
            Sonuc<List<Il>> sonuc = await _basvuruIsKurallari.IlleriListeleAsync();
            return sonuc.Nesne ?? new List<Il>();
        }

        private async Task<List<Ilce>> IlceleriOkuAsync(int? ilId)
        {
            Sonuc<List<Ilce>> sonuc = await _basvuruIsKurallari.IlceleriListeleAsync(ilId);
            return sonuc.Nesne ?? new List<Ilce>();
        }

        private async Task<List<DegerZinciri>> DegerZincirleriniOkuAsync(int? ilId, bool asamalariYukle = false)
        {
            Sonuc<List<DegerZinciri>> sonuc = await _basvuruIsKurallari.DegerZincirleriListeleAsync(ilId, asamalariYukle);
            return sonuc.Nesne ?? new List<DegerZinciri>();
        }

        private async Task<List<DegerZinciriAsama>> DegerZinciriAsamalariniOkuAsync(int? degerZinciriId)
        {
            Sonuc<List<DegerZinciriAsama>> sonuc = await _basvuruIsKurallari.DegerZinciriAsamalariListeleAsync(degerZinciriId.GetValueOrDefault());
            return sonuc.Nesne ?? new List<DegerZinciriAsama>();
        }

        private async Task ReferansListeleriYukleAsync(BasvuruFormViewModel model)
        {
            model.Donemler = await DonemleriOkuAsync();
            model.Iller = await IlleriOkuAsync();
            model.Ilceler = await IlceleriOkuAsync(model.Basvuru.IlId);
            model.DegerZincirleri = await DegerZincirleriniOkuAsync(model.Basvuru.IlId);

            bool kayitliZincirGecerli = model.Basvuru.DegerZinciriId.HasValue &&
                model.DegerZincirleri.Any(z => z.Id == model.Basvuru.DegerZinciriId.Value);

            model.SeciliDegerZinciriId = kayitliZincirGecerli
                ? model.Basvuru.DegerZinciriId
                : model.DegerZincirleri.FirstOrDefault()?.Id;

            if (!kayitliZincirGecerli)
                model.Basvuru.DegerZinciriAsamalari = new List<string>();

            model.SeciliDegerZinciriAsamalari = await DegerZinciriAsamalariniOkuAsync(model.SeciliDegerZinciriId);
        }

        private static string HataMesaji(Sonuc sonuc, string varsayilanMesaj)
        {
            return sonuc.Hatalar.Count > 0
                ? string.Join(" ", sonuc.Hatalar)
                : varsayilanMesaj;
        }

        private static BasvuruFormViewModel HataModeli(Basvuru basvuru, int aktifBolum, string mesaj)
        {
            return new BasvuruFormViewModel
            {
                Basvuru = BasvuruHazirla(basvuru),
                AktifBolum = aktifBolum,
                Hatalar = new List<string> { mesaj }
            };
        }

        private static Basvuru YeniBasvuru()
        {
            return BasvuruHazirla(new Basvuru());
        }

        private static Basvuru BasvuruHazirla(Basvuru basvuru)
        {
            if (string.IsNullOrWhiteSpace(basvuru.Durum) ||
                string.Equals(basvuru.Durum, "Taslak", StringComparison.OrdinalIgnoreCase))
            {
                basvuru.Durum = Basvuru.OnBasvuruDurumu;
            }

            if (basvuru.Firma != null)
            {
                basvuru.FirmaId = basvuru.Firma.Id;
                basvuru.TicaretUnvani = basvuru.Firma.TicaretUnvani;
                basvuru.VergiKimlikNo = basvuru.Firma.VergiKimlikNo;
            }

            if (basvuru.Donem != null)
            {
                basvuru.DonemId = basvuru.Donem.Id;
                basvuru.BasvuruDonemi = basvuru.Donem.Ad;
            }

            if (basvuru.Il != null)
            {
                basvuru.IlId = basvuru.Il.Id;
                basvuru.IlAdi = basvuru.Il.Ad;
            }

            basvuru.YatirimAdresSayisi = basvuru.YatirimAdresleri.Count;

            return basvuru;
        }
    }
}

