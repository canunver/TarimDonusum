using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers;

[OturumKontrol]
public class SiralamaPuaniTanimController : BMYController
{
    private readonly BasvuruIsKurallari _basvuru;
    private readonly TanimIsKurallari _tanim;

    public SiralamaPuaniTanimController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer, BasvuruIsKurallari basvuru, TanimIsKurallari tanim)
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
        Json(await _tanim.SiralamaPuaniListeleAsync(await OturumKullanicisiOkuAsync(_basvuru)));

    [HttpPost]
    public async Task<IActionResult> KonuKaydet([FromBody] SiralamaPuaniKonu konu) =>
        Json(await _tanim.SiralamaPuaniKonuKaydetAsync(konu, await OturumKullanicisiOkuAsync(_basvuru)));

    [HttpPost]
    public async Task<IActionResult> AltKriterKaydet([FromBody] SiralamaPuaniAltKriter altKriter) =>
        Json(await _tanim.SiralamaPuaniAltKriterKaydetAsync(altKriter, await OturumKullanicisiOkuAsync(_basvuru)));

    [HttpPost]
    public async Task<IActionResult> KosulKaydet([FromBody] SiralamaPuaniKosul kosul) =>
        Json(await _tanim.SiralamaPuaniKosulKaydetAsync(kosul, await OturumKullanicisiOkuAsync(_basvuru)));

    [HttpPost]
    public async Task<IActionResult> Sil([FromBody] SiralamaPuaniSilModel model) =>
        Json(await _tanim.SiralamaPuaniSilAsync(model.tablo, model.id, await OturumKullanicisiOkuAsync(_basvuru)));
}

public class SiralamaPuaniSilModel
{
    public string tablo { get; set; } = "";
    public int id { get; set; }
}
