using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TarimDonusum.FrameWork.Logging;

namespace TarimDonusum.Framework
{
    public abstract class BMYController : Controller
    {
        protected ILogger Logger { get; }

        protected BMYController(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType());
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