using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.TestVeri;

/// <summary>
/// TEST: Rastgele test verisi üretimi. Test süreci bitince TestVeri klasörü silinir ve
/// "TEST" yorumlu tek satırlık partial çağrıları (Views/Basvuru/Bolumler/_Firma.cshtml, _UzmanSonuclari.cshtml) kaldırılır.
/// </summary>
[OturumKontrol]
public class TestVeriController : BMYController
{
    private readonly BasvuruIsKurallari _basvuru;
    private readonly IConfiguration _configuration;

    public TestVeriController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer, BasvuruIsKurallari basvuru, IConfiguration configuration)
        : base(loggerFactory, localizer)
    {
        _basvuru = basvuru;
        _configuration = configuration;
    }

    /// <summary>Sayfalar innerHTML ile yüklendiğinden düğmeler betiği tıklanınca buradan yükler.</summary>
    [HttpGet]
    public IActionResult Betik() => Content(BetikMetni, "application/javascript; charset=utf-8");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnBasvuruUret()
    {
        Sonuc<int> sonuc = new();
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        if (kullanici?.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi) != true)
        {
            sonuc.HataEkle("Test ön başvurusu yalnızca başvuru kullanıcısıyla üretilebilir.");
            return Json(sonuc);
        }

        OnBasvuruTestVeriUretici uretici = new(_basvuru, _configuration, kullanici);
        try
        {
            sonuc.nesne = await uretici.UretAsync();
            sonuc.mesaj = string.Join(" ", uretici.Notlar.Prepend("Test ön başvurusu üretildi ve kaydedildi."));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Test ön başvurusu üretilemedi. BasvuruId: {BasvuruId}", uretici.BasvuruId);
            sonuc.HataEkle(ex is TestVeriHatasi ? ex.Message : $"Beklenmeyen hata: {ex.Message}");
            sonuc.nesne = uretici.BasvuruId;
        }
        return Json(sonuc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BasvuruDoldur(int basvuruId)
    {
        Sonuc<int> sonuc = new();
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        if (kullanici?.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi) != true)
        {
            sonuc.HataEkle("Başvuru yalnızca başvuru kullanıcısıyla doldurulabilir.");
            return Json(sonuc);
        }
        try
        {
            sonuc.mesaj = await new BasvuruTestVeriUretici(_basvuru, kullanici).DoldurAsync(basvuruId);
            sonuc.nesne = basvuruId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Test başvurusu doldurulamadı. BasvuruId: {BasvuruId}", basvuruId);
            sonuc.HataEkle(ex is TestVeriHatasi ? ex.Message : $"Beklenmeyen hata: {ex.Message}");
        }
        return Json(sonuc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnBasvuruIncelemeKabul(int basvuruId)
    {
        Sonuc<int> sonuc = new();
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        if (kullanici == null || kullanici.Yetkiler.Count == 0 || kullanici.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi))
        {
            sonuc.HataEkle("İnceleme yalnızca uzman kullanıcıyla sonuçlandırılabilir.");
            return Json(sonuc);
        }
        try
        {
            sonuc.mesaj = await new OnBasvuruIncelemeTestVeriUretici(_basvuru, _configuration, kullanici).KabulEtAsync(basvuruId);
            sonuc.nesne = basvuruId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Test incelemesi sonuçlandırılamadı. BasvuruId: {BasvuruId}", basvuruId);
            sonuc.HataEkle(ex is TestVeriHatasi ? ex.Message : $"Beklenmeyen hata: {ex.Message}");
        }
        return Json(sonuc);
    }

    private const string BetikMetni = """
        window.TestVeriCalistir = async function (button) {
            if (!confirm(button.dataset.onay)) return;
            const metin = button.innerHTML;
            button.disabled = true;
            button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Çalışıyor...';
            const mesaj = (m, basarili) => typeof PopupMesajGoster === 'function' ? PopupMesajGoster(m, basarili) : alert(m);
            const ac = id => window.location.href = button.dataset.yonlendir === 'yenile' ? window.location.href : '/Basvuru/Duzenle/' + id;
            try {
                const yanit = await fetch(button.dataset.url, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' }
                });
                const sonuc = await yanit.json();
                const id = Number(sonuc?.nesne || 0);
                if (sonuc?.basarili) {
                    mesaj(sonuc.mesaj, true);
                    setTimeout(() => ac(id), 1500);
                    return;
                }
                const hata = sonuc?.hataStr || (sonuc?.hatalar || []).join(' ') || 'İşlem tamamlanamadı.';
                if (id > 0 && button.dataset.yonlendir !== 'yenile' && confirm(hata + '\n\nYarım kalan ön başvuru açılsın mı?')) { ac(id); return; }
                mesaj(hata, false);
            } catch (e) {
                mesaj('İşlem tamamlanamadı: ' + e, false);
            }
            button.disabled = false;
            button.innerHTML = metin;
        };
        """;
}
