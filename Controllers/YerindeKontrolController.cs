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
}
