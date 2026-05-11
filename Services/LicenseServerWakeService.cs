using System.Net.Http;

namespace CVDesktopEditor.Services
{
    public class LicenseServerWakeService
    {
        public async Task WakeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(8)
                };

                await httpClient.GetAsync(AppConfigurationService.ProductionLicenseApiBaseUrl, cancellationToken);
                AppLogger.Info("License server wake-up request completed.");
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"License server wake-up failed: {ex.Message}");
            }
        }
    }
}
