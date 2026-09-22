using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Net;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Servisler;
using TarimDonusum.ViewModels.Basvuru;

namespace TarimDonusum.Controllers
{
    public class DenetlemeController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuruIsKurallari;
        private readonly IMailServisi _mailServisi;

        public DenetlemeController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuruIsKurallari,
            IMailServisi mailServisi)
            : base(loggerFactory, localizer)
        {
            _basvuruIsKurallari = basvuruIsKurallari;
            _mailServisi = mailServisi;
        }

        [OturumKontrol]
        public async Task<IActionResult> Index()
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (BasvuruKullanicisiMi(kullanici))
                    return Forbid();

                Sonuc<List<Basvuru>> sonuc = await _basvuruIsKurallari.TumVersiyonlariListeleAsync();
                if (!sonuc.basarili)
                    TempData["Mesaj"] = HataMesaji(sonuc, "Başvuru kayıtları listelenemedi.");

                return View(sonuc.nesne ?? new List<Basvuru>());
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Denetleme liste ekranı açılamadı.");
                TempData["Mesaj"] = "Başvuru kayıtları listelenemedi.";
                return View(new List<Basvuru>());
            }
        }

        [OturumKontrol]
        public async Task<IActionResult> Basvuru(int id, int bolum = 1)
        {
            try
            {
                Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
                if (BasvuruKullanicisiMi(kullanici))
                    return Forbid();

                Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id);
                if (!sonuc.basarili || sonuc.nesne == null)
                {
                    TempData["Mesaj"] = HataMesaji(sonuc, "Başvuru kaydı okunamadı.");
                    return RedirectToAction(nameof(Index));
                }
                await _basvuruIsKurallari.DenetimListeleriniIlkDegerleAsync(sonuc.nesne);

                Sonuc<List<Donem>> donemSonuc = await _basvuruIsKurallari.DonemleriListeleAsync();
                Sonuc<List<Il>> ilSonuc = await _basvuruIsKurallari.IlleriListeleAsync();
                Sonuc<List<Ilce>> ilceSonuc = await _basvuruIsKurallari.IlceleriListeleAsync(sonuc.nesne.basvuruFirma.il.id);
                //Sonuc<List<DegerZinciri>> degerZinciriSonuc = await _basvuruIsKurallari.DegerZincirleriListeleAsync(sonuc.nesne.IlId.Value, 1);
                //List<DegerZinciri> degerZincirleri = degerZinciriSonuc.nesne ?? new List<DegerZinciri>();
                //bool kayitliZincirGecerli = sonuc.nesne.yatirim.degerZinciriId.HasValue &&
                    //degerZincirleri.Any(z => z.id == sonuc.nesne.yatirim.degerZinciriId.Value);
                //int? seciliDegerZinciriId = kayitliZincirGecerli
                //    ? sonuc.nesne.yatirim.degerZinciriId
                //    : degerZincirleri.FirstOrDefault()?.id;
                //Sonuc<List<DegerZinciriAsama>> asamaSonuc = await _basvuruIsKurallari.DegerZinciriAsamalariListeleAsync(seciliDegerZinciriId.GetValueOrDefault());
                //if (!kayitliZincirGecerli)
                //    sonuc.nesne.yatirim.degerZinciriAsamalari = new List<DegerZinciriAsama>();

                return View(new BasvuruFormViewModel
                {
                    Basvuru = sonuc.nesne,
                    SaltOkunur = true,
                    DenetciGorunumu = true,
                    Donemler = donemSonuc.nesne ?? new List<Donem>(),
                    Iller = ilSonuc.nesne ?? new List<Il>(),
                    Ilceler = ilceSonuc.nesne ?? new List<Ilce>(),
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Denetleme başvuru ekranı açılamadı. BasvuruId: {BasvuruId}", id);
                TempData["Mesaj"] = "Başvuru kaydı okunamadı.";
                return RedirectToAction(nameof(Index));
            }
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakineUzmanKaydet([FromBody] BasvuruMakineUzmanKayitModel model)
        {
            Kullanici? kullanici=await OturumKullanicisiOkuAsync(_basvuruIsKurallari);if(kullanici==null)return Unauthorized();if(BasvuruKullanicisiMi(kullanici))return Forbid();return Json(await _basvuruIsKurallari.BasvuruMakineUzmanKaydetAsync(model,kullanici));
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasvuruOzetiKurumKaydet([FromBody] BasvuruOzetiKurum model)
        {
            Kullanici? kullanici=await OturumKullanicisiOkuAsync(_basvuruIsKurallari);if(kullanici==null)return Unauthorized();if(BasvuruKullanicisiMi(kullanici))return Forbid();return Json(await _basvuruIsKurallari.BasvuruOzetiKurumKaydetAsync(model,kullanici));
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakineUzmanDokumaniKaydet([FromBody] BasvuruMakineUzmanDokuman model)
        {
            Kullanici? kullanici=await OturumKullanicisiOkuAsync(_basvuruIsKurallari);if(kullanici==null)return Unauthorized();if(BasvuruKullanicisiMi(kullanici))return Forbid();return Json(await _basvuruIsKurallari.BasvuruMakineUzmanDokumaniKaydetAsync(model,kullanici));
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnBasvuruDenetimiKaydet([FromBody] Basvuru denetim, [FromQuery] bool sonuclandir = false)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });
            if (BasvuruKullanicisiMi(kullanici))
                return Forbid();

            Sonuc sonuc = await _basvuruIsKurallari.OnBasvuruDenetimiKaydetAsync(denetim, kullanici, sonuclandir);
            if (sonuclandir && sonuc.basarili && denetim.DenetimSonucu.HasValue)
                await OnBasvuruKararBildirimiGonderAsync(denetim, sonuc);
            return Json(sonuc);
        }

        private async Task OnBasvuruKararBildirimiGonderAsync(Basvuru denetim, Sonuc islemSonucu)
        {
            Sonuc<OnBasvuruBildirimBilgisi> bilgiSonucu =
                await _basvuruIsKurallari.OnBasvuruBildirimBilgisiOkuAsync(denetim.Id);
            if (!bilgiSonucu.basarili || bilgiSonucu.nesne.EpostaAdresleri.Count == 0)
            {
                islemSonucu.mesaj += " Ancak bildirim gönderilecek geçerli e-posta adresi bulunamadı.";
                Log(LogLevel.Warning, BMYEventID.Yok, null,
                    "Ön başvuru karar bildirimi için alıcı bulunamadı. BasvuruId: {BasvuruId}", denetim.Id);
                return;
            }

            OnBasvuruBildirimBilgisi bilgi = bilgiSonucu.nesne;
            string basvuru = string.IsNullOrWhiteSpace(bilgi.BasvuruNo) ? $"#{denetim.Id}" : bilgi.BasvuruNo;
            string gerekce = WebUtility.HtmlEncode(denetim.DenetimGerekcesi?.Trim() ?? "").Replace("\r\n", "<br>").Replace("\n", "<br>");
            (string konu, string govde) = denetim.DenetimSonucu.Value switch
            {
                enumOnBasvuruDenetimSonucu.KabulEdildi =>
                    ($"Ön Başvurunuz Kabul Edildi - {basvuru}",
                     $"<p>Sayın {WebUtility.HtmlEncode(bilgi.FirmaUnvani)},</p><p><strong>{WebUtility.HtmlEncode(basvuru)}</strong> numaralı ön başvurunuzun uzman incelemesi tamamlanmış ve ön başvurunuz <strong>kabul edilmiştir</strong>.</p><p>Tam başvuru kaydınız oluşturulmuştur. Başvuru sistemine giriş yaparak sürece devam edebilirsiniz.</p><p>Bilgilerinize sunarız.</p>"),
                enumOnBasvuruDenetimSonucu.DuzeltmeIcinIadeEdildi =>
                    ($"Ön Başvurunuz Düzeltme İçin İade Edildi - {basvuru}",
                     $"<p>Sayın {WebUtility.HtmlEncode(bilgi.FirmaUnvani)},</p><p><strong>{WebUtility.HtmlEncode(basvuru)}</strong> numaralı ön başvurunuzun uzman incelemesi tamamlanmış ve başvurunuz düzeltme yapılmak üzere tarafınıza iade edilmiştir.</p><p><strong>Düzeltme gerekçesi:</strong><br>{gerekce}</p><p>Başvuru sistemine giriş yaparak belirtilen hususları düzelttikten sonra ön başvurunuzu yeniden incelemeye sunabilirsiniz.</p><p>Bilgilerinize sunarız.</p>"),
                enumOnBasvuruDenetimSonucu.Reddedildi =>
                    ($"Ön Başvurunuz Reddedildi - {basvuru}",
                     $"<p>Sayın {WebUtility.HtmlEncode(bilgi.FirmaUnvani)},</p><p><strong>{WebUtility.HtmlEncode(basvuru)}</strong> numaralı ön başvurunuzun uzman incelemesi tamamlanmış ve ön başvurunuz <strong>reddedilmiştir</strong>.</p><p><strong>Ret gerekçesi:</strong><br>{gerekce}</p><p>Bu karara itiraz edebilirsiniz. İtirazınızı başvuru sistemindeki ön başvuru itiraz sayfası üzerinden iletebilirsiniz.</p><p>Bilgilerinize sunarız.</p>"),
                _ => ("", "")
            };
            if (string.IsNullOrWhiteSpace(konu)) return;

            string alicilar = string.Join(";", bilgi.EpostaAdresleri);
            string mailHatasi = await _mailServisi.MailAtAsync("", alicilar, konu, govde, true, false);
            if (!string.IsNullOrWhiteSpace(mailHatasi))
            {
                islemSonucu.mesaj += " Karar kaydedildi ancak e-posta bildirimi gönderilemedi.";
                Log(LogLevel.Error, BMYEventID.Yok, null,
                    "Ön başvuru karar bildirimi gönderilemedi. BasvuruId: {BasvuruId}, Alıcılar: {Alicilar}, Hata: {Hata}",
                    denetim.Id, alicilar, mailHatasi);
            }
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenetimListesiKaydet([FromBody] DenetimListesiKayit model)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null) return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });
            if (BasvuruKullanicisiMi(kullanici)) return Forbid();
            return Json(await _basvuruIsKurallari.DenetimListesiKaydetAsync(model, kullanici));
        }

        [OturumKontrol]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SistemSonuclariniYenidenUret([FromBody] DenetimListesiKayit model)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null) return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });
            if (BasvuruKullanicisiMi(kullanici)) return Forbid();
            return Json(await _basvuruIsKurallari.SistemDenetimListesiniYenidenUretAsync(model.basvuruId, kullanici));
        }
        private static bool BasvuruKullanicisiMi(Kullanici? kullanici)
        {
            return kullanici?.Yetkiler.Any(x => x.Rol == KullaniciRol.BasvuruKullanicisi) == true;
        }

        private static string HataMesaji(Sonuc sonuc, string varsayilanMesaj)
        {
            return sonuc.hatalar.Count > 0
                ? string.Join(" ", sonuc.hatalar)
                : varsayilanMesaj;
        }
    }
}
