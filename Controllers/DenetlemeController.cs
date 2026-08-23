using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;
using TarimDonusum.ViewModels.Basvuru;

namespace TarimDonusum.Controllers
{
    public class DenetlemeController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuruIsKurallari;

        public DenetlemeController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuruIsKurallari)
            : base(loggerFactory, localizer)
        {
            _basvuruIsKurallari = basvuruIsKurallari;
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
                _basvuruIsKurallari.DenetimListeleriniIlkDegerle(sonuc.nesne);

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
        public async Task<IActionResult> OnBasvuruDenetimiKaydet([FromBody] Basvuru denetim, [FromQuery] bool sonuclandir = false)
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Json(new { basarili = false, mesaj = "Oturum süresi doldu." });
            if (BasvuruKullanicisiMi(kullanici))
                return Forbid();

            Sonuc sonuc = await _basvuruIsKurallari.OnBasvuruDenetimiKaydetAsync(denetim, kullanici, sonuclandir);
            return Json(sonuc);
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
