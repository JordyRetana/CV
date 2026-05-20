using System.IO;
using System.Text.Json;

namespace CVDesktopEditor.Services
{
    public class AppConfigurationService
    {
        public const string ProductionLicenseApiBaseUrl = "https://cvdesktopeditor-license-api.onrender.com";
        public const string SupportPhone = "+506 8713-8971";
        public const string SupportEmail = "jretanamendez@gmail.com";
        public const string PurchaseMessage = "Para comprar o renovar la licencia contacta a Jordy Retana: +506 8713-8971 / jretanamendez@gmail.com.";

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
                var configuration = JsonSerializer.Deserialize<AppConfiguration>(json) ?? AppConfiguration.CreateDefault();
                if (IsLocalLicenseApiUrl(configuration.LicenseApiBaseUrl))
                {
                    configuration.LicenseApiBaseUrl = ProductionLicenseApiBaseUrl;
                    Save(configuration);
                }

                return configuration;
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

        private static bool IsLocalLicenseApiUrl(string apiUrl)
        {
            return apiUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   apiUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class AppConfiguration
    {
        public string UpdateChannel { get; set; } = "stable";
        public bool EnableBetaUpdates { get; set; }
        public bool EnableDiagnosticLogs { get; set; } = true;
        public string? LicenseKeyFingerprint { get; set; }
        public string LicenseApiBaseUrl { get; set; } = AppConfigurationService.ProductionLicenseApiBaseUrl;
        public string ThemeMode { get; set; } = "dark";

        public static AppConfiguration CreateDefault()
        {
            return new AppConfiguration();
        }
    }
}
