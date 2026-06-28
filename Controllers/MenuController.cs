using Microsoft.AspNetCore.Mvc;
using TarimDonusum.FrameWork.Menu;

namespace TarimDonusum.Controllers
{
    public class MenuController : Controller
    {
        [HttpGet]
        public IActionResult Giris()
        {
            return Json(MenuManager.MenuGiris);
        }
    }
}
