using Microsoft.Extensions.Logging;
using Serilog.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TarimDonusum.FrameWork.Logging
{
    public static class BMYLog
    {
        private const string EventSource = "TarimDonusumApp";
        private const string EventLogName = "TarimDonusum";
        private static bool _eventSourceChecked = false;
        private static readonly object _eventSourceLock = new();

        public static void Log(ILogger logger, LogLevel level,
    BMYEventID eventID, Exception? ex, string message, params object[] args)
        {
            EventId id = eventID == BMYEventID.Yok
                ? default
                : new EventId((int)eventID, eventID.ToString());

            switch (level)
            {
                case LogLevel.Trace:
                    logger.LogTrace(id, ex, message, args);
                    break;

                case LogLevel.Debug:
                    logger.LogDebug(id, ex, message, args);
                    break;

                case LogLevel.Information:
                    logger.LogInformation(id, ex, message, args);
                    break;

                case LogLevel.Warning:
                    logger.LogWarning(id, ex, message, args);
                    break;

                case LogLevel.Error:
                    logger.LogError(id, ex, message, args);
                    break;

                case LogLevel.Critical:
                    logger.LogCritical(id, ex, message, args);
                    break;

                    //default:
                    //    Logger.Log(id, level, ex, message, args);
                    //    break;
            }

            if (OperatingSystem.IsWindows())
                WindowsEventaYaz(logger, level, eventID, ex, message, args);
            // Audit
            if (eventID != BMYEventID.Yok)
            {
                // TODO:
                // SIEM
                // Mail
                // vb.
            }
        }

        [SupportedOSPlatform("windows")]
        private static void WindowsEventaYaz(ILogger logger, LogLevel level, BMYEventID eventID, Exception? ex, string message, params object[] args)
        {
            // Audit
            if (eventID != BMYEventID.Yok)
            {
                try
                {
                    if (!_eventSourceChecked)
                    {
                        lock (_eventSourceLock)
                        {
                            if (!EventLog.SourceExists(EventSource))
                            {
                                EventLog.CreateEventSource(EventSource, EventLogName);
                                Console.WriteLine($"'{EventLogName}' günlüğü ve '{EventSource}' kaynağı başarıyla oluşturuldu.");
                                Console.WriteLine("Sistemin günlüğü tam algılaması için uygulamanızı yeniden başlatmanız gerekebilir.");
                            }

                            _eventSourceChecked = true;
                        }
                    }

                    using (EventLog eventLog = new EventLog())
                    {
                        eventLog.Source = EventSource;
                        EventLogEntryType entryType = level switch
                        {
                            LogLevel.Trace => EventLogEntryType.Information,
                            LogLevel.Debug => EventLogEntryType.Information,
                            LogLevel.Information => EventLogEntryType.Information,
                            LogLevel.Warning => EventLogEntryType.Warning,
                            LogLevel.Error => EventLogEntryType.Error,
                            LogLevel.Critical => EventLogEntryType.Error,
                            _ => EventLogEntryType.Information
                        };

                        string text = $@"{message}
                            EventID : {(int)eventID}
                            Olay    : {eventID}
                        ";

                        if (ex != null)
                        {
                            text += $@" İstisna: {ex}";
                        }

                        eventLog.WriteEntry(text, entryType, (int)eventID);
                    }
                }
                catch (Exception ex1)
                {
                    logger.LogWarning(ex1.Message);

                    // Event Viewer'a yazılamıyorsa
                    // uygulamayı asla durdurma.
                }
            }
        }
    }
}