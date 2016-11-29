using System;
using System.IO;
using System.Reflection;
using SebWindowsClient.ConfigurationUtils;

namespace SebWindowsClient.DiagnosticsUtils
{
    public class Logger
    {
        //TODO: Change signature of methods and make singleton
        public static void AddError(string message, object eventSource, Exception exception, string details = null)
        {
            Log(Severity.Error, message, eventSource, exception, details);
        }

        public static void AddWarning(string message, object eventSource, Exception exception = null, string details = null)
        {
            Log(Severity.Warning, message, eventSource, exception, details);
        }

        public static void AddInformation(string message, object eventSource = null, Exception exception = null,
            string details = null)
        {
            Log(Severity.Information, message, eventSource, exception, details);
        }

        public static void InitLogger(string logFileDirectory = null, string logFilePath = null)
        {
            try
            {
                if (String.IsNullOrEmpty(logFileDirectory))
                {
                    logFileDirectory = SEBClientInfo.SebClientLogFileDirectory;
                    if (String.IsNullOrEmpty(logFileDirectory))
                    {
                        throw new DirectoryNotFoundException();
                    }
                }

                if (Directory.Exists(logFileDirectory) == false)
                    Directory.CreateDirectory(logFileDirectory);

                if (String.IsNullOrEmpty(logFilePath))
                {
                    logFilePath = SEBClientInfo.SebClientLogFile;
                    if (String.IsNullOrEmpty(logFilePath))
                    {
                        logFilePath = String.Format(@"{0}\{1}", logFileDirectory, SEBClientInfo.SEB_CLIENT_LOG);
                    }
                }
                LogFilePath = logFilePath;
            }
            catch (Exception)
            {
                LogFilePath = String.Format(@"{0}\{1}\{2}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SEBClientInfo.MANUFACTURER_LOCAL, SEBClientInfo.SEB_CLIENT_LOG);
            }
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logger.AddInformation(String.Format("SEB version: {0}",version));
        }

        private static string LogFilePath { get; set; }

        enum Severity
        {
            Error,
            Warning,
            Information
        }

        private static void Log(Severity severity, string message, object eventSource, Exception exception, string details = null)
        {
            try
            {
                using (var file = new StreamWriter(LogFilePath, true))
                {
                    string eventSourceString = eventSource == null ? "" : string.Format(" ({0})", eventSource);
                    string exceptionString = exception == null
                        ? ""
                        : string.Format("\n\n Exception: {0} - {1}\n{2}", exception, exception.Message,
                            exception.StackTrace);
                    string detailsString = details == null || (exception != null && details == exception.Message)
                        ? ""
                        : string.Format("\n\n{0}",details);

                    file.WriteLine("{0} [{1}]: {2}{3}{4}{5}\n", DateTime.Now.ToLocalTime(), severity, message, eventSourceString, exceptionString, detailsString);
                }
            }
            catch
            {
            }
        }
    }
}
