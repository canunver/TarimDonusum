using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Raporlar;

namespace TarimDonusum.Controllers;

/// <summary>Yerinde Kontrol menüsü (DB 1201 kontrol listesi); yalnızca uzmanlar görür.</summary>
[OturumKontrol]
public class YerindeKontrolController : BMYController
{
    private readonly BasvuruIsKurallari _basvuru;
    private readonly YerindeKontrolIsKurallari _yerindeKontrol;
    private readonly IWebHostEnvironment _environment;

    public YerindeKontrolController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer, BasvuruIsKurallari basvuru, YerindeKontrolIsKurallari yerindeKontrol,
        IWebHostEnvironment environment)
        : base(loggerFactory, localizer)
    {
        _basvuru = basvuru;
        _yerindeKontrol = yerindeKontrol;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        if (!YerindeKontrolIsKurallari.UzmanMi(kullanici)) return Forbid();
        Sonuc<List<YerindeKontrolListeSatiri>> sonuc = await _yerindeKontrol.ListeleAsync(kullanici);
        if (!sonuc.basarili) TempData["Mesaj"] = sonuc.hataStr;
        return View(sonuc.nesne ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Baslat(int basvuruId) =>
        Json(await _yerindeKontrol.BaslatAsync(basvuruId, await OturumKullanicisiOkuAsync(_basvuru)));

    [HttpGet]
    public async Task<IActionResult> Duzenle(int id, string bolum = "GB")
    {
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        if (!YerindeKontrolIsKurallari.UzmanMi(kullanici)) return Forbid();
        Sonuc<YerindeKontrolFormModel> sonuc = await _yerindeKontrol.OkuAsync(id, kullanici);
        if (!sonuc.basarili || sonuc.nesne == null)
        {
            TempData["Mesaj"] = sonuc.hataStr;
            return RedirectToAction(nameof(Index));
        }
        sonuc.nesne.bolum = YerindeKontrolBolumleri.Tum.Any(x => x.Kod == bolum) ? bolum : "GB";
        return View(sonuc.nesne);
    }

    /// <summary>Seçilen bölümün sayfasını DB 1201 şablonundaki biçimiyle tek sayfalık Excel olarak indirir.</summary>
    [HttpGet]
    public async Task<IActionResult> Yazdir(int id, string bolum = "GB")
    {
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        if (!YerindeKontrolIsKurallari.UzmanMi(kullanici)) return Forbid();
        Sonuc<YerindeKontrolFormModel> sonuc = await _yerindeKontrol.OkuAsync(id, kullanici);
        if (!sonuc.basarili || sonuc.nesne == null)
        {
            TempData["Mesaj"] = sonuc.hataStr;
            return RedirectToAction(nameof(Index));
        }
        try
        {
            RaporDosyasi dosya = new RPYK_YerindeKontrol(_environment.ContentRootPath).Olustur(sonuc.nesne, bolum);
            return File(dosya.Icerik, RaporDosyasi.ExcelMimeTuru, dosya.DosyaAdi);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Yerinde kontrol Excel çıktısı oluşturulamadı. Id: {Id}, Bolum: {Bolum}", id, bolum);
            TempData["Mesaj"] = "Excel dosyası oluşturulamadı.";
            return RedirectToAction(nameof(Duzenle), new { id, bolum });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumKaydet([FromBody] YerindeKontrolBolumKayit model) =>
        Json(await _yerindeKontrol.BolumKaydetAsync(model, await OturumKullanicisiOkuAsync(_basvuru)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenelBilgilerKaydet([FromBody] YerindeKontrol model) =>
        Json(await _yerindeKontrol.GenelBilgilerKaydetAsync(model, await OturumKullanicisiOkuAsync(_basvuru)));

    // Videolar için istek sınırı en büyük video boyutunun biraz üstündedir; tür bazındaki sınırlar iş kuralında kontrol edilir.
    private const long YuklemeSiniri = YerindeKontrolIsKurallari.VideoEnBuyuk + 10L * 1024 * 1024;

    /// <summary>Kısım 4 (tur=Belge, PDF) veya Kısım 5 (tur=Fotograf, fotoğraf / video) için tek dosya yükler.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(YuklemeSiniri)]
    [RequestFormLimits(MultipartBodyLengthLimit = YuklemeSiniri)]
    public async Task<IActionResult> DosyaYukle(int id, string tur, IFormFile? dosya)
    {
        byte[] icerik = [];
        if (dosya != null && dosya.Length > 0 && dosya.Length <= YuklemeSiniri)
        {
            await using MemoryStream stream = new();
            await dosya.CopyToAsync(stream);
            icerik = stream.ToArray();
        }
        else if (dosya != null && dosya.Length > YuklemeSiniri)
        {
            Sonuc<object> buyuk = new();
            buyuk.HataEkle($"{dosya.FileName}: Dosya çok büyük.");
            return Json(buyuk);
        }
        return Json(await _yerindeKontrol.DosyaYukleAsync(id, tur, dosya?.FileName ?? "", icerik, await OturumKullanicisiOkuAsync(_basvuru)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DosyaSil([FromBody] YerindeKontrolDosyaSil model) =>
        Json(await _yerindeKontrol.DosyaSilAsync(model, await OturumKullanicisiOkuAsync(_basvuru)));

    /// <summary>Dosyayı tarayıcıda açar (PDF, fotoğraf, video); indir=true ise indirir. Video için parça parça okuma (ileri sarma) desteklenir.</summary>
    [HttpGet]
    public async Task<IActionResult> Dosya(int id, string tur, int satirId, bool indir = false)
    {
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        if (!YerindeKontrolIsKurallari.UzmanMi(kullanici)) return Forbid();
        Sonuc<Dosya> sonuc = await _yerindeKontrol.DosyaGetirAsync(id, tur, satirId, kullanici);
        if (!sonuc.basarili || sonuc.nesne == null) return NotFound(sonuc.hataStr);

        string dosyaAdi = string.IsNullOrWhiteSpace(sonuc.nesne.DosyaAdi) ? $"dosya-{satirId}" : sonuc.nesne.DosyaAdi;
        Microsoft.Net.Http.Headers.ContentDispositionHeaderValue disposition = new(indir ? "attachment" : "inline");
        disposition.SetHttpFileName(dosyaAdi); // Türkçe karakterli adlar için filename* (UTF-8) de yazılır.
        Response.Headers.ContentDisposition = disposition.ToString();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(sonuc.nesne.Icerik, YerindeKontrolDosyaTurleri.IcerikTuru(dosyaAdi), enableRangeProcessing: true);
    }
}
