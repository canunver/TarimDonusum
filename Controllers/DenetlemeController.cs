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

        public async Task<IActionResult> Index()
        {
            try
            {
                Sonuc<List<Basvuru>> sonuc = await _basvuruIsKurallari.TumunuListeleAsync();
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

        public async Task<IActionResult> Basvuru(int id, int bolum = 1)
        {
            try
            {
                Sonuc<Basvuru> sonuc = await _basvuruIsKurallari.OkuAsync(id);
                if (!sonuc.basarili || sonuc.nesne == null)
                {
                    TempData["Mesaj"] = HataMesaji(sonuc, "Başvuru kaydı okunamadı.");
                    return RedirectToAction(nameof(Index));
                }

                Sonuc<List<Donem>> donemSonuc = await _basvuruIsKurallari.DonemleriListeleAsync();
                Sonuc<List<Il>> ilSonuc = await _basvuruIsKurallari.IlleriListeleAsync();
                Sonuc<List<Ilce>> ilceSonuc = await _basvuruIsKurallari.IlceleriListeleAsync(sonuc.nesne.IlId);
                Sonuc<List<DegerZinciri>> degerZinciriSonuc = await _basvuruIsKurallari.DegerZincirleriListeleAsync(sonuc.nesne.IlId.Value, 1);
                List<DegerZinciri> degerZincirleri = degerZinciriSonuc.nesne ?? new List<DegerZinciri>();
                bool kayitliZincirGecerli = sonuc.nesne.yatirim.degerZinciriId.HasValue &&
                    degerZincirleri.Any(z => z.id == sonuc.nesne.yatirim.degerZinciriId.Value);
                int? seciliDegerZinciriId = kayitliZincirGecerli
                    ? sonuc.nesne.yatirim.degerZinciriId
                    : degerZincirleri.FirstOrDefault()?.id;
                Sonuc<List<DegerZinciriAsama>> asamaSonuc = await _basvuruIsKurallari.DegerZinciriAsamalariListeleAsync(seciliDegerZinciriId.GetValueOrDefault());

                if (!kayitliZincirGecerli)
                    sonuc.nesne.yatirim.degerZinciriAsamalari = new List<DegerZinciriAsama>();

                return View(new BasvuruFormViewModel
                {
                    Basvuru = sonuc.nesne,
                    SaltOkunur = true,
                    DenetciGorunumu = true,
                    AktifBolum = Math.Clamp(bolum, 1, 8),
                    Donemler = donemSonuc.nesne ?? new List<Donem>(),
                    Iller = ilSonuc.nesne ?? new List<Il>(),
                    Ilceler = ilceSonuc.nesne ?? new List<Ilce>(),
                    DegerZincirleri = degerZincirleri,
                    SeciliDegerZinciriId = seciliDegerZinciriId,
                    SeciliDegerZinciriAsamalari = asamaSonuc.nesne ?? new List<DegerZinciriAsama>()
                });
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, BMYEventID.Yok, ex, "Denetleme başvuru ekranı açılamadı. BasvuruId: {BasvuruId}", id);
                TempData["Mesaj"] = "Başvuru kaydı okunamadı.";
                return RedirectToAction(nameof(Index));
            }
        }

        private static string HataMesaji(Sonuc sonuc, string varsayilanMesaj)
        {
            return sonuc.hatalar.Count > 0
                ? string.Join(" ", sonuc.hatalar)
                : varsayilanMesaj;
        }
    }
}
