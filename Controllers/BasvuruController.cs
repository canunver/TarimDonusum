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

        public async Task<IActionResult> Index()
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync();
                if (kullanici == null)
                {
                    if (AjaxIstegi())
                        return Json(new { basarili = false, mesaj = "Oturum süresi doldu.", redirectUrl = Url.Action("Index", "Home") });

                    return RedirectToAction("Index", "Home");
                }

                Sonuc<List<Basvuru>> sonuc = await _basvuruIsKurallari.KullaniciBasvurulariniListeleAsync(kullanici);
                if (!sonuc.basarili)
                    TempData["Mesaj"] = HataMesaji(sonuc, "Başvuru kayıtları listelenemedi.");

                return View(sonuc.nesne ?? new List<Basvuru>());
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
                Kullanici? kullanici = await OturumKullanicisiOkuAsync();
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

                return View("Form", await FormViewModelHazirlaAsync(sonuc.nesne, Math.Clamp(bolum, 1, 7)));
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
        public async Task<IActionResult> KaydetFirmaBasvuru([FromBody] BasvuruFirma model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync();
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetFirmaBasvuru(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];

                //Sonuc<int> sonuc = model.Basvuru.AktifBolum switch
                //{
                //    2 => await _basvuruIsKurallari.KaydetAsama2Async(model.Basvuru, kullanici),
                //    3 => await _basvuruIsKurallari.KaydetAsama3Async(model.Basvuru, kullanici),
                //    4 => await _basvuruIsKurallari.KaydetAsama4Async(model.Basvuru, kullanici),
                //    5 => await _basvuruIsKurallari.KaydetAsama5Async(model.Basvuru, kullanici),
                //    6 => await _basvuruIsKurallari.KaydetAsama6Async(model.Basvuru, kullanici),
                //    7 => await _basvuruIsKurallari.KaydetAsama7Async(model.Basvuru, kullanici),
                //    _ => await _basvuruIsKurallari.KaydetAsama1Async(model.Basvuru, kullanici)
                //};

                //Log(LogLevel.Information, BMYEventID.Yok, null, "Başvuru kaydedildi. BasvuruId: {BasvuruId}", sonuc.Nesne);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KaydetIrtibat([FromBody] BasvuruIletisim model)
        {
            Sonuc<int> sonuc;
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync();
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");
                sonuc = await _basvuruIsKurallari.KaydetIrtibatAsync(model, kullanici);
                if (sonuc.basarili)
                    sonuc.mesaj = L["kayitBasarili"];

                //Sonuc<int> sonuc = model.Basvuru.AktifBolum switch
                //{
                //    3 => await _basvuruIsKurallari.KaydetAsama3Async(model.Basvuru, kullanici),
                //    4 => await _basvuruIsKurallari.KaydetAsama4Async(model.Basvuru, kullanici),
                //    5 => await _basvuruIsKurallari.KaydetAsama5Async(model.Basvuru, kullanici),
                //    6 => await _basvuruIsKurallari.KaydetAsama6Async(model.Basvuru, kullanici),
                //    7 => await _basvuruIsKurallari.KaydetAsama7Async(model.Basvuru, kullanici),
                //    _ => await _basvuruIsKurallari.KaydetAsama1Async(model.Basvuru, kullanici)
                //};

                //Log(LogLevel.Information, BMYEventID.Yok, null, "Başvuru kaydedildi. BasvuruId: {BasvuruId}", sonuc.Nesne);
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
                if (!sonuc.basarili)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.hatalar) });

                return Json(new
                {
                    basarili = true,
                    asamalar = (sonuc.nesne ?? new List<DegerZinciriAsama>())
                        .OrderBy(a => a.siraNo)
                        .ThenBy(a => a.ad)
                        .Select(a => new
                        {
                            a.id,
                            a.ad,
                            a.aciklama
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
                Kullanici? kullanici = await OturumKullanicisiOkuAsync();
                if (kullanici == null)
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc<BasvuruUygulamaAdresi> sonuc = await _basvuruIsKurallari.UygulamaAdresiKaydetAsync(adres, kullanici);
                if (!sonuc.basarili || sonuc.nesne == null)
                    return Json(new { basarili = false, mesaj = HataMesaji(sonuc, "Uygulama adresi kaydedilemedi.") });

                return Json(new
                {
                    basarili = true,
                    mesaj = "Uygulama adresi kaydedildi.",
                    adres = UygulamaAdresiJson(sonuc.nesne)
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
                Kullanici? kullanici = await OturumKullanicisiOkuAsync();
                if (kullanici == null)
                    return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });

                Sonuc sonuc = await _basvuruIsKurallari.UygulamaAdresiSilAsync(basvuruId, adresId, kullanici);
                if (!sonuc.basarili)
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
                if (!sonuc.basarili)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.hatalar) });

                if (sonuc.nesne == null)
                    return Json(new { basarili = true, bulundu = false, mesaj = L["Basvuru.Firma.NotFound"].ToString() });

                Firma firma = sonuc.nesne;
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
                if (!sonuc.basarili)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.hatalar) });

                firma.Id = sonuc.nesne;

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
                if (!sonuc.basarili || sonuc.nesne == null)
                    return Json(new { basarili = false, mesaj = string.Join(" ", sonuc.hatalar) });

                return Json(new
                {
                    basarili = true,
                    firma = FirmaJson(sonuc.nesne)
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

        private async Task<Kullanici?> OturumKullanicisiOkuAsync()
        {
            int kullaniciId = OturumKullaniciId();
            if (kullaniciId <= 0)
                return null;

            Sonuc<Kullanici> sonuc = await _basvuruIsKurallari.KullaniciOkuAsync(kullaniciId);
            return sonuc.basarili ? sonuc.nesne : null;
        }

        private static object FirmaJson(Firma firma)
        {
            return new
            {
                firma.Id,
                firma.vergiKimlikNo,
                firma.ticaretUnvani,
                firma.ticaretSicilNo,
                KurulusTarihi = firma.kurulusTarihi?.ToString("yyyy-MM-dd"),
                firma.mersisNo,
                firma.naceKodu,
                firma.webSitesi,
                firma.telefon,
                firma.kepAdresi,
                firma.eposta,
                firma.faaliyetKonusu,
                firma.adres,
                Basvuranlar = firma.basvuranlar.Select(b => new
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

        private async Task<Sonuc<List<DegerZinciriAsama>>> DegerZinciriAsamalariniOkuAsync(int? degerZinciriId)
        {
            Sonuc<List<DegerZinciriAsama>> sonuc = await _basvuruIsKurallari.DegerZinciriAsamalariListeleAsync(degerZinciriId.GetValueOrDefault());
            return sonuc;
        }

        [HttpGet]
        public async Task<IActionResult> DegerZinciriAsamalariListele(int zincirId, int basvuruId)
        {
            Sonuc<List<DegerZinciriAsama>> sonuc = new Sonuc<List<DegerZinciriAsama>>();
            sonuc.nesne.Add(new DegerZinciriAsama() { id = 33, ad = "eee", aciklama = "zzzz1" });
            sonuc.nesne.Add(new DegerZinciriAsama() { id = 34, ad = "fff", aciklama = "zzzz2", secili = true });
            sonuc.nesne.Add(new DegerZinciriAsama() { id = 35, ad = "gggg", aciklama = "zzzz3" });
            return Json(sonuc);
        }

        [HttpGet]
        public async Task<IActionResult> DegerZincirleriListele(int ilId, int basvuruId)
        {
            var degerZincirleri = await _basvuruIsKurallari.DegerZincirleriListeleAsync(ilId, basvuruId);
            return Json(degerZincirleri);
        }

        private async Task ReferansListeleriYukleAsync(BasvuruFormViewModel model)
        {
            model.Donemler = (await DonemleriOkuAsync()).nesne;
            model.Iller = (await IlleriOkuAsync()).nesne;
            model.Ilceler = (await IlceleriOkuAsync(model.Basvuru.IlId)).nesne;

            bool kayitliZincirGecerli = model.Basvuru.yatirim.degerZinciriId.HasValue &&
                model.DegerZincirleri.Any(z => z.id == model.Basvuru.yatirim.degerZinciriId.Value);

            model.SeciliDegerZinciriId = kayitliZincirGecerli
                ? model.Basvuru.yatirim.degerZinciriId
                : model.DegerZincirleri.FirstOrDefault()?.id;

            if (!kayitliZincirGecerli)
                model.Basvuru.yatirim.degerZinciriAsamalari = new List<DegerZinciriAsama>();

            model.SeciliDegerZinciriAsamalari = (await DegerZinciriAsamalariniOkuAsync(model.SeciliDegerZinciriId)).nesne;
        }

        private static string HataMesaji(Sonuc sonuc, string varsayilanMesaj)
        {
            return sonuc.hatalar.Count > 0
                ? string.Join(" ", sonuc.hatalar)
                : varsayilanMesaj;
        }

        private static BasvuruFormViewModel HataModeli(Basvuru basvuru, int aktifBolum, string mesaj)
        {
            return new BasvuruFormViewModel
            {
                Basvuru = basvuru,
                AktifBolum = aktifBolum,
                Hatalar = new List<string> { mesaj }
            };
        }

        private static Basvuru YeniBasvuru()
        {
            return new Basvuru();
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

    string[] yatirimYeriStatusleri = Items("Basvuru.Options.SiteStatuses");
    string[] yapiRuhsatiDurumlari = Items("Basvuru.Options.BuildingPermitStatuses");




Mülkiyet|Kira|Tahsis|İrtifak hakkı|Organize sanayi / ihtisas alanı tahsisi|Diğer
Ownership|Lease|Allocation|Easement right|Organized industrial / specialized zone allocation|Other



@foreach (TarimDonusum.Models.BasvuruSahibiTuru item in Enum.GetValues<TarimDonusum.Models.BasvuruSahibiTuru>())
{
	<option value="@((int)item)" selected="@(b.BasvuruSahibiTuru == item)">@b.BasvuruSahibiTuruAd</option>
}



Yapı ruhsatı mevcut|Yapı ruhsatı başvurusu yapıldı|Ruhsat gerekmediğine dair yazı mevcut|Henüz temin edilmedi|Yapım işi yok
Building permit available|Building permit application submitted|Letter stating no permit is required is available|Not yet obtained|No construction work


        <div class="basvuru-actions">
            <a class="btn btn-outline-secondary" asp-controller="@(Model.DenetciGorunumu ? "Denetleme" : "Basvuru")" asp-action="Index">@L["Basvuru.Common.List"]</a>
            @if (!ro)
            {
                <button class="btn btn-primary" type="submit">@L["Basvuru.Common.Save"]</button>
            }
        </div>


                    <input name="Basvuru.BasvuruFirma.Id" value="@b.BasvuruFirma.Id" type="hidden" />
                    <input name="Basvuru.BasvuruFirma.BasvuruId" value="@b.Id" type="hidden" />
                    <input name="Basvuru.BasvuruFirma.FirmaId" id="firmaId" value="@b.FirmaId" type="hidden" />
                    <input name="Basvuru.BasvuruFirma.BasvuruDonemi" value="@b.BasvuruDonemi" type="hidden" />



            <section class="basvuru-panel @Active(4)" data-panel="4">
                <h2>@L["Basvuru.Section4.Title"]</h2>
                <div class="adres-toolbar">
                    @if (!ro)
                    {
                        <button type="button" class="btn btn-outline-primary" id="adresYeniBtn"><i class="fas fa-plus"></i> @L["Basvuru.Address.New"]</button>
                    }
                </div>
                <div class="table-responsive uygulama-adres-wrap">
                    <table class="table uygulama-adres-table">
                        <thead>
                            <tr>
                                <th>@L["Basvuru.Field.SiraNo"]</th>
                                <th>@L["Basvuru.Field.Il"]</th>
                                <th>@L["Basvuru.Field.Ilce"]</th>
                                <th>@L["Basvuru.Field.TamAdres"]</th>
                                <th>@L["Basvuru.Field.YatirimYeriStatusu"]</th>
                                <th>@L["Basvuru.Field.KiraTahsis"]</th>
                                <th>@L["Basvuru.Field.YapiRuhsatiDurumu"]</th>
                                @if (!ro)
                                {
                                    <th>@L["Basvuru.Common.Actions"]</th>
                                }
                            </tr>
                        </thead>
                        <tbody id="uygulamaAdresListesi"></tbody>
                    </table>
                </div>
            </section>

            <section class="basvuru-panel @Active(5)" data-panel="5">
                <h2>@L["Basvuru.Section5.Title"]</h2>
                <div class="form-grid">
                    <label>@L["Basvuru.Field.ToplamYatirimTutari"]<input class="money-integer" name="Basvuru.ToplamYatirimTutari" type="text" inputmode="numeric" value="@Dec(b.ToplamYatirimTutari)" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.UygunHarcamaTutari"]<input class="money-integer" name="Basvuru.UygunHarcamaTutari" type="text" inputmode="numeric" value="@Dec(b.UygunHarcamaTutari)" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.TalepEdilenDestekTutari"]<input class="money-integer" name="Basvuru.TalepEdilenDestekTutari" type="text" inputmode="numeric" value="@Dec(b.TalepEdilenDestekTutari)" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.BasvuruSahibiKatkisi"]<input class="money-integer" name="Basvuru.BasvuruSahibiKatkisi" type="text" inputmode="numeric" value="@Dec(b.BasvuruSahibiKatkisi)" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.DestekOrani"]<input name="Basvuru.DestekOrani" type="number" step="0.01" value="@Dec(b.DestekOrani)" disabled="@ro" /></label>
                </div>
                <label class="wide-label">@L["Basvuru.Field.YatiriminAmaci"]<textarea name="Basvuru.YatiriminAmaci" disabled="@ro">@b.YatiriminAmaci</textarea></label>
            </section>

            <section class="basvuru-panel @Active(6)" data-panel="6">
                <h2>@L["Basvuru.Section6.Title"]</h2>
                <div class="form-grid">
                    <label>@L["Basvuru.Field.OncekiYilNetSatis"]<input class="money-integer" name="Basvuru.OncekiYilNetSatis" type="text" inputmode="numeric" value="@Dec(b.OncekiYilNetSatis)" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.SonYilNetSatis"]<input class="money-integer" name="Basvuru.SonYilNetSatis" type="text" inputmode="numeric" value="@Dec(b.SonYilNetSatis)" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.OncekiYilAktifToplami"]<input class="money-integer" name="Basvuru.OncekiYilAktifToplami" type="text" inputmode="numeric" value="@Dec(b.OncekiYilAktifToplami)" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.SonYilAktifToplami"]<input class="money-integer" name="Basvuru.SonYilAktifToplami" type="text" inputmode="numeric" value="@Dec(b.SonYilAktifToplami)" disabled="@ro" /></label>
                </div>
            </section>

            <section class="basvuru-panel @Active(7)" data-panel="7">
                <h2>@L["Basvuru.Section6.Title"]</h2>
                <div class="form-grid">
                    <label>@L["Basvuru.Field.BelgePaketiDosyaAdi"]<input name="Basvuru.BelgePaketiDosyaAdi" value="@b.BelgePaketiDosyaAdi" disabled="@ro" /></label>
                    <label>@L["Basvuru.Field.TaahhutDosyaAdi"]<input name="Basvuru.TaahhutDosyaAdi" value="@b.TaahhutDosyaAdi" disabled="@ro" /></label>
                </div>
                <label class="wide-label">@L["Basvuru.Field.BelgeBeyani"]<textarea name="Basvuru.BelgeBeyani" disabled="@ro">@b.BelgeBeyani</textarea></label>
                <h3>@L["Basvuru.Field.BelgeGruplari"]</h3>
                <div class="check-grid">
                    @foreach (string item in belgeler)
                    {
                        <label><input type="checkbox" name="Basvuru.BelgeGruplari" value="@item" checked="@CheckedStr(b.BelgeGruplari, item)" disabled="@ro" /> @item</label>
                    }
                </div>
            </section>


            @if (Model.DenetciGorunumu)
            {
                bool maliUygun = (b.SonYilNetSatis >= 100000000 && b.SonYilNetSatis <= 4000000000) ||
                (b.SonYilAktifToplami >= 100000000 && b.SonYilAktifToplami <= 4000000000);
                <section class="basvuru-panel @Active(8)" data-panel="8">
                    <h2>@L["Basvuru.Section8.Title"]</h2>
                    <div class="metric-grid">
                        <div><span>@L["Basvuru.Common.Application"]</span><strong>@b.Id</strong></div>
                        <div><span>@L["Basvuru.Field.Durum"]</span><strong>@b.Durum</strong></div>
                        <div><span>@L["Basvuru.Field.BelgeGruplariShort"]</span><strong>@b.BelgeGruplari.Count / @belgeler.Length</strong></div>
                        <div><span>@L["Basvuru.Field.MaliKontrol"]</span><strong>@(maliUygun ? T("Basvuru.Common.Eligible") : T("Basvuru.Common.NeedsReview"))</strong></div>
                    </div>
                    <div class="alert @(maliUygun ? "alert-success" : "alert-warning")">@L["Basvuru.Warning.FinancialRange"]</div>
                    <label class="wide-label">@L["Basvuru.Field.DenetciNotu"]<textarea name="Basvuru.DenetciNotu" disabled>@b.DenetciNotu</textarea></label>
                    <label class="wide-label">@L["Basvuru.Field.DenetimSonucu"]<input name="Basvuru.DenetimSonucu" value="@b.DenetimSonucu" disabled /></label>
                </section>
            }


    var zincirler = Model.DegerZincirleri;
    bool kayitliZincirGecerli = Model.SeciliDegerZinciriId.HasValue && zincirler.Any(z => z.Id == Model.SeciliDegerZinciriId.Value);
    int seciliZincirId = kayitliZincirGecerli ? Model.SeciliDegerZinciriId!.Value : (zincirler.FirstOrDefault()?.Id ?? 0);
    List<DegerZinciriAsama> seciliAsamalar = kayitliZincirGecerli ? b.DegerZinciriAsamalari : new List<DegerZinciriAsama>();
    var seciliZincirAsamalari = Model.SeciliDegerZinciriAsamalari.OrderBy(a => a.SiraNo).ThenBy(a => a.Ad);


    string[] belgeler = Items("Basvuru.Options.DocumentGroups");

    string Active(int n) => Model.AktifBolum == n ? "active" : "";




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
        const ilkFirma = @Html.Raw(JsonSerializer.Serialize(b.BasvuruFirma.firma == null ? null : new
        {
            b.BasvuruFirma.firma.Id,
            b.BasvuruFirma.firma.VergiKimlikNo,
            b.BasvuruFirma.firma.TicaretUnvani,
            b.BasvuruFirma.firma.TicaretSicilNo,
            KurulusTarihi = b.BasvuruFirma.firma.KurulusTarihi?.ToString("yyyy-MM-dd"),
            b.BasvuruFirma.firma.MersisNo,
            b.BasvuruFirma.firma.NaceKodu,
            b.BasvuruFirma.firma.WebSitesi,
            b.BasvuruFirma.firma.Telefon,
            b.BasvuruFirma.firma.KepAdresi,
            b.BasvuruFirma.firma.Eposta,
            b.BasvuruFirma.firma.FaaliyetKonusu,
            b.BasvuruFirma.firma.Adres
        }));
        let firmaModalModu = 'ekle';
        let seciliFirma = ilkFirma;
        active = @Model.AktifBolum;
        const kayitliDegerZinciriAsamalari = @Html.Raw(JsonSerializer.Serialize(seciliAsamalar));
        const degerZinciriSelect = document.getElementById('degerZinciriSelect');
        const degerZinciriAsamaListesi = document.getElementById('degerZinciriAsamaListesi');
        const uygulamaAdresListesi = document.getElementById('uygulamaAdresListesi');
        const uygulamaAdresModalEl = document.getElementById('uygulamaAdresModal');
        const uygulamaAdresModal = modalOlustur(uygulamaAdresModalEl);
        let basvuruId = @b.Id;
        const paraLocale = @Html.Raw(JsonSerializer.Serialize(CultureInfo.CurrentUICulture.Name));
        const paraFormatter = new Intl.NumberFormat(paraLocale || undefined, { maximumFractionDigits: 0 });
        const basvuruIlAdi = @Html.Raw(JsonSerializer.Serialize(basvuruIlAdi));
        const adresBosMesaj = @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Address.Empty")));
        const adresSilOnay = @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Address.DeleteConfirm")));
        let uygulamaAdresleri = @Html.Raw(JsonSerializer.Serialize(b.YatirimAdresleri
        .Where(adres => adres.Id > 0)
        .Select((adres, index) => new
        {
            adres.Id,
            adres.BasvuruId,
            adres.SiraNo,
            adres.IlceId,
            Il = string.IsNullOrWhiteSpace(adres.IlAdi) ? basvuruIlAdi : adres.IlAdi,
            Ilce = adres.IlceAdi,
            adres.TamAdres,
            YatirimYeriStatusu = adres.YatirimYeriStatusu.HasValue ? (int)adres.YatirimYeriStatusu.Value : (int?)null,
            adres.KiraVeyaTahsisSuresi,
            KiraTahsisBitisTarihi = adres.KiraTahsisBitisTarihi?.ToString("yyyy-MM-dd"),
            YapiRuhsatiDurumu = adres.YapiRuhsatiDurumu.HasValue ? (int)adres.YapiRuhsatiDurumu.Value : (int?)null
        })));
        const ilceler = @Html.Raw(JsonSerializer.Serialize(Model.Ilceler.Select(ilce => new
        {
            ilce.Id,
            ilce.Ad
        })));


        function paraTamsayiDegeri(value) {
            let text = String(value || '').trim();
            if (!text) return '';

            const sonVirgul = text.lastIndexOf(',');
            const sonNokta = text.lastIndexOf('.');
            const sonAyrac = Math.max(sonVirgul, sonNokta);

            if (sonAyrac >= 0) {
                const sonrasi = text.slice(sonAyrac + 1).replace(/\D/g, '');
                if (sonrasi.length > 0 && sonrasi.length < 3) {
                    text = text.slice(0, sonAyrac);
                }
            }

            return text.replace(/\D/g, '');
        }

        function paraFormatla(input) {
            const temizDeger = paraTamsayiDegeri(input.value);
            input.value = temizDeger ? paraFormatter.format(Number(temizDeger)) : '';
        }

        function paraInputlariniNormalizeEt() {
            form?.querySelectorAll('.money-integer').forEach(input => {
                input.value = paraTamsayiDegeri(input.value);
            });
        }

        form?.querySelectorAll('.money-integer').forEach(input => {
            paraFormatla(input);
            input.addEventListener('focus', () => {
                input.value = paraTamsayiDegeri(input.value);
            });
            input.addEventListener('blur', () => paraFormatla(input));
        });

        if (degerZinciriSelect && degerZinciriAsamaListesi) {
            degerZinciriSelect.addEventListener('change', () => {
                degerZinciriAsamalariniYukle(secilenDegerZinciriAsamalari(), true);
            });
        }

        if (form) {
            form.addEventListener('submit', async (event) => {
                event.preventDefault();
                await basvuruKaydetAjax();
            });
        }

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
            const basvuruFirma = {
                id: deger('Basvuru.BasvuruFirma.Id'),
                basvuruId: basvuruId || deger('Basvuru.Id'),
                firmaId: deger('Basvuru.BasvuruFirma.FirmaId'),
                donemId: deger('Basvuru.BasvuruFirma.DonemId'),
                ilId: deger('Basvuru.BasvuruFirma.IlId'),
                basvuruDonemi: deger('Basvuru.BasvuruFirma.BasvuruDonemi'),
                basvuruKonusu: deger('Basvuru.BasvuruFirma.BasvuruKonusu'),
                vergiKimlikNo: deger('Basvuru.BasvuruFirma.VergiKimlikNo'),
                sonIkiYildirFaalMi: deger('Basvuru.BasvuruFirma.SonIkiYildirFaalMi'),
                basvuruSahibiTuru: deger('Basvuru.BasvuruFirma.BasvuruSahibiTuru'),
                ozelSektorPayi: deger('Basvuru.BasvuruFirma.OzelSektorPayi'),
                bagliOrtakIsletmeVarMi: deger('Basvuru.BasvuruFirma.BagliOrtakIsletmeVarMi'),
                bagliOrtakAciklama: deger('Basvuru.BasvuruFirma.BagliOrtakAciklama')
            };

            data.set('AktifBolum', '1');
            data.set('Basvuru.Id', deger('Basvuru.Id'));
            data.set('Basvuru.BasvuruFirma.Id', basvuruFirma.id);
            data.set('Basvuru.BasvuruFirma.BasvuruId', basvuruFirma.basvuruId);
            data.set('Basvuru.BasvuruFirma.FirmaId', basvuruFirma.firmaId);
            data.set('Basvuru.BasvuruFirma.DonemId', basvuruFirma.donemId);
            data.set('Basvuru.BasvuruFirma.IlId', basvuruFirma.ilId);
            data.set('Basvuru.BasvuruFirma.BasvuruDonemi', basvuruFirma.basvuruDonemi);
            data.set('Basvuru.BasvuruFirma.BasvuruKonusu', basvuruFirma.basvuruKonusu);
            data.set('Basvuru.BasvuruFirma.VergiKimlikNo', basvuruFirma.vergiKimlikNo);
            data.set('Basvuru.BasvuruFirma.SonIkiYildirFaalMi', basvuruFirma.sonIkiYildirFaalMi);
            data.set('Basvuru.BasvuruFirma.BasvuruSahibiTuru', basvuruFirma.basvuruSahibiTuru);
            data.set('Basvuru.BasvuruFirma.OzelSektorPayi', basvuruFirma.ozelSektorPayi);
            data.set('Basvuru.BasvuruFirma.BagliOrtakIsletmeVarMi', basvuruFirma.bagliOrtakIsletmeVarMi);
            data.set('Basvuru.BasvuruFirma.BagliOrtakAciklama', basvuruFirma.bagliOrtakAciklama);

            return data;
        }


        dirty = false;

        document.getElementById('stepHelpBtn')?.addEventListener('click', () => {
            const messages = stepHelpMessages[active] || [document.querySelector(`.basvuru-panel[data-panel="${active}"] h2`)?.textContent || ''];
            basvuruMesajGoster(messages.filter(Boolean).join('\n\n'));
        });

        function setFirma(firma) {
            seciliFirma = firma || null;
            if (firmaId) firmaId.value = firma.id || firma.Id || '';
            if (firmaAdi) firmaAdi.value = firma.ticaretUnvani || firma.TicaretUnvani || '';
            if (vergiInput) vergiInput.value = firma.vergiKimlikNo || firma.VergiKimlikNo || vergiInput.value;
            dirty = true;
        }

        function firmaDeger(firma, alan) {
            if (!firma) return '';
            const camel = alan.charAt(0).toLowerCase() + alan.slice(1);
            return firma[camel] || firma[alan] || '';
        }

        function modalValue(id) {
            return document.getElementById(id)?.value || '';
        }

        function modalSet(id, value) {
            const el = document.getElementById(id);
            if (el) el.value = value || '';
        }

        function firmaModalDoldur(firma) {
            modalSet('modalVergiKimlikNo', firmaDeger(firma, 'VergiKimlikNo'));
            modalSet('modalTicaretUnvani', firmaDeger(firma, 'TicaretUnvani'));
            modalSet('modalTicaretSicilNo', firmaDeger(firma, 'TicaretSicilNo'));
            modalSet('modalKurulusTarihi', firmaDeger(firma, 'KurulusTarihi'));
            modalSet('modalMersisNo', firmaDeger(firma, 'MersisNo'));
            modalSet('modalNaceKodu', firmaDeger(firma, 'NaceKodu'));
            modalSet('modalWebSitesi', firmaDeger(firma, 'WebSitesi'));
            modalSet('modalTelefon', firmaDeger(firma, 'Telefon'));
            modalSet('modalKepAdresi', firmaDeger(firma, 'KepAdresi'));
            modalSet('modalEposta', firmaDeger(firma, 'Eposta'));
            modalSet('modalFaaliyetKonusu', firmaDeger(firma, 'FaaliyetKonusu'));
            modalSet('modalAdres', firmaDeger(firma, 'Adres'));
            document.getElementById('firmaModalHata').textContent = '';
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

        function renderDegerZinciriAsamalari(asamalar, seciliAsamalar, degisti) {
            if (!degerZinciriSelect || !degerZinciriAsamaListesi) return;

            degerZinciriAsamaListesi.innerHTML = '';

            if (asamalar.length === 0) {
                if (degisti) dirty = true;
                return;
            }

            asamalar.forEach(asama => {
                const tr = document.createElement('tr');
                const secimTd = document.createElement('td');
                const adTd = document.createElement('td');
                const aciklamaTd = document.createElement('td');
                const strong = document.createElement('strong');
                const asamaAdi = asama.ad || asama.Ad || '';
                const asamaAciklama = asama.aciklama || asama.Aciklama || '';

                const input = document.createElement('input');
                input.type = 'checkbox';
                input.name = 'Basvuru.DegerZinciriAsamalari';
                input.value = asamaAdi;
                input.disabled = @(ro ? "true" : "false");
                input.checked = seciliAsamalar.includes(asamaAdi);
                input.addEventListener('change', () => dirty = true);

                secimTd.className = 'text-center';
                secimTd.appendChild(input);
                strong.textContent = asamaAdi;
                adTd.appendChild(strong);
                aciklamaTd.textContent = asamaAciklama;

                tr.appendChild(secimTd);
                tr.appendChild(adTd);
                tr.appendChild(aciklamaTd);
                degerZinciriAsamaListesi.appendChild(tr);
            });

            if (degisti) dirty = true;
        }

        function secilenDegerZinciriAsamalari() {
            if (!degerZinciriAsamaListesi) return kayitliDegerZinciriAsamalari;

            return Array
                .from(degerZinciriAsamaListesi.querySelectorAll('input[name="Basvuru.DegerZinciriAsamalari"]:checked'))
                .map(x => x.value);
        }

        function degerZinciriAsamalariniYukle(seciliAsamalar, degisti) {
            if (!degerZinciriSelect || !degerZinciriAsamaListesi) return;

            const zincirId = Number(degerZinciriSelect.value || 0);
            degerZinciriAsamaListesi.innerHTML = '';
            if (!zincirId) {
                if (degisti) dirty = true;
                return;
            }

            fetch('@Url.Action("DegerZinciriAsamalari", "Basvuru")?degerZinciriId=' + encodeURIComponent(zincirId), {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            })
                .then(response => response.json())
                .then(result => {
                    if (!result.basarili) {
                        basvuruMesajGoster(result.mesaj || 'Değer zinciri aşamaları listelenemedi.', false);
                        return;
                    }

                    renderDegerZinciriAsamalari(result.asamalar || [], seciliAsamalar || [], degisti);
                })
                .catch(() => {
                    basvuruMesajGoster('Değer zinciri aşamaları listelenemedi.', false);
                });
        }

        if (typeof PopupMesajIlklendir === 'function') {
            PopupMesajIlklendir();
        }

        function modalOlustur(element) {
            if (!element) return null;

            if (window.jQuery && typeof window.jQuery(element).modal === 'function') {
                return {
                    show: () => window.jQuery(element).modal('show'),
                    hide: () => window.jQuery(element).modal('hide')
                };
            }

            return {
                show: () => {
                    element.style.display = 'block';
                    element.classList.add('show');
                    element.removeAttribute('aria-hidden');
                },
                    hide: () => {
                        element.classList.remove('show');
                        element.style.display = 'none';
                        element.setAttribute('aria-hidden', 'true');
                        document.body.classList.remove('modal-open');
                        document.body.style.removeProperty('padding-right');
                        document.querySelectorAll('.modal-backdrop').forEach(backdrop => backdrop.remove());
                    }
                };
            }

        function adresDeger(adres, alan) {
            if (!adres) return '';
            const camel = alan.charAt(0).toLowerCase() + alan.slice(1);
            return adres[camel] || adres[alan] || '';
        }

        function adresMetni(adres) {
            const durum = adresDeger(adres, 'KiraVeyaTahsisSuresi');
            const bitis = adresDeger(adres, 'KiraTahsisBitisTarihi');
            return [durum, bitis].filter(Boolean).join(' / ');
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

        function renderUygulamaAdresleri(degisti = false) {
            if (!uygulamaAdresListesi) return;

            uygulamaAdresleri = (uygulamaAdresleri || []).map((adres, index) => ({
                id: Number(adresDeger(adres, 'Id') || 0) || 0,
                basvuruId: Number(adresDeger(adres, 'BasvuruId') || basvuruId || 0),
                siraNo: Number(adresDeger(adres, 'SiraNo') || index + 1),
                ilceId: Number(adresDeger(adres, 'IlceId') || 0) || null,
                il: adresDeger(adres, 'Il') || basvuruIlAdi,
                ilce: adresDeger(adres, 'IlceAdi') || adresDeger(adres, 'Ilce') || ilceAdiBul(adresDeger(adres, 'IlceId')),
                tamAdres: adresDeger(adres, 'TamAdres') || adresDeger(adres, 'AcikAdres'),
                yatirimYeriStatusu: Number(adresDeger(adres, 'YatirimYeriStatusu') || 0) || null,
                kiraVeyaTahsisSuresi: Number(adresDeger(adres, 'KiraVeyaTahsisSuresi') || adresDeger(adres, 'KiraTahsisDurumu') || 0) || null,
                kiraTahsisBitisTarihi: adresDeger(adres, 'KiraTahsisBitisTarihi'),
                yapiRuhsatiDurumu: Number(adresDeger(adres, 'YapiRuhsatiDurumu') || 0) || null
            })).sort((a, b) => (a.siraNo - b.siraNo) || (a.id - b.id));

            uygulamaAdresListesi.innerHTML = '';

            if (uygulamaAdresleri.length === 0) {
                const tr = document.createElement('tr');
                const td = document.createElement('td');
                td.colSpan = @(ro ? "7" : "8");
                td.className = 'text-muted text-center';
                td.textContent = adresBosMesaj;
                tr.appendChild(td);
                uygulamaAdresListesi.appendChild(tr);
                return;
            }

            uygulamaAdresleri.forEach((adres, index) => {
                const tr = document.createElement('tr');
                const alanlar = [
                    String(adres.siraNo),
                    adres.il,
                    adres.ilce,
                    adres.tamAdres,
                    "",
                    adresMetni(adres),
                    ""
                ];

                alanlar.forEach((value, alanIndex) => {
                    const td = document.createElement('td');
                    td.textContent = value || '';
                    if (alanIndex === 3) td.className = 'adres-tam-adres';
                    tr.appendChild(td);
                });

                if (@(ro ? "false" : "true")) {
                    const actionTd = document.createElement('td');
                    const editBtn = document.createElement('button');
                    const deleteBtn = document.createElement('button');

                    editBtn.type = 'button';
                    editBtn.className = 'btn btn-sm btn-outline-secondary mr-1';
                    editBtn.innerHTML = '<i class="fas fa-edit"></i>';
                    editBtn.addEventListener('click', () => adresModalAc(index));

                    deleteBtn.type = 'button';
                    deleteBtn.className = 'btn btn-sm btn-outline-danger';
                    deleteBtn.innerHTML = '<i class="fas fa-trash"></i>';
                    deleteBtn.addEventListener('click', async () => {
                        if (!confirm(adresSilOnay)) return;
                        await adresSil(adres.id);
                    });

                    actionTd.appendChild(editBtn);
                    actionTd.appendChild(deleteBtn);
                    tr.appendChild(actionTd);
                }

                uygulamaAdresListesi.appendChild(tr);
            });

            if (degisti) dirty = true;
        }

        function adresModalAc(index) {
            const adres = index >= 0 ? uygulamaAdresleri[index] : null;
            modalSet('adresModalIndex', index >= 0 ? String(index) : '');
            modalSet('adresModalSiraNo', adresDeger(adres, 'SiraNo') || String((uygulamaAdresleri.length || 0) + 1));
            modalSet('adresModalIl', adresDeger(adres, 'Il') || basvuruIlAdi);
            modalSet('adresModalIlce', adresDeger(adres, 'IlceId'));
            modalSet('adresModalTamAdres', adresDeger(adres, 'TamAdres') || adresDeger(adres, 'AcikAdres'));
            modalSet('adresModalYatirimYeriStatusu', adresDeger(adres, 'YatirimYeriStatusu'));
            modalSet('adresModalKiraVeyaTahsisSuresi', adresDeger(adres, 'KiraVeyaTahsisSuresi') || adresDeger(adres, 'KiraTahsisDurumu'));
            modalSet('adresModalKiraTahsisBitisTarihi', adresDeger(adres, 'KiraTahsisBitisTarihi'));
            modalSet('adresModalYapiRuhsatiDurumu', adresDeger(adres, 'YapiRuhsatiDurumu'));
            uygulamaAdresModal?.show();
        }

        renderUygulamaAdresleri(false);

        document.getElementById('adresYeniBtn')?.addEventListener('click', () => adresModalAc(-1));
        document.querySelectorAll('.uygulama-adres-modal-kapat').forEach(btn => {
            btn.addEventListener('click', () => uygulamaAdresModal?.hide());
        });
        document.getElementById('adresKaydetBtn')?.addEventListener('click', async () => {
            const indexText = modalValue('adresModalIndex');
            const mevcutAdres = indexText === '' ? null : uygulamaAdresleri[Number(indexText)];
            const seciliIlceId = Number(modalValue('adresModalIlce') || 0) || null;
            const adres = {
                id: mevcutAdres?.id || 0,
                basvuruId,
                siraNo: Number(modalValue('adresModalSiraNo') || 0) || 1,
                il: basvuruIlAdi,
                ilceId: seciliIlceId,
                ilce: ilceAdiBul(seciliIlceId),
                tamAdres: modalValue('adresModalTamAdres'),
                yatirimYeriStatusu: Number(modalValue('adresModalYatirimYeriStatusu') || 0) || null,
                kiraVeyaTahsisSuresi: Number(modalValue('adresModalKiraVeyaTahsisSuresi') || 0) || null,
                kiraTahsisBitisTarihi: modalValue('adresModalKiraTahsisBitisTarihi'),
                yapiRuhsatiDurumu: Number(modalValue('adresModalYapiRuhsatiDurumu') || 0) || null
            };

            if (!adres.kiraTahsisBitisTarihi) {
                basvuruMesajGoster('Kira/tahsis bitiş tarihi girilmelidir.', false);
                return;
            }

            await adresKaydet(adres);
        });

        async function adresKaydet(adres) {
            const token = antiForgeryToken();
            const body = new URLSearchParams();
            body.set('__RequestVerificationToken', token);
            body.set('Id', adres.id || '');
            body.set('BasvuruId', adres.basvuruId || '');
            body.set('SiraNo', adres.siraNo || '1');
            body.set('IlceId', adres.ilceId || '');
            body.set('TamAdres', adres.tamAdres || '');
            body.set('YatirimYeriStatusu', adres.yatirimYeriStatusu || '');
            body.set('KiraVeyaTahsisSuresi', adres.kiraVeyaTahsisSuresi || '');
            body.set('KiraTahsisBitisTarihi', adres.kiraTahsisBitisTarihi || '');
            body.set('YapiRuhsatiDurumu', adres.yapiRuhsatiDurumu || '');

            const response = await fetch('@Url.Action("UygulamaAdresiKaydet", "Basvuru")', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': token
                },
                body
            });
            const result = await response.json();
            if (!result.basarili) {
                basvuruMesajGoster(result.mesaj || 'Uygulama adresi kaydedilemedi.', false);
                return;
            }

            const kayitliAdres = result.adres;
            const adresId = Number(kayitliAdres.id || kayitliAdres.Id || 0);
            const index = uygulamaAdresleri.findIndex(x => Number(x.id || x.Id || 0) === adresId);
            if (index >= 0) {
                uygulamaAdresleri[index] = kayitliAdres;
            } else {
                uygulamaAdresleri.push(kayitliAdres);
            }

            renderUygulamaAdresleri(false);
            uygulamaAdresModal?.hide();
            basvuruMesajGoster(result.mesaj || 'Uygulama adresi kaydedildi.');
        }

        async function adresSil(adresId) {
            if (!adresId) return;

            const token = antiForgeryToken();
            const body = new URLSearchParams();
            body.set('__RequestVerificationToken', token);
            body.set('basvuruId', String(basvuruId));
            body.set('adresId', String(adresId));

            const response = await fetch('@Url.Action("UygulamaAdresiSil", "Basvuru")', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': token
                },
                body
            });
            const result = await response.json();
            if (!result.basarili) {
                basvuruMesajGoster(result.mesaj || 'Uygulama adresi silinemedi.', false);
                return;
            }

            uygulamaAdresleri = uygulamaAdresleri.filter(x => Number(x.id || x.Id || 0) !== Number(adresId));
            renderUygulamaAdresleri(false);
            basvuruMesajGoster(result.mesaj || 'Uygulama adresi silindi.');
        }

        document.getElementById('firmaSorgulaBtn')?.addEventListener('click', async () => {
            const vergiNo = vergiInput?.value?.trim() || '';
            if (!vergiNo) {
                alert(@Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.TaxRequired"))));
                return;
            }

            const response = await fetch('@Url.Action("FirmaSorgula", "Basvuru")?vergiKimlikNo=' + encodeURIComponent(vergiNo));
            const result = await response.json();
            if (!result.basarili) {
                alert(result.mesaj || @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.QueryFailed"))));
                return;
            }

            if (result.bulundu) {
                setFirma(result.firma);
                return;
            }

            if (confirm(result.mesaj || @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.CreateQuestion"))))) {
                firmaModalModu = 'ekle';
                firmaModalDoldur({ vergiKimlikNo: vergiNo });
                firmaModal?.show();
            }
        });

        document.getElementById('firmaYeniBtn')?.addEventListener('click', () => {
            firmaModalModu = 'ekle';
            firmaModalDoldur({ vergiKimlikNo: vergiInput?.value?.trim() || '' });
            firmaModal?.show();
        });

        document.querySelectorAll('.firma-modal-kapat').forEach(btn => {
            btn.addEventListener('click', () => firmaModal?.hide());
        });

        document.getElementById('firmaDuzenleBtn')?.addEventListener('click', () => {
            const id = firmaId?.value || '';
            if (!id) {
                alert(@Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.SelectFirst"))));
                return;
            }

            firmaModalModu = 'duzenle';
            firmaModalDoldur(seciliFirma || {
                id,
                vergiKimlikNo: vergiInput?.value?.trim() || '',
                ticaretUnvani: firmaAdi?.value || ''
            });
            firmaModal?.show();
        });

        document.getElementById('firmaKaydetBtn')?.addEventListener('click', async () => {
            const token = antiForgeryToken();
            if (!token) {
                alert(@Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.SaveFailed"))));
                return;
            }

            const body = new URLSearchParams();
            body.set('__RequestVerificationToken', token);
            body.set('Id', firmaModalModu === 'duzenle' ? (firmaId?.value || '') : '');
            body.set('VergiKimlikNo', modalValue('modalVergiKimlikNo'));
            body.set('TicaretUnvani', modalValue('modalTicaretUnvani'));
            body.set('TicaretSicilNo', modalValue('modalTicaretSicilNo'));
            body.set('KurulusTarihi', modalValue('modalKurulusTarihi'));
            body.set('MersisNo', modalValue('modalMersisNo'));
            body.set('NaceKodu', modalValue('modalNaceKodu'));
            body.set('WebSitesi', modalValue('modalWebSitesi'));
            body.set('Telefon', modalValue('modalTelefon'));
            body.set('KepAdresi', modalValue('modalKepAdresi'));
            body.set('Eposta', modalValue('modalEposta'));
            body.set('FaaliyetKonusu', modalValue('modalFaaliyetKonusu'));
            body.set('Adres', modalValue('modalAdres'));

            const url = firmaModalModu === 'duzenle'
                ? '@Url.Action("FirmaGuncelle", "Basvuru")'
                : '@Url.Action("FirmaEkle", "Basvuru")';

            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': token
                },
                body
            });

            if (!response.ok) {
                document.getElementById('firmaModalHata').textContent = @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.SaveFailed"))) + ' (' + response.status + ')';
                return;
            }

            const result = await response.json();
            if (!result.basarili) {
                document.getElementById('firmaModalHata').textContent = result.mesaj || @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.SaveFailed")));
                return;
            }

            setFirma(result.firma);
            firmaModal?.hide();
            basvuruMesajGoster(firmaModalModu === 'duzenle'
                ? @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.Updated")))
                : @Html.Raw(JsonSerializer.Serialize(T("Basvuru.Firma.Saved"))));
        });
    });


*/
