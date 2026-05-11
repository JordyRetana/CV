using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class AdminLicenseWindow : Window
    {
        private string _lastLicenseKey = "";

        public AdminLicenseWindow()
        {
            InitializeComponent();
            var defaultExpiration = DateTime.Today.AddYears(1);
            TxtExpireDay.Text = defaultExpiration.Day.ToString("00");
            TxtExpireMonth.Text = defaultExpiration.Month.ToString("00");
            TxtExpireYear.Text = defaultExpiration.Year.ToString();
            TxtResult.Text = "Genera una licencia y envia esa clave al cliente. El instalador actual esta en artifacts\\velopack\\stable si lo creaste con Velopack.";
        }

        private async void BtnGenerateLicense_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtAdminKey.Password))
            {
                TxtResult.Text = "Pega tu ADMIN_API_KEY primero.";
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                TxtResult.Text = "Escribe el correo del cliente.";
                return;
            }

            if (!int.TryParse(TxtMaxDevices.Text, out var maxDevices) || maxDevices < 1)
                maxDevices = 1;

            if (!TryGetExpirationDate(out var expiresAt))
            {
                TxtResult.Text = "Revisa la fecha de expiracion. Usa dia, mes y ano validos.";
                return;
            }

            try
            {
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(AppConfigurationService.ProductionLicenseApiBaseUrl.TrimEnd('/') + "/"),
                    Timeout = TimeSpan.FromSeconds(45)
                };

                httpClient.DefaultRequestHeaders.Add("X-Admin-Key", TxtAdminKey.Password.Trim());

                var response = await httpClient.PostAsJsonAsync("admin/licenses", new
                {
                    Email = TxtEmail.Text.Trim(),
                    FullName = TxtFullName.Text.Trim(),
                    Kind = "premium",
                    MaxDevices = maxDevices,
                    ExpiresAt = expiresAt
                });

                var license = await response.Content.ReadFromJsonAsync<CreateLicenseResponse>();
                if (!response.IsSuccessStatusCode || license == null)
                {
                    TxtResult.Text = $"No se pudo crear la licencia. Codigo: {(int)response.StatusCode}.";
                    return;
                }

                _lastLicenseKey = license.LicenseKey;
                TxtResult.Text =
                    $"Licencia creada correctamente.\n\n" +
                    $"Cliente: {TxtEmail.Text.Trim()}\n" +
                    $"Expira: {license.ExpiresAt?.LocalDateTime:g}\n" +
                    $"Clave:\n{license.LicenseKey}\n\n" +
                    $"Mensaje sugerido:\nHola, esta es tu clave de licencia para activar CV Desktop Editor: {license.LicenseKey}";
            }
            catch (Exception ex)
            {
                TxtResult.Text = $"No se pudo contactar el servidor de licencias.\n\n{ex.Message}";
            }
        }

        private void BtnCopyLicense_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastLicenseKey))
            {
                TxtResult.Text = "Todavia no hay una licencia nueva para copiar.";
                return;
            }

            Clipboard.SetText(_lastLicenseKey);
            TxtResult.Text += "\n\nClave copiada al portapapeles.";
        }

        private async void BtnBuildInstaller_Click(object sender, RoutedEventArgs e)
        {
            var projectRoot = FindProjectRoot();
            if (projectRoot == null)
            {
                OpenBundledClientInstaller();
                return;
            }

            var buildScript = Path.Combine(projectRoot, "scripts", "build-velopack.ps1");
            if (!File.Exists(buildScript))
            {
                TxtResult.Text = "No encontre scripts\\build-velopack.ps1.";
                return;
            }

            try
            {
                TxtResult.Text = "Generando instalador Velopack. Esto puede tardar un poco...";

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{buildScript}\" -Version 0.3.1 -Channel stable",
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process == null)
                {
                    TxtResult.Text = "No se pudo iniciar PowerShell para generar el instalador.";
                    return;
                }

                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    TxtResult.Text = $"No se pudo generar el instalador. Codigo de salida: {process.ExitCode}.";
                    return;
                }
            }
            catch (Exception ex)
            {
                TxtResult.Text = $"No se pudo generar el instalador.\n\n{ex.Message}";
                return;
            }

            var installerPath = Path.Combine(projectRoot, "artifacts", "velopack", "stable", "CVDesktopEditor-stable-Setup.exe");
            var folder = Path.GetDirectoryName(installerPath);
            if (folder == null || !Directory.Exists(folder))
            {
                TxtResult.Text = "El build termino, pero no encontre la carpeta del instalador.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });

            TxtResult.Text = $"Instalador listo:\n{installerPath}";
        }

        private string? FindProjectRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var scriptPath = Path.Combine(current.FullName, "scripts", "build-velopack.ps1");
                var projectPath = Path.Combine(current.FullName, "CVDesktopEditor.csproj");
                if (File.Exists(scriptPath) && File.Exists(projectPath))
                    return current.FullName;

                current = current.Parent;
            }

            return null;
        }

        private bool TryGetExpirationDate(out DateTimeOffset expiresAt)
        {
            expiresAt = default;

            if (!int.TryParse(TxtExpireDay.Text, out var day) ||
                !int.TryParse(TxtExpireMonth.Text, out var month) ||
                !int.TryParse(TxtExpireYear.Text, out var year))
            {
                return false;
            }

            try
            {
                expiresAt = new DateTimeOffset(new DateTime(year, month, day, 23, 59, 59, DateTimeKind.Local)).ToUniversalTime();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OpenBundledClientInstaller()
        {
            var bundledInstaller = Path.Combine(AppContext.BaseDirectory, "ClientInstaller", "CVDesktopEditor-stable-Setup.exe");
            var fallbackInstaller = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..",
                "artifacts", "velopack", "stable", "CVDesktopEditor-stable-Setup.exe"));

            var installerPath = File.Exists(bundledInstaller)
                ? bundledInstaller
                : File.Exists(fallbackInstaller)
                    ? fallbackInstaller
                    : "";

            if (string.IsNullOrWhiteSpace(installerPath))
            {
                TxtResult.Text = "No encontre un instalador de cliente. Genera el cliente con scripts\\build-velopack.ps1 y luego genera de nuevo el instalador admin.";
                return;
            }

            var folder = Path.GetDirectoryName(installerPath);
            if (folder == null)
            {
                TxtResult.Text = "No pude abrir la ubicacion del instalador cliente.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });

            TxtResult.Text = $"Instalador de cliente listo para enviar:\n{installerPath}";
        }

        private sealed class CreateLicenseResponse
        {
            public Guid LicenseId { get; set; }
            public Guid UserId { get; set; }
            public string LicenseKey { get; set; } = "";
            public DateTimeOffset? ExpiresAt { get; set; }
        }
    }
}
