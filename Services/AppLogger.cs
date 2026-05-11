using System.IO;
using System.Text;

namespace CVDesktopEditor.Services
{
    public static class AppLogger
    {
        private static readonly object LockObject = new();

        public static string LogFolder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CVDesktopEditor",
            "Logs");

        public static string CurrentLogPath => Path.Combine(LogFolder, $"app-{DateTime.Now:yyyy-MM-dd}.log");

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(Exception exception, string message)
        {
            var details = new StringBuilder();
            details.AppendLine(message);
            details.AppendLine(exception.ToString());
            Write("ERROR", details.ToString());
        }

        private static void Write(string level, string message)
        {
            try
            {
                Directory.CreateDirectory(LogFolder);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";

                lock (LockObject)
                {
                    File.AppendAllText(CurrentLogPath, line);
                }
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }
}
