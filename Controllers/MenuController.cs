using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Menu;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.Controllers
{
    public class MenuController : BMYController
    {
        private readonly BasvuruIsKurallari _basvuruIsKurallari;

        public MenuController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer,
            BasvuruIsKurallari basvuruIsKurallari)
            : base(loggerFactory, localizer)
        {
            _basvuruIsKurallari = basvuruIsKurallari;
        }

        [HttpGet]
        public async Task<IActionResult> Giris()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Json(MenuManager.GetMenuGiris(L));

            return Json(MenuManager.GetMenuBasvuru(L, kullanici.Yetkiler.Select(y => y.Rol)));
        }

        [HttpGet]
        public async Task<IActionResult> Basvuru()
        {
            Kullanici? kullanici = await OturumKullanicisiOkuAsync(_basvuruIsKurallari);
            if (kullanici == null)
                return Json(MenuManager.GetMenuGiris(L));

            return Json(MenuManager.GetMenuBasvuru(L, kullanici.Yetkiler.Select(y => y.Rol)));
        }
    }
}
