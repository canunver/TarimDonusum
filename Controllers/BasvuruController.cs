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
        public async Task<IActionResult> Index()
        {
            Sonuc<List<Basvuru>> sonuc;
            ; try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (kullanici == null)
                    return RedirectToAction("Index", "Home");

                ViewBag.YeniBasvuruYetkisi = BasvuruKullanicisiMi(kullanici);
                sonuc = await _basvuruIsKurallari.KullaniciBasvuruVersiyonlariniListeleAsync(kullanici);
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

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> ProjeButcesiYazdir(int id)
        {
            if (id <= 0)
                return BadRequest("Proje bütçesi yazdırılmadan önce başvuru kaydedilmelidir.");

            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : "Başvuru bulunamadı.");

            string sablonDosya = Path.Combine(_environment.ContentRootPath, "Sablonlar", "ProjeButcesi.xltx");
            if (!System.IO.File.Exists(sablonDosya))
                return NotFound("Proje bütçesi şablonu bulunamadı.");

            string geciciDosya = Path.Combine(Path.GetTempPath(), $"proje-butcesi-{Guid.NewGuid():N}.xlsx");

            try
            {
                Tablo tablo = OrtakFonksiyonlar.NewTablo();
                tablo.DosyaAc(sablonDosya, geciciDosya);

                Dictionary<string, decimal> tutarlar = ProjeButcesiTutarlari(sonuc.nesne.yatirimOzeti.yatirimOzetiJson);
                Dictionary<string, int> satirlar = new()
                {
                    ["A1"] = 3, ["A2"] = 4, ["A3"] = 5, ["A4"] = 6,
                    ["B1"] = 8, ["B2"] = 9, ["B3"] = 10, ["B4"] = 11,
                    ["B5"] = 12, ["B6"] = 13, ["B7"] = 14,
                    ["D"] = 16, ["E"] = 17, ["F"] = 18, ["G"] = 19
                };

                foreach ((string anahtar, int satir) in satirlar)
                    tablo.HucreDegerYaz(satir, 8, tutarlar.GetValueOrDefault(anahtar));

                tablo.HucreFormulYaz(20, 8, "ROUNDUP(SUM(I17:I20),2)");
                tablo.CalculateFormula();
                tablo.DosyaSaklaTamYol();
                tablo.DosyaKapat();

                byte[] dosyaIcerigi = System.IO.File.ReadAllBytes(geciciDosya);
                return File(
                    dosyaIcerigi,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ProjeButcesi-{id}.xlsx");
            }
            finally
            {
                if (System.IO.File.Exists(geciciDosya))
                    System.IO.File.Delete(geciciDosya);
            }
        }

        private static Dictionary<string, decimal> ProjeButcesiTutarlari(string? yatirimOzetiJson)
        {
            Dictionary<string, decimal> tutarlar = new(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(yatirimOzetiJson))
                return tutarlar;

            using JsonDocument belge = JsonDocument.Parse(yatirimOzetiJson);
            if (!belge.RootElement.TryGetProperty("investmentBudgetData", out JsonElement butce) ||
                butce.ValueKind != JsonValueKind.Object)
                return tutarlar;

            foreach (JsonProperty satir in butce.EnumerateObject())
            {
                if (satir.Value.ValueKind == JsonValueKind.Object &&
                    satir.Value.TryGetProperty("amount", out JsonElement tutar))
                    tutarlar[satir.Name] = ProjeButcesiTutari(tutar);
            }

            return tutarlar;
        }

        private static decimal ProjeButcesiTutari(JsonElement tutar)
        {
            if (tutar.ValueKind == JsonValueKind.Number && tutar.TryGetDecimal(out decimal sayi))
                return sayi;

            string metin = tutar.ValueKind == JsonValueKind.String ? tutar.GetString() ?? "" : "";
            metin = metin.Replace("TL", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (decimal.TryParse(metin, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out sayi))
                return sayi;
            return decimal.TryParse(metin, NumberStyles.Any, CultureInfo.InvariantCulture, out sayi) ? sayi : 0;
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> YatirimOzetiYazdir(int id)
        {
            if (id <= 0)
                return BadRequest("Yatırım özeti yazdırılmadan önce başvuru kaydedilmelidir.");

            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : "Başvuru bulunamadı.");

            string sablonDosya = Path.Combine(_environment.ContentRootPath, "Sablonlar", "YatirimOzeti.xltx");
            if (!System.IO.File.Exists(sablonDosya))
                return NotFound("Yatırım özeti şablonu bulunamadı.");

            string geciciDosya = Path.Combine(Path.GetTempPath(), $"yatirim-ozeti-{Guid.NewGuid():N}.xlsx");
            try
            {
                Tablo tablo = OrtakFonksiyonlar.NewTablo();
                tablo.DosyaAc(sablonDosya, geciciDosya);

                int formatSheet = -1, formatSatir1 = -1, formatSutun1 = -1, formatSatir2 = -1, formatSutun2 = -1;
                int hedefSheet = -1, hedefSatir = -1, hedefSutun = -1, hedefSatir2 = -1, hedefSutun2 = -1;
                tablo.HucreAdAdresCoz("FormatSatir", ref formatSheet, ref formatSatir1, ref formatSutun1, ref formatSatir2, ref formatSutun2);
                tablo.HucreAdAdresCoz("BaslaSatir", ref hedefSheet, ref hedefSatir, ref hedefSutun, ref hedefSatir2, ref hedefSutun2);

                if (formatSheet < 0 || hedefSheet < 0)
                    throw new InvalidOperationException("Yatırım özeti şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");

                const int urunSatirSayisi = 5;
                const int urunSutunSayisi = 14;
                formatSatir2 = Math.Max(formatSatir2, formatSatir1 + urunSatirSayisi - 1);
                formatSutun2 = Math.Max(formatSutun2, formatSutun1 + urunSutunSayisi - 1);
                tablo.AktifSheetDegistir(formatSheet);
                double formatIlkSatirYuksekligi = tablo.SatirGercekYukseklikAl(formatSatir1);

                List<YatirimOzetiUrunu> urunler = YatirimOzetiUrunleri(sonuc.nesne.yatirimOzeti.yatirimOzetiJson);
                for (int urunNo = 0; urunNo < urunler.Count; urunNo++)
                {
                    int urunBaslangicSatiri = hedefSatir + (urunNo * urunSatirSayisi);
                    tablo.HucreKopyala(
                        formatSheet, formatSatir1, formatSutun1, formatSatir2, formatSutun2,
                        hedefSheet, urunBaslangicSatiri, hedefSutun);

                    tablo.AktifSheetDegistir(hedefSheet);
                    tablo.SatirGercekYukseklikAyarla(urunBaslangicSatiri, urunBaslangicSatiri, formatIlkSatirYuksekligi);
                    YatirimOzetiUrunu urun = urunler[urunNo];
                    tablo.HucreDegerYaz(urunBaslangicSatiri, hedefSutun, urunNo + 1);
                    tablo.HucreDegerYaz(urunBaslangicSatiri, hedefSutun + 1, urun.Ad);
                    tablo.HucreDegerYaz(urunBaslangicSatiri, hedefSutun + 2, urun.Birim);

                    string[] gostergeler = ["capacity", "production", "sales", "price"];
                    for (int gostergeNo = 0; gostergeNo < gostergeler.Length; gostergeNo++)
                    {
                        List<decimal> degerler = urun.Veriler.GetValueOrDefault(gostergeler[gostergeNo]) ?? [];
                        for (int yil = 0; yil < Math.Min(11, degerler.Count); yil++)
                            tablo.HucreDegerYaz(urunBaslangicSatiri + gostergeNo + 1, hedefSutun + yil + 3, degerler[yil]);
                    }
                }

                tablo.AktifSheetDegistir(formatSheet);
                tablo.SatirSil(formatSatir1, formatSatir2);
                tablo.DosyaSaklaTamYol();
                tablo.DosyaKapat();
                byte[] dosyaIcerigi = System.IO.File.ReadAllBytes(geciciDosya);
                return File(
                    dosyaIcerigi,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"YatirimOzeti-{id}.xlsx");
            }
            finally
            {
                if (System.IO.File.Exists(geciciDosya))
                    System.IO.File.Delete(geciciDosya);
            }
        }

        private static List<YatirimOzetiUrunu> YatirimOzetiUrunleri(string? yatirimOzetiJson)
        {
            List<YatirimOzetiUrunu> urunler = [];
            if (string.IsNullOrWhiteSpace(yatirimOzetiJson))
                return urunler;

            using JsonDocument belge = JsonDocument.Parse(yatirimOzetiJson);
            if (!belge.RootElement.TryGetProperty("productionRows", out JsonElement satirlar) ||
                satirlar.ValueKind != JsonValueKind.Array)
                return urunler;

            foreach (JsonElement satir in satirlar.EnumerateArray())
            {
                string ad = satir.TryGetProperty("name", out JsonElement adElement) ? adElement.GetString() ?? "" : "";
                string birim = satir.TryGetProperty("unit", out JsonElement birimElement) ? birimElement.GetString() ?? "" : "";
                Dictionary<string, List<decimal>> veriler = new(StringComparer.OrdinalIgnoreCase);

                if (satir.TryGetProperty("data", out JsonElement veriElement) && veriElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (string gosterge in new[] { "capacity", "production", "sales", "price" })
                    {
                        List<decimal> degerler = [];
                        if (veriElement.TryGetProperty(gosterge, out JsonElement dizi) && dizi.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement deger in dizi.EnumerateArray())
                                degerler.Add(ProjeButcesiTutari(deger));
                        }
                        veriler[gosterge] = degerler;
                    }
                }

                urunler.Add(new YatirimOzetiUrunu(ad, birim, veriler));
            }

            return urunler;
        }

        private sealed record YatirimOzetiUrunu(
            string Ad,
            string Birim,
            Dictionary<string, List<decimal>> Veriler);

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> IsletmeGideriYazdir(int id)
        {
            if (id <= 0)
                return BadRequest("İşletme giderleri yazdırılmadan önce başvuru kaydedilmelidir.");

            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : "Başvuru bulunamadı.");

            string sablonDosya = Path.Combine(_environment.ContentRootPath, "Sablonlar", "IsletmeGideri.xltx");
            if (!System.IO.File.Exists(sablonDosya))
                return NotFound("İşletme gideri şablonu bulunamadı.");

            string geciciDosya = Path.Combine(Path.GetTempPath(), $"isletme-giderleri-{Guid.NewGuid():N}.xlsx");
            try
            {
                Tablo tablo = OrtakFonksiyonlar.NewTablo();
                tablo.DosyaAc(sablonDosya, geciciDosya);

                int formatSheet = -1, formatSatir = -1, formatSutun = -1, formatSatir2 = -1, formatSutun2 = -1;
                int hedefSheet = -1, hedefSatir = -1, hedefSutun = -1, hedefSatir2 = -1, hedefSutun2 = -1;
                tablo.HucreAdAdresCoz("FormatSatir", ref formatSheet, ref formatSatir, ref formatSutun, ref formatSatir2, ref formatSutun2);
                tablo.HucreAdAdresCoz("BaslaSatir", ref hedefSheet, ref hedefSatir, ref hedefSutun, ref hedefSatir2, ref hedefSutun2);
                if (formatSheet < 0 || hedefSheet < 0)
                    throw new InvalidOperationException("İşletme gideri şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");

                const int sonSutunOfseti = 11;
                tablo.AktifSheetDegistir(formatSheet);
                double[] formatYukseklikleri =
                [
                    tablo.SatirGercekYukseklikAl(formatSatir),
                    tablo.SatirGercekYukseklikAl(formatSatir + 1),
                    tablo.SatirGercekYukseklikAl(formatSatir + 2)
                ];

                List<IsletmeGideriSatiri> giderler = IsletmeGiderleri(sonuc.nesne.yatirimOzeti.yatirimOzetiJson);
                decimal genelToplam = giderler.Sum(x => x.Toplam);
                decimal genelSabit = giderler.Sum(x => x.SabitTutar);
                decimal genelDegisken = giderler.Sum(x => x.DegiskenTutar);
                int yazilacakSatir = hedefSatir;

                foreach (IGrouping<string, IsletmeGideriSatiri> grup in giderler.GroupBy(x => x.Grup))
                {
                    decimal grupToplam = grup.Sum(x => x.Toplam);
                    decimal grupSabit = grup.Sum(x => x.SabitTutar);
                    decimal grupDegisken = grup.Sum(x => x.DegiskenTutar);
                    decimal grupPayi = genelToplam == 0 ? 0 : grupToplam / genelToplam * 100;

                    IsletmeGideriFormatSatiriKopyala(tablo, formatSheet, formatSatir, formatSutun, hedefSheet, yazilacakSatir, hedefSutun, sonSutunOfseti, formatYukseklikleri[0]);
                    tablo.AktifSheetDegistir(hedefSheet);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 1, grup.Key);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 5, grupToplam);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 8, grupPayi);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 9, grupSabit);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 10, grupDegisken);
                    yazilacakSatir++;

                    foreach (IsletmeGideriSatiri gider in grup)
                    {
                        IsletmeGideriFormatSatiriKopyala(tablo, formatSheet, formatSatir + 1, formatSutun, hedefSheet, yazilacakSatir, hedefSutun, sonSutunOfseti, formatYukseklikleri[1]);
                        tablo.AktifSheetDegistir(hedefSheet);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 1, gider.Unsur);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 2, gider.Miktar);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 3, gider.Birim);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 4, gider.BirimFiyat);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 5, gider.Toplam);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 6, gider.SabitYuzde);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 7, gider.DegiskenYuzde);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 8, genelToplam == 0 ? 0 : gider.Toplam / genelToplam * 100);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 9, gider.SabitTutar);
                        tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 10, gider.DegiskenTutar);
                        yazilacakSatir++;
                    }
                }

                IsletmeGideriFormatSatiriKopyala(tablo, formatSheet, formatSatir + 2, formatSutun, hedefSheet, yazilacakSatir, hedefSutun, sonSutunOfseti, formatYukseklikleri[2]);
                tablo.AktifSheetDegistir(hedefSheet);
                tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 1, "TOPLAM İŞLETME GİDERLERİ");
                tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 5, genelToplam);
                tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 8, genelToplam == 0 ? 0 : 100);
                tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 9, genelSabit);
                tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 10, genelDegisken);

                tablo.AktifSheetDegistir(formatSheet);
                tablo.SatirSil(formatSatir, formatSatir + 2);
                tablo.DosyaSaklaTamYol();
                tablo.DosyaKapat();

                byte[] dosyaIcerigi = System.IO.File.ReadAllBytes(geciciDosya);
                return File(
                    dosyaIcerigi,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"IsletmeGiderleri-{id}.xlsx");
            }
            finally
            {
                if (System.IO.File.Exists(geciciDosya))
                    System.IO.File.Delete(geciciDosya);
            }
        }

        private static void IsletmeGideriFormatSatiriKopyala(
            Tablo tablo, int kaynakSheet, int kaynakSatir, int kaynakSutun,
            int hedefSheet, int hedefSatir, int hedefSutun, int sonSutunOfseti, double yukseklik)
        {
            tablo.HucreKopyala(
                kaynakSheet, kaynakSatir, kaynakSutun, kaynakSatir, kaynakSutun + sonSutunOfseti,
                hedefSheet, hedefSatir, hedefSutun);
            tablo.AktifSheetDegistir(hedefSheet);
            tablo.SatirGercekYukseklikAyarla(hedefSatir, hedefSatir, yukseklik);
        }

        private static List<IsletmeGideriSatiri> IsletmeGiderleri(string? yatirimOzetiJson)
        {
            List<IsletmeGideriSatiri> giderler = [];
            if (string.IsNullOrWhiteSpace(yatirimOzetiJson))
                return giderler;

            using JsonDocument belge = JsonDocument.Parse(yatirimOzetiJson);
            if (!belge.RootElement.TryGetProperty("operatingExpenseRows", out JsonElement satirlar) ||
                satirlar.ValueKind != JsonValueKind.Array)
                return giderler;

            foreach (JsonElement satir in satirlar.EnumerateArray())
            {
                string grup = JsonMetin(satir, "group");
                string unsur = JsonMetin(satir, "item");
                decimal miktar = JsonSayi(satir, "qty");
                string birim = JsonMetin(satir, "unit");
                decimal birimFiyat = JsonSayi(satir, "unitPrice");
                decimal sabitYuzde = JsonSayi(satir, "fixedPct");
                decimal degiskenYuzde = JsonSayi(satir, "variablePct");
                decimal toplam = miktar * birimFiyat;

                giderler.Add(new IsletmeGideriSatiri(
                    grup, unsur, miktar, birim, birimFiyat, toplam,
                    sabitYuzde, degiskenYuzde,
                    toplam * sabitYuzde / 100,
                    toplam * degiskenYuzde / 100));
            }

            return giderler;
        }

        private static string JsonMetin(JsonElement nesne, string alan)
        {
            return nesne.TryGetProperty(alan, out JsonElement deger) && deger.ValueKind == JsonValueKind.String
                ? deger.GetString() ?? ""
                : "";
        }

        private static decimal JsonSayi(JsonElement nesne, string alan)
        {
            return nesne.TryGetProperty(alan, out JsonElement deger) ? ProjeButcesiTutari(deger) : 0;
        }

        private sealed record IsletmeGideriSatiri(
            string Grup,
            string Unsur,
            decimal Miktar,
            string Birim,
            decimal BirimFiyat,
            decimal Toplam,
            decimal SabitYuzde,
            decimal DegiskenYuzde,
            decimal SabitTutar,
            decimal DegiskenTutar);

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> OnBasvuruTamlikYazdir(int id)
        {
            if (id <= 0)
                return BadRequest("Tamlık kontrolü yazdırılmadan önce başvuru kaydedilmelidir.");

            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : "Başvuru bulunamadı.");

            Basvuru basvuru = sonuc.nesne;
            _basvuruIsKurallari.DenetimListeleriniIlkDegerle(basvuru);
            List<TamlikKontrolSatiri> kontrolSatirlari = TamlikKontrolSatirlari(basvuru.SistemDenetimAnketi);

            string sablonDosya = Path.Combine(_environment.ContentRootPath, "Sablonlar", "OnBasvuruTamlik.xltx");
            if (!System.IO.File.Exists(sablonDosya))
                return NotFound("Ön başvuru tamlık kontrolü şablonu bulunamadı.");

            string geciciDosya = Path.Combine(Path.GetTempPath(), $"on-basvuru-tamlik-{Guid.NewGuid():N}.xlsx");
            try
            {
                Tablo tablo = OrtakFonksiyonlar.NewTablo();
                tablo.DosyaAc(sablonDosya, geciciDosya);

                int formatSheet = -1, formatSatir = -1, formatSutun = -1, formatSatir2 = -1, formatSutun2 = -1;
                int hedefSheet = -1, hedefSatir = -1, hedefSutun = -1, hedefSatir2 = -1, hedefSutun2 = -1;
                tablo.HucreAdAdresCoz("FormatSatir", ref formatSheet, ref formatSatir, ref formatSutun, ref formatSatir2, ref formatSutun2);
                tablo.HucreAdAdresCoz("BaslaSatir", ref hedefSheet, ref hedefSatir, ref hedefSutun, ref hedefSatir2, ref hedefSutun2);
                if (formatSheet < 0 || hedefSheet < 0)
                    throw new InvalidOperationException("Tamlık kontrolü şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");

                tablo.AktifSheetDegistir(formatSheet);
                int formatMinimumYuksekligi = tablo.SatirYukseklikAl(formatSatir);

                for (int index = 0; index < kontrolSatirlari.Count; index++)
                {
                    int yazilacakSatir = hedefSatir + index;
                    tablo.HucreKopyala(
                        formatSheet, formatSatir, formatSutun, formatSatir, formatSutun + 5,
                        hedefSheet, yazilacakSatir, hedefSutun);

                    tablo.AktifSheetDegistir(hedefSheet);
                    TamlikKontrolSatiri kontrol = kontrolSatirlari[index];
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun, kontrol.No);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 1, kontrol.Konu);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 2, kontrol.Soru);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 3, kontrol.Kaynak);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 4, kontrol.Sonuc);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 5, kontrol.Aciklama);
                    tablo.SatirYukseklikAyarla(yazilacakSatir, yazilacakSatir, -1, 0, formatMinimumYuksekligi);
                }

                tablo.AktifSheetDegistir(formatSheet);
                tablo.SatirSil(formatSatir, formatSatir);
                tablo.DosyaSaklaTamYol();
                tablo.DosyaKapat();

                byte[] dosyaIcerigi = System.IO.File.ReadAllBytes(geciciDosya);
                return File(
                    dosyaIcerigi,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"OnBasvuruTamlik-{id}.xlsx");
            }
            finally
            {
                if (System.IO.File.Exists(geciciDosya))
                    System.IO.File.Delete(geciciDosya);
            }
        }

        private static List<TamlikKontrolSatiri> TamlikKontrolSatirlari(string? json)
        {
            List<TamlikKontrolSatiri> satirlar = [];
            if (string.IsNullOrWhiteSpace(json))
                return satirlar;

            using JsonDocument belge = JsonDocument.Parse(json);
            if (belge.RootElement.ValueKind != JsonValueKind.Array)
                return satirlar;

            foreach (JsonElement satir in belge.RootElement.EnumerateArray())
            {
                int no = satir.TryGetProperty("no", out JsonElement noElement) && noElement.TryGetInt32(out int deger) ? deger : 0;
                string sonuc = JsonMetin(satir, "sonuc");
                satirlar.Add(new TamlikKontrolSatiri(
                    no,
                    JsonMetin(satir, "konu"),
                    JsonMetin(satir, "soru"),
                    JsonMetin(satir, "kaynak"),
                    string.Equals(sonuc, "Tam", StringComparison.OrdinalIgnoreCase) ? "E" : "H",
                    JsonMetin(satir, "aciklama")));
            }

            return satirlar;
        }

        private sealed record TamlikKontrolSatiri(
            int No,
            string Konu,
            string Soru,
            string Kaynak,
            string Sonuc,
            string Aciklama);

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> OnBasvuruUygunlukYazdir(int id)
        {
            if (id <= 0)
                return BadRequest("Uygunluk kontrolü yazdırılmadan önce başvuru kaydedilmelidir.");

            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : "Başvuru bulunamadı.");

            Basvuru basvuru = sonuc.nesne;
            _basvuruIsKurallari.DenetimListeleriniIlkDegerle(basvuru);
            List<TamlikKontrolSatiri> kontrolSatirlari = UygunlukKontrolSatirlari(basvuru.DenetimAnketi);

            string sablonDosya = Path.Combine(_environment.ContentRootPath, "Sablonlar", "OnBasvuruUygunluk.xltx");
            if (!System.IO.File.Exists(sablonDosya))
                return NotFound("Ön başvuru uygunluk kontrolü şablonu bulunamadı.");

            string geciciDosya = Path.Combine(Path.GetTempPath(), $"on-basvuru-uygunluk-{Guid.NewGuid():N}.xlsx");
            try
            {
                Tablo tablo = OrtakFonksiyonlar.NewTablo();
                tablo.DosyaAc(sablonDosya, geciciDosya);

                int formatSheet = -1, formatSatir = -1, formatSutun = -1, formatSatir2 = -1, formatSutun2 = -1;
                int hedefSheet = -1, hedefSatir = -1, hedefSutun = -1, hedefSatir2 = -1, hedefSutun2 = -1;
                tablo.HucreAdAdresCoz("FormatSatir", ref formatSheet, ref formatSatir, ref formatSutun, ref formatSatir2, ref formatSutun2);
                tablo.HucreAdAdresCoz("BaslaSatir", ref hedefSheet, ref hedefSatir, ref hedefSutun, ref hedefSatir2, ref hedefSutun2);
                if (formatSheet < 0 || hedefSheet < 0)
                    throw new InvalidOperationException("Uygunluk kontrolü şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");

                tablo.AktifSheetDegistir(formatSheet);
                int formatMinimumYuksekligi = tablo.SatirYukseklikAl(formatSatir);

                for (int index = 0; index < kontrolSatirlari.Count; index++)
                {
                    int yazilacakSatir = hedefSatir + index;
                    tablo.HucreKopyala(
                        formatSheet, formatSatir, formatSutun, formatSatir, formatSutun + 5,
                        hedefSheet, yazilacakSatir, hedefSutun);

                    tablo.AktifSheetDegistir(hedefSheet);
                    TamlikKontrolSatiri kontrol = kontrolSatirlari[index];
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun, kontrol.No);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 1, kontrol.Konu);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 2, kontrol.Soru);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 3, kontrol.Kaynak);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 4, kontrol.Sonuc);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 5, kontrol.Aciklama);
                    tablo.SatirYukseklikAyarla(yazilacakSatir, yazilacakSatir, -1, 0, formatMinimumYuksekligi);
                }

                tablo.AktifSheetDegistir(formatSheet);
                tablo.SatirSil(formatSatir, formatSatir);
                tablo.DosyaSaklaTamYol();
                tablo.DosyaKapat();

                byte[] dosyaIcerigi = System.IO.File.ReadAllBytes(geciciDosya);
                return File(
                    dosyaIcerigi,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"OnBasvuruUygunluk-{id}.xlsx");
            }
            finally
            {
                if (System.IO.File.Exists(geciciDosya))
                    System.IO.File.Delete(geciciDosya);
            }
        }

        private static List<TamlikKontrolSatiri> UygunlukKontrolSatirlari(string? json)
        {
            List<TamlikKontrolSatiri> satirlar = [];
            if (string.IsNullOrWhiteSpace(json))
                return satirlar;

            using JsonDocument belge = JsonDocument.Parse(json);
            if (belge.RootElement.ValueKind != JsonValueKind.Array)
                return satirlar;

            foreach (JsonElement satir in belge.RootElement.EnumerateArray())
            {
                int no = satir.TryGetProperty("no", out JsonElement noElement) && noElement.TryGetInt32(out int deger) ? deger : 0;
                satirlar.Add(new TamlikKontrolSatiri(
                    no,
                    JsonMetin(satir, "konu"),
                    JsonMetin(satir, "soru"),
                    JsonMetin(satir, "kaynak"),
                    JsonMetin(satir, "sonuc"),
                    JsonMetin(satir, "aciklama")));
            }

            return satirlar;
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> OnBilgilerYazdir(int id)
        {
            if (id <= 0)
                return BadRequest("Ön bilgiler yazdırılmadan önce başvuru kaydedilmelidir.");

            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : "Başvuru bulunamadı.");

            string sablonDosya = Path.Combine(_environment.ContentRootPath, "Sablonlar", "OnBilgiler.xltx");
            if (!System.IO.File.Exists(sablonDosya))
                return NotFound("Ön bilgiler şablonu bulunamadı.");

            string geciciDosya = Path.Combine(Path.GetTempPath(), $"on-bilgiler-{Guid.NewGuid():N}.xlsx");
            try
            {
                Tablo tablo = OrtakFonksiyonlar.NewTablo();
                tablo.DosyaAc(sablonDosya, geciciDosya);

                Basvuru basvuru = sonuc.nesne;
                tablo.HucreAdBulYaz("YatrimAd", basvuru.yatirim.yatirimAdi ?? "");
                tablo.HucreAdBulYaz("SahipAd", basvuru.basvuruFirma.firma.ticaretUnvani ?? "");
                tablo.HucreAdBulYaz("YatirimAmaci", basvuru.yatirim.yatiriminAmaci ?? "");

                using JsonDocument belge = JsonDocument.Parse(
                    string.IsNullOrWhiteSpace(basvuru.dbCtpTeknikProje.dbCtpTeknikProjeJson)
                        ? "{}"
                        : basvuru.dbCtpTeknikProje.dbCtpTeknikProjeJson);
                JsonElement kok = belge.RootElement;

                OnBilgiListesiYaz(tablo, kok, "existingProducts", "BaslaSatirMevcut", ["product", "capacity"]);
                OnBilgiListesiYaz(tablo, kok, "plannedProducts", "BaslaSatirUretilecek", ["product", "capacity"]);
                OnBilgiListesiYaz(tablo, kok, "inputs", "BaslaSatirGirdiler", ["input", "need"]);
                OnBilgiListesiYaz(tablo, kok, "solarRows", "BaslaSatirEnerjiSayi", ["type", "panels", "panelPower", "totalPower"]);
                OnBilgiListesiYaz(tablo, kok, "installedRows", "BaslaSatirEnerjiGuc", ["type", "power"]);

                tablo.DosyaSaklaTamYol();
                tablo.DosyaKapat();

                byte[] dosyaIcerigi = System.IO.File.ReadAllBytes(geciciDosya);
                return File(
                    dosyaIcerigi,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"OnBilgiler-{id}.xlsx");
            }
            finally
            {
                if (System.IO.File.Exists(geciciDosya))
                    System.IO.File.Delete(geciciDosya);
            }
        }

        private static void OnBilgiListesiYaz(
            Tablo tablo,
            JsonElement kok,
            string listeAlani,
            string baslangicBolgesi,
            string[] alanlar)
        {
            List<string[]> satirlar = [];
            if (kok.ValueKind == JsonValueKind.Object &&
                kok.TryGetProperty(listeAlani, out JsonElement liste) &&
                liste.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement kayit in liste.EnumerateArray())
                {
                    string[] degerler = alanlar.Select(alan => JsonDegerMetni(kayit, alan)).ToArray();
                    if (degerler.Any(deger => !string.IsNullOrWhiteSpace(deger)))
                        satirlar.Add(degerler);
                }
            }

            if (satirlar.Count == 0)
                return;

            int sheetNo = -1, baslaSatir = -1, baslaSutun = -1, satir2 = -1, sutun2 = -1;
            tablo.HucreAdAdresCoz(baslangicBolgesi, ref sheetNo, ref baslaSatir, ref baslaSutun, ref satir2, ref sutun2);
            if (sheetNo < 0)
                throw new InvalidOperationException($"Ön bilgiler şablonunda {baslangicBolgesi} isimli bölgesi bulunamadı.");

            tablo.AktifSheetDegistir(sheetNo);
            double formatYuksekligi = tablo.SatirGercekYukseklikAl(baslaSatir);

            for (int index = 0; index < satirlar.Count; index++)
            {
                int hedefSatir = baslaSatir + index;
                if (index > 0)
                {
                    tablo.SatirAc(sheetNo, hedefSatir, 1);
                    tablo.HucreKopyala(
                        sheetNo, baslaSatir, baslaSutun, baslaSatir, baslaSutun + 3,
                        sheetNo, hedefSatir, baslaSutun);
                    tablo.SatirGercekYukseklikAyarla(hedefSatir, hedefSatir, formatYuksekligi);
                }

                string[] degerler = satirlar[index];
                for (int sutun = 0; sutun < degerler.Length; sutun++)
                    tablo.HucreDegerYaz(hedefSatir, baslaSutun + sutun, degerler[sutun]);
            }
        }

        private static string JsonDegerMetni(JsonElement nesne, string alan)
        {
            if (!nesne.TryGetProperty(alan, out JsonElement deger))
                return "";

            return deger.ValueKind switch
            {
                JsonValueKind.String => deger.GetString() ?? "",
                JsonValueKind.Number => deger.GetRawText(),
                JsonValueKind.True => "Evet",
                JsonValueKind.False => "Hayır",
                _ => ""
            };
        }

        [OturumKontrol]
        [HttpGet]
        public async Task<IActionResult> MakineEkipmanYazdir(int id)
        {
            if (id <= 0)
                return BadRequest("Makine-ekipman raporu yazdırılmadan önce başvuru kaydedilmelidir.");

            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Unauthorized();

            Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id, kullanici);
            if (!sonuc.basarili || sonuc.nesne == null)
                return NotFound(sonuc.hatalar.Count > 0 ? string.Join(" ", sonuc.hatalar) : "Başvuru bulunamadı.");

            string sablonDosya = Path.Combine(_environment.ContentRootPath, "Sablonlar", "MakineEkipman.xltx");
            if (!System.IO.File.Exists(sablonDosya))
                return NotFound("Makine-ekipman şablonu bulunamadı.");

            List<MakineEkipmanSatiri> makineler = MakineEkipmanSatirlari(
                sonuc.nesne.dbCtpTeknikProje.dbCtpTeknikProjeJson);
            string geciciDosya = Path.Combine(Path.GetTempPath(), $"makine-ekipman-{Guid.NewGuid():N}.xlsx");

            try
            {
                Tablo tablo = OrtakFonksiyonlar.NewTablo();
                tablo.DosyaAc(sablonDosya, geciciDosya);

                int formatSheet = -1, formatSatir = -1, formatSutun = -1, formatSatir2 = -1, formatSutun2 = -1;
                int hedefSheet = -1, hedefSatir = -1, hedefSutun = -1, hedefSatir2 = -1, hedefSutun2 = -1;
                tablo.HucreAdAdresCoz("FormatSatir", ref formatSheet, ref formatSatir, ref formatSutun, ref formatSatir2, ref formatSutun2);
                tablo.HucreAdAdresCoz("BaslaSatir", ref hedefSheet, ref hedefSatir, ref hedefSutun, ref hedefSatir2, ref hedefSutun2);
                if (formatSheet < 0 || hedefSheet < 0)
                    throw new InvalidOperationException("Makine-ekipman şablonunda FormatSatir veya BaslaSatir isimli bölgesi bulunamadı.");

                tablo.AktifSheetDegistir(formatSheet);
                int formatMinimumYuksekligi = tablo.SatirYukseklikAl(formatSatir);

                for (int index = 0; index < makineler.Count; index++)
                {
                    int yazilacakSatir = hedefSatir + index;
                    tablo.HucreKopyala(
                        formatSheet, formatSatir, formatSutun, formatSatir, formatSutun + 9,
                        hedefSheet, yazilacakSatir, hedefSutun);

                    tablo.AktifSheetDegistir(hedefSheet);
                    MakineEkipmanSatiri makine = makineler[index];
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun, index + 1);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 1, makine.Ad);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 2, makine.Adet);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 3, makine.KullanimAmaci);
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 4, makine.Durum == "Mevcut" ? "X" : "");
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 5, makine.Durum == "Yeni" ? "X" : "");
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 6, makine.YatirimdaKullanilacak == "Evet" ? "X" : "");
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 7, makine.YatirimdaKullanilacak == "Hayır" ? "X" : "");
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 8, makine.DestekTalebi == "Evet" ? "X" : "");
                    tablo.HucreDegerYaz(yazilacakSatir, hedefSutun + 9, makine.DestekTalebi == "Hayır" ? "X" : "");
                    tablo.SatirYukseklikAyarla(yazilacakSatir, yazilacakSatir, -1, 0, formatMinimumYuksekligi);
                }

                tablo.AktifSheetDegistir(formatSheet);
                tablo.SatirSil(formatSatir, formatSatir);
                tablo.DosyaSaklaTamYol();
                tablo.DosyaKapat();

                byte[] dosyaIcerigi = System.IO.File.ReadAllBytes(geciciDosya);
                return File(
                    dosyaIcerigi,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"MakineEkipman-{id}.xlsx");
            }
            finally
            {
                if (System.IO.File.Exists(geciciDosya))
                    System.IO.File.Delete(geciciDosya);
            }
        }

        private static List<MakineEkipmanSatiri> MakineEkipmanSatirlari(string? json)
        {
            List<MakineEkipmanSatiri> satirlar = [];
            if (string.IsNullOrWhiteSpace(json))
                return satirlar;

            using JsonDocument belge = JsonDocument.Parse(json);
            if (!belge.RootElement.TryGetProperty("machineryRows", out JsonElement makineler) ||
                makineler.ValueKind != JsonValueKind.Array)
                return satirlar;

            foreach (JsonElement makine in makineler.EnumerateArray())
            {
                MakineEkipmanSatiri satir = new(
                    JsonDegerMetni(makine, "name"),
                    JsonDegerMetni(makine, "qty"),
                    JsonDegerMetni(makine, "purpose"),
                    JsonDegerMetni(makine, "assetStatus"),
                    JsonDegerMetni(makine, "useInInvestment"),
                    JsonDegerMetni(makine, "supportRequested"));
                if (!string.IsNullOrWhiteSpace(satir.Ad) || !string.IsNullOrWhiteSpace(satir.KullanimAmaci))
                    satirlar.Add(satir);
            }

            return satirlar;
        }

        private sealed record MakineEkipmanSatiri(
            string Ad,
            string Adet,
            string KullanimAmaci,
            string Durum,
            string YatirimdaKullanilacak,
            string DestekTalebi);

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
                sonuc.HataEkle("Başvuru kaydedilemedi.");
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
                basvuru.durum == enumBasvuruDurum.OnBasvuruDurumu &&
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
