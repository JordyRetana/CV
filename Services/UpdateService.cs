using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace CVDesktopEditor.Services
{
    public class UpdateService
    {
        private readonly AppConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public UpdateService(AppConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(Uri manifestUri, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(manifestUri, cancellationToken);
                var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest == null)
                    return UpdateCheckResult.Failed("Update manifest is empty.");

                var channel = _configuration.EnableBetaUpdates ? "beta" : _configuration.UpdateChannel;
                if (!manifest.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase))
                    return UpdateCheckResult.NoUpdate("Manifest channel does not match current channel.");

                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
                if (!Version.TryParse(manifest.Version, out var latestVersion))
                    return UpdateCheckResult.Failed("Update manifest version is invalid.");

                return latestVersion > currentVersion
                    ? UpdateCheckResult.Available(manifest)
                    : UpdateCheckResult.NoUpdate("Application is up to date.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Update check failed.");
                return UpdateCheckResult.Failed(ex.Message);
            }
        }
    }

    public class UpdateManifest
    {
        public string Version { get; set; } = "";
        public string Channel { get; set; } = "stable";
        public string ReleaseNotesUrl { get; set; } = "";
        public string InstallerUrl { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }

    public class UpdateCheckResult
    {
        public bool HasUpdate { get; init; }
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = "";
        public UpdateManifest? Manifest { get; init; }

        public static UpdateCheckResult Available(UpdateManifest manifest) => new()
        {
            HasUpdate = true,
            IsSuccess = true,
            Manifest = manifest,
            Message = $"Version {manifest.Version} is available."
        };

        public static UpdateCheckResult NoUpdate(string message) => new()
        {
            HasUpdate = false,
            IsSuccess = true,
            Message = message
        };

        public static UpdateCheckResult Failed(string message) => new()
        {
            HasUpdate = false,
            IsSuccess = false,
            Message = message
        };
    }
}
