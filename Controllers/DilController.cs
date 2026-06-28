using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace TarimDonusum.Controllers
{
    public class DilController: Controller
    {
        [HttpGet]
        public IActionResult Degistir(string culture, string returnUrl)
        {
            if (culture != "tr" && culture != "en")
                culture = "tr";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });

            if (string.IsNullOrWhiteSpace(returnUrl))
                returnUrl = Url.Content("~/");

            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.Content("~/");

            return LocalRedirect(returnUrl);
        }
    }
}
