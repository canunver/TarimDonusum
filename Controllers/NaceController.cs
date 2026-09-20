using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers;

[OturumKontrol]
public class NaceController : BMYController
{
    private readonly BasvuruIsKurallari _basvuru; private readonly TanimIsKurallari _tanim;
    public NaceController(ILoggerFactory f,IStringLocalizer<SharedResource> l,BasvuruIsKurallari b,TanimIsKurallari t):base(f,l){_basvuru=b;_tanim=t;}
    [HttpGet] public async Task<IActionResult> Index(){var k=await OturumKullanicisiOkuAsync(_basvuru);return k?.Yetkiler.Any(x=>x.Rol==KullaniciRol.SistemYoneticisi)==true?View():Forbid();}
    [HttpGet] public async Task<IActionResult> Ara(string? metin,bool yonetim=false)=>Json(await _tanim.NaceAraAsync(metin,await OturumKullanicisiOkuAsync(_basvuru),yonetim));
    [HttpPost] public async Task<IActionResult> Kaydet([FromBody] Nace nace)=>Json(await _tanim.NaceKaydetAsync(nace,await OturumKullanicisiOkuAsync(_basvuru)));
}
