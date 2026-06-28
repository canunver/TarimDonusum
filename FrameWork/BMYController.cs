using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using TarimDonusum.FrameWork.Logging;

namespace TarimDonusum.FrameWork
{
    public abstract class BMYController : Controller
    {
        protected ILogger Logger { get; }
        protected IStringLocalizer<SharedResource> L { get; }

        protected BMYController(ILoggerFactory loggerFactory, IStringLocalizer<SharedResource> localizer)
        {
            Logger = loggerFactory.CreateLogger(GetType());
            L = localizer;
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