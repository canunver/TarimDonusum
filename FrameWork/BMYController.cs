using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using TarimDonusum.Araclar;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.IsKurallari;
using TarimDonusum.Models;

namespace TarimDonusum.FrameWork
{

    public class OturumKontrolAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Oturumdaki kullanıcı ID'sini kontrol et (Session kullanıyorsanız örn:)
            var kullaniciId = context.HttpContext.Session.GetInt32("KULLANICI_ID");

            if (kullaniciId == null || kullaniciId <= 0)
            {
                // Kullanıcı yoksa akışı kes ve Login sayfasına yönlendir
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }

            base.OnActionExecuting(context);
        }
    }

    public abstract class BMYController : Controller
    {
        protected ILogger Logger { get; }
        protected IStringLocalizer<SharedResource> L { get; }

        protected BMYController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer)
        {
            Logger = loggerFactory.CreateLogger(GetType());
            L = localizer;
        }

        protected async Task<Kullanici?> OturumKullanicisiOkuAsync(BasvuruIsKurallari _basvuruIsKurallari)
        {
            int? kullaniciId = HttpContext.Session.GetInt32("KULLANICI_ID");
            if (kullaniciId == null || kullaniciId <= 0)
                return null;

            Sonuc<Kullanici> sonuc = await _basvuruIsKurallari.KullaniciOkuAsync(kullaniciId.Value);
            if (sonuc.basarili && sonuc.nesne != null) return sonuc.nesne;
            return null;
        }

        #region Log
        protected void Log(
            LogLevel level,
            BMYEventID eventID,
            Exception? ex,
            string message,
            params object[] args)
        {
            BMYLog.Log(Logger, level, eventID, ex, message, args);
        }
        #endregion
    }
}
