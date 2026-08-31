using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers;

[OturumKontrol]
public sealed class TahminAyarlariController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer, BasvuruIsKurallari basvuruIsKurallari, TanimIsKurallari tanimIsKurallari)
    : BMYController(loggerFactory, localizer)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Kullanici? kullanici=await OturumKullanicisiOkuAsync(basvuruIsKurallari);
        if(kullanici?.Yetkiler.Any(x=>x.Rol==KullaniciRol.SistemYoneticisi)!=true)return Forbid();
        Sonuc<List<Donem>> sonuc=await tanimIsKurallari.DonemleriListeleAsync(kullanici);
        return View(sonuc.nesne??[]);
    }

    [HttpGet]
    public async Task<IActionResult> Listele(int donemId)
    {
        Kullanici? kullanici=await OturumKullanicisiOkuAsync(basvuruIsKurallari);
        return Json(await tanimIsKurallari.DonemTahminleriniOkuAsync(donemId,kullanici));
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet([FromBody] DonemTahminAyari model)
    {
        Kullanici? kullanici=await OturumKullanicisiOkuAsync(basvuruIsKurallari);
        return Json(await tanimIsKurallari.DonemTahminleriniKaydetAsync(model,kullanici));
    }
}
