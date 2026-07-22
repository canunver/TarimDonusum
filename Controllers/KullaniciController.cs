using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.Servisler;

namespace TarimDonusum.Controllers
{
    [OturumKontrol]
    public class KullaniciController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuru;
        private readonly KullaniciIsKurallari _kullanici;
        private readonly TanimIsKurallari _tanim;
        private readonly IMailServisi _mail;

        public KullaniciController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuru, KullaniciIsKurallari kullanici, TanimIsKurallari tanim, IMailServisi mail)
            : base(loggerFactory, localizer) { _basvuru = basvuru; _kullanici = kullanici; _tanim = tanim; _mail = mail; }

        [HttpGet] public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> SayfaVerisi()
        {
            Kullanici? oturum = await OturumKullanicisiOkuAsync(_basvuru);
            Sonuc<List<Birim>> birimler = await _tanim.BirimleriListeleAsync(oturum);
            var kullaniciTipleri = Enum.GetValues<KullaniciRol>()
                .Select(tip => new
                {
                    value = (int)tip,
                    text = L[$"Kullanici.Type.{tip}"].ToString(),
                    birimGerekli = tip == KullaniciRol.BirimKullanicisi,
                    varsayilan = tip == KullaniciRol.BirimKullanicisi
                });
            var roller = Enum.GetValues<KullaniciIslemRolu>()
                .Where(rol => rol != KullaniciIslemRolu.Yok)
                .Select(rol => new
                {
                    value = (int)rol,
                    text = L[$"Kullanici.Role.{rol}"].ToString()
                });
            Sonuc<object> sonuc = new()
            {
                nesne = new
                {
                    birimler = birimler.nesne ?? new List<Birim>(),
                    kullaniciTipleri,
                    roller
                }
            };
            foreach (string hata in birimler.hatalar) sonuc.HataEkle(hata);
            return Json(sonuc);
        }

        [HttpGet]
        public async Task<IActionResult> Ara([FromQuery] KullaniciArama arama)
            => Json(await _kullanici.KullanicilariAraAsync(arama, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpPost]
        public async Task<IActionResult> Kaydet([FromBody] KullaniciKayit kayit)
            => Json(await _kullanici.KullaniciKaydetAsync(kayit, await OturumKullanicisiOkuAsync(_basvuru)));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ParolaBaglantisiGonder([FromBody] Kullanici model)
        {
            Sonuc<ParolaBaglantisiSonucu> sonuc = await _kullanici.ParolaBaglantisiOlusturAsync(
                model.Id, await OturumKullanicisiOkuAsync(_basvuru));
            if (!sonuc.basarili || sonuc.nesne == null) return Json(sonuc);

            string link = Url.Action("ParolaBelirle", "Home", new { token = sonuc.nesne.Token }, Request.Scheme) ?? "";
            string mailHatasi = await _mail.MailAtAsync("", sonuc.nesne.Eposta,
                L["Kullanici.PasswordLink.MailSubject"],
                string.Format(L["Kullanici.PasswordLink.MailBody"], sonuc.nesne.AdSoyad, link), true, false);
            if (!string.IsNullOrWhiteSpace(mailHatasi)) sonuc.HataEkle(mailHatasi);
            Sonuc cevap = new();
            foreach (string hata in sonuc.hatalar) cevap.HataEkle(hata);
            if (cevap.basarili) cevap.mesaj = L["Kullanici.PasswordLink.Sent"];
            return Json(cevap);
        }
    }
}
