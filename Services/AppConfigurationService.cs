using System.IO;
using System.Text.Json;

namespace CVDesktopEditor.Services
{
    public class AppConfigurationService
    {
        private readonly string _configPath;

        public AppConfigurationService()
        {
            var baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CVDesktopEditor");

            Directory.CreateDirectory(baseFolder);
            _configPath = Path.Combine(baseFolder, "app-settings.json");
        }

        public AppConfiguration Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    var defaults = AppConfiguration.CreateDefault();
                    Save(defaults);
                    return defaults;
                }

                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppConfiguration>(json) ?? AppConfiguration.CreateDefault();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to load app configuration.");
                return AppConfiguration.CreateDefault();
            }
        }

        public void Save(AppConfiguration configuration)
        {
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_configPath, json);
        }
    }

    public class AppConfiguration
    {
        public string UpdateChannel { get; set; } = "stable";
        public bool EnableBetaUpdates { get; set; }
        public bool EnableDiagnosticLogs { get; set; } = true;
        public string? LicenseKeyFingerprint { get; set; }
        public string LicenseApiBaseUrl { get; set; } = "http://localhost:5000";

        public static AppConfiguration CreateDefault()
        {
            return new AppConfiguration();
        }
    }
}
