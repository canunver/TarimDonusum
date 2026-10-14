using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers;

/// <summary>Tanımlar – tam başvuru "A. Belge / Form Envanteri ve Ön Kontrol" satırları.</summary>
[OturumKontrol]
public class TamBasvuruBelgeKontrolController : BMYController
{
    private readonly BasvuruIsKurallari _basvuru;
    private readonly TanimIsKurallari _tanim;

    public TamBasvuruBelgeKontrolController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer, BasvuruIsKurallari basvuru, TanimIsKurallari tanim)
        : base(loggerFactory, localizer)
    {
        _basvuru = basvuru;
        _tanim = tanim;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuru);
        return kullanici?.Yetkiler.Any(x => x.Rol == KullaniciRol.SistemYoneticisi) == true ? View() : Forbid();
    }

    [HttpGet]
    public async Task<IActionResult> Listele() =>
        Json(await _tanim.TamBasvuruBelgeKontrolleriListeleAsync(await OturumKullanicisiOkuAsync(_basvuru)));

    [HttpPost]
    public async Task<IActionResult> Kaydet([FromBody] TamBasvuruBelgeKontrolTanim tanim) =>
        Json(await _tanim.TamBasvuruBelgeKontrolKaydetAsync(tanim, await OturumKullanicisiOkuAsync(_basvuru)));
}
