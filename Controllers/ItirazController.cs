using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Net;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Servisler;

namespace TarimDonusum.Controllers;

public class ItirazController : BMYController
{
    private readonly BasvuruIsKurallari _basvuru;
    private readonly IMailServisi _mail;
    public ItirazController(ILoggerFactory loggerFactory,IStringLocalizer<SharedResource> localizer,BasvuruIsKurallari basvuru,IMailServisi mail):base(loggerFactory,localizer){_basvuru=basvuru;_mail=mail;}

    [OturumKontrol][HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        Kullanici? kullanici=await OturumKullanicisiOkuAsync(_basvuru); if(kullanici==null)return RedirectToAction("Index","Home");
        Sonuc<Basvuru> basvuru=await _basvuru.OkuAsync(id,kullanici); if(!basvuru.basarili||basvuru.nesne==null)return RedirectToAction("Index",BasvuruKullanicisiMi(kullanici)?"Basvuru":"Denetleme");
        Sonuc<List<OnBasvuruItiraz>> tarihce=await _basvuru.OnBasvuruItirazTarihcesiOkuAsync(id,kullanici);
        return View(new OnBasvuruItirazViewModel{Basvuru=basvuru.nesne,Tarihce=tarihce.nesne??[],UzmanGorunumu=!BasvuruKullanicisiMi(kullanici)});
    }

    [OturumKontrol][HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Gonder([FromBody] OnBasvuruItirazKayitModel model)
    {
        Kullanici? kullanici=await OturumKullanicisiOkuAsync(_basvuru); if(kullanici==null)return Unauthorized();
        Sonuc sonuc=await _basvuru.OnBasvuruItirazEtAsync(model,kullanici);
        if(sonuc.basarili) await MailAtAsync(model,true,false);
        return Json(sonuc);
    }

    [OturumKontrol][HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Karar([FromForm] OnBasvuruItirazKayitModel model, IFormFile? taranmisYazi)
    {
        Kullanici? kullanici=await OturumKullanicisiOkuAsync(_basvuru); if(kullanici==null)return Unauthorized(); if(BasvuruKullanicisiMi(kullanici))return Forbid();
        byte[] icerik=[];
        if(taranmisYazi is { Length:>0 }) { await using MemoryStream ms=new(); await taranmisYazi.CopyToAsync(ms); icerik=ms.ToArray(); }
        Sonuc<int> sonuc=await _basvuru.OnBasvuruItirazKarariKaydetAsync(model,kullanici,taranmisYazi?.FileName??"",icerik);
        if(sonuc.basarili) await MailAtAsync(model,false,model.RevizyonaGonder);
        return Json(sonuc);
    }

    private async Task MailAtAsync(OnBasvuruItirazKayitModel model,bool uzmana,bool revizyon)
    {
        try {
            Sonuc<OnBasvuruBildirimBilgisi> bilgi=await _basvuru.OnBasvuruBildirimBilgisiOkuAsync(model.BasvuruId); if(!bilgi.basarili)return;
            List<string> adresler=uzmana
                ? await _basvuru.ItirazUzmanEpostalariOkuAsync(model.BasvuruId)
                : await _basvuru.ItirazBasvuranEpostalariOkuAsync(model.BasvuruId);
            string no=string.IsNullOrWhiteSpace(bilgi.nesne.BasvuruNo)?$"#{model.BasvuruId}":bilgi.nesne.BasvuruNo;
            string metin=WebUtility.HtmlEncode(model.Metin.Trim()).Replace("\r\n","<br>").Replace("\n","<br>");
            string konu=uzmana?$"Ön Başvuru Red Kararınıza İtiraz Edildi - {no}":revizyon?$"Ön Başvuru İtirazı Revizyona Gönderildi - {no}":$"Ön Başvuru İtirazı Reddedildi - {no}";
            string govde=uzmana?$"<p><strong>{WebUtility.HtmlEncode(no)}</strong> numaralı ön başvuru için itiraz iletilmiştir.</p><p><strong>İtiraz metni:</strong><br>{metin}</p><p>İtiraz ekranından inceleyebilirsiniz.</p>":revizyon?$"<p>Sayın {WebUtility.HtmlEncode(bilgi.nesne.FirmaUnvani)},</p><p><strong>{WebUtility.HtmlEncode(no)}</strong> numaralı ön başvurunuza ilişkin itiraz değerlendirilmiş ve başvurunuz revizyon için tarafınıza iade edilmiştir. Düzeltmeleri yaptıktan sonra ön başvurunuzu tekrar incelemeye gönderebilirsiniz.</p><p><strong>Uzman gerekçesi:</strong><br>{metin}</p>":$"<p>Sayın {WebUtility.HtmlEncode(bilgi.nesne.FirmaUnvani)},</p><p><strong>{WebUtility.HtmlEncode(no)}</strong> numaralı ön başvurunuza ilişkin itiraz reddedilmiştir.</p><p><strong>Uzman gerekçesi:</strong><br>{metin}</p><p>Ön başvuru itiraz ekranından yeniden itiraz edebilirsiniz.</p>";
            if(adresler.Count>0){string alicilar=string.Join(";",adresler.Distinct(StringComparer.OrdinalIgnoreCase));Log(LogLevel.Information,BMYEventID.Yok,null,"İtiraz maili gönderiliyor. BasvuruId: {BasvuruId}, Alıcılar: {Alicilar}, Tür: {Tur}",model.BasvuruId,alicilar,uzmana?"Uzmana":"Başvurana");string hata=await _mail.MailAtAsync("",alicilar,konu,govde,true,false);if(!string.IsNullOrWhiteSpace(hata))Log(LogLevel.Error,BMYEventID.Yok,null,"İtiraz maili gönderilemedi. BasvuruId: {BasvuruId}, Alıcılar: {Alicilar}, Hata: {Hata}",model.BasvuruId,alicilar,hata);else Log(LogLevel.Information,BMYEventID.Yok,null,"İtiraz maili gönderildi. BasvuruId: {BasvuruId}, Alıcılar: {Alicilar}",model.BasvuruId,alicilar);}
            else Log(LogLevel.Warning,BMYEventID.Yok,null,"İtiraz maili için geçerli alıcı bulunamadı. BasvuruId: {BasvuruId}, Tür: {Tur}",model.BasvuruId,uzmana?"Uzmana":"Başvurana");
        } catch(Exception ex){Log(LogLevel.Error,BMYEventID.Yok,ex,"İtiraz bildirimi hazırlanamadı. BasvuruId: {BasvuruId}",model.BasvuruId);}
    }

    private static bool BasvuruKullanicisiMi(Kullanici? k)=>k?.Yetkiler.Any(x=>x.Rol==KullaniciRol.BasvuruKullanicisi)==true;
}
