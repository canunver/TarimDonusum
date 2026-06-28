using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TarimDonusum.FrameWork;
using TarimDonusum.FrameWork.Menu;

namespace TarimDonusum.Controllers
{
    public class MenuController : BMYController
    {
        public MenuController(
            ILoggerFactory loggerFactory,
            IStringLocalizer<SharedResource> localizer)
            : base(loggerFactory, localizer)
        {
        }

        [HttpGet]
        public IActionResult Giris()
        {
            return Json(MenuManager.GetMenuGiris(L));
        }
    }
}