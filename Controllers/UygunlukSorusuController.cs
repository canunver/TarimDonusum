using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers;

[OturumKontrol]
public class UygunlukSorusuController : BMYController
{
    private readonly BasvuruIsKurallari _basvuru;
    private readonly TanimIsKurallari _tanim;
    public UygunlukSorusuController(ILoggerFactory loggerFactory,IStringLocalizer<SharedResource> localizer,BasvuruIsKurallari basvuru,TanimIsKurallari tanim):base(loggerFactory,localizer){_basvuru=basvuru;_tanim=tanim;}
    public IActionResult Index()=>View();
    [HttpGet] public async Task<IActionResult> Listele()=>Json(await _tanim.UygunlukSorulariniListeleAsync(await OturumKullanicisiOkuAsync(_basvuru)));
    [HttpPost] public async Task<IActionResult> Kaydet([FromBody] UygunlukSorusu soru)=>Json(await _tanim.UygunlukSorusuKaydetAsync(soru,await OturumKullanicisiOkuAsync(_basvuru)));
}
