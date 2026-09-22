using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers;

[OturumKontrol]
public sealed class CevreselSosyalAnketController(
    ILoggerFactory loggerFactory,
    IStringLocalizer<SharedResource> localizer,
    BasvuruIsKurallari basvuruIsKurallari,
    CevreselSosyalAnketYonetimIsKurallari yonetim) : BMYController(loggerFactory,localizer)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Kullanici? k=await OturumKullanicisiOkuAsync(basvuruIsKurallari);
        return k?.Yetkiler.Any(x=>x.Rol==KullaniciRol.SistemYoneticisi)==true?View():Forbid();
    }

    [HttpGet] public async Task<IActionResult> SayfaVerisi(int? surumId=null) => Json(await yonetim.SayfaVerisiAsync(await OturumKullanicisiOkuAsync(basvuruIsKurallari),surumId));
    [HttpPost] public async Task<IActionResult> TaslakOlustur() => Json(await yonetim.TaslakOlusturAsync(await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
    [HttpPost] public async Task<IActionResult> AnketAdiniKaydet([FromBody] CevreselSosyalAnketAdiModeli model) => Json(await yonetim.AnketAdiniKaydetAsync(model,await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
    [HttpPost] public async Task<IActionResult> KapsamDisiKaydet([FromBody] CevreselSosyalKapsamDisiModeli model) => Json(await yonetim.KapsamDisiKaydetAsync(model,await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
    [HttpPost] public async Task<IActionResult> BolumKaydet([FromBody] CevreselSosyalBolumKayitModeli model) => Json(await yonetim.BolumKaydetAsync(model,await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
    [HttpPost] public async Task<IActionResult> BolumSil([FromBody] CevreselSosyalBolumKayitModeli model) => Json(await yonetim.BolumSilAsync(model.id,model.anketSurumId,await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
    [HttpPost] public async Task<IActionResult> SoruKaydet([FromBody] CevreselSosyalSoruKayitModeli model) => Json(await yonetim.SoruKaydetAsync(model,await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
    [HttpPost] public async Task<IActionResult> SoruSil([FromBody] CevreselSosyalSoruKayitModeli model) => Json(await yonetim.SoruSilAsync(model.soru.id,model.anketSurumId,await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
    [HttpPost] public async Task<IActionResult> Yayinla([FromBody] CevreselSosyalAnketDuzenlemeModeli model) => Json(await yonetim.YayinlaAsync(model.id,await OturumKullanicisiOkuAsync(basvuruIsKurallari)));
}
