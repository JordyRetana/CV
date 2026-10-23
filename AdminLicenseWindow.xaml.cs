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
        private readonly AdminCredentialService _credentialService = new();
        private string _lastLicenseKey = "";

        public AdminLicenseWindow()
        {
            InitializeComponent();
            WindowSizeService.KeepTaskbarVisible(this);
            WindowSizeService.FitToWorkArea(this);
            var defaultExpiration = DateTime.Today.AddYears(1);
            TxtExpireDay.Text = defaultExpiration.Day.ToString("00");
            TxtExpireMonth.Text = defaultExpiration.Month.ToString("00");
            TxtExpireYear.Text = defaultExpiration.Year.ToString();
            TxtResult.Text = "La licencia generada aparecerá aquí.";
            RefreshCredentialView();
            Loaded += async (_, _) => await RefreshLicensesAsync();
        }

        private async void BtnGenerateLicense_Click(object sender, RoutedEventArgs e)
        {
            var adminKey = _credentialService.Load() ?? TxtAdminKey.Password.Trim();
            if (string.IsNullOrWhiteSpace(adminKey))
            {
                TxtResult.Text = "Introduce la clave de administrador para conectar este equipo.";
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
                BtnGenerateLicense.IsEnabled = false;
                TxtServerStatus.Text = "Conectando...";
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(AppConfigurationService.ProductionLicenseApiBaseUrl.TrimEnd('/') + "/"),
                    Timeout = TimeSpan.FromSeconds(45)
                };

                httpClient.DefaultRequestHeaders.Add("X-Admin-Key", adminKey);

                var response = await httpClient.PostAsJsonAsync("admin/licenses", new
                {
                    Email = TxtEmail.Text.Trim(),
                    FullName = TxtFullName.Text.Trim(),
                    Kind = "premium",
                    MaxDevices = maxDevices,
                    ExpiresAt = expiresAt
                });

                if (!response.IsSuccessStatusCode)
                {
                    TxtServerStatus.Text = "No conectado";
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                        response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _credentialService.Clear();
                        TxtAdminKey.Clear();
                        RefreshCredentialView();
                        TxtResult.Text = "El servidor rechazó la clave admin. Vuelve a introducirla.";
                    }
                    else
                    {
                        TxtResult.Text = (int)response.StatusCode >= 500
                            ? "El servidor de licencias no puede acceder a la base de datos. Espera a que Supabase vuelva a estar activo y vuelve a intentarlo."
                            : $"No se pudo crear la licencia. Código: {(int)response.StatusCode}.";
                    }
                    return;
                }

                var license = await response.Content.ReadFromJsonAsync<CreateLicenseResponse>();
                if (license == null || string.IsNullOrWhiteSpace(license.LicenseKey))
                {
                    TxtServerStatus.Text = "Respuesta inválida";
                    TxtResult.Text = "El servidor no devolvió una clave de licencia válida.";
                    return;
                }

                if (_credentialService.Load() == null)
                {
                    _credentialService.Save(adminKey);
                    TxtAdminKey.Clear();
                    RefreshCredentialView();
                }

                _lastLicenseKey = license.LicenseKey;
                TxtServerStatus.Text = "Licencia creada";
                TxtResult.Text =
                    $"Licencia creada correctamente.\n\n" +
                    $"Cliente: {TxtEmail.Text.Trim()}\n" +
                    $"Expira: {license.ExpiresAt?.LocalDateTime:g}\n" +
                    $"Clave:\n{license.LicenseKey}\n\n" +
                    $"Mensaje sugerido:\nHola, esta es tu clave de licencia para activar CV Desktop Editor: {license.LicenseKey}";
                await RefreshLicensesAsync();
            }
            catch (Exception ex)
            {
                TxtServerStatus.Text = "Sin conexión";
                TxtResult.Text = $"No se pudo contactar el servidor de licencias.\n\n{ex.Message}";
            }
            finally
            {
                BtnGenerateLicense.IsEnabled = true;
            }
        }

        private void BtnChangeAdminKey_Click(object sender, RoutedEventArgs e)
        {
            _credentialService.Clear();
            TxtAdminKey.Clear();
            RefreshCredentialView();
            TxtAdminKey.Focus();
            TxtServerStatus.Text = "Clave pendiente";
            LicenseList.ItemsSource = null;
            TxtLicensesSummary.Text = "Conecta el panel para ver las licencias.";
        }

        private async void BtnRefreshLicenses_Click(object sender, RoutedEventArgs e) => await RefreshLicensesAsync();

        private async Task RefreshLicensesAsync()
        {
            var adminKey = _credentialService.Load();
            if (string.IsNullOrWhiteSpace(adminKey))
            {
                TxtLicensesSummary.Text = "Conecta el panel para ver las licencias.";
                return;
            }

            try
            {
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(AppConfigurationService.ProductionLicenseApiBaseUrl.TrimEnd('/') + "/"),
                    Timeout = TimeSpan.FromSeconds(45)
                };
                httpClient.DefaultRequestHeaders.Add("X-Admin-Key", adminKey);
                using var response = await httpClient.GetAsync("admin/licenses");
                if (!response.IsSuccessStatusCode)
                {
                    TxtLicensesSummary.Text = $"No se pudo consultar: HTTP {(int)response.StatusCode}.";
                    return;
                }

                var licenses = await response.Content.ReadFromJsonAsync<List<AdminLicenseSummary>>() ?? [];
                LicenseList.ItemsSource = licenses.Select(license => new AdminLicenseItem(
                    license.Email,
                    license.FullName ?? "",
                    license.Status == "active" && license.ExpiresAt <= DateTimeOffset.UtcNow
                        ? "Vencida"
                        : license.Status == "active" ? "Activa" : license.Status,
                    $"{license.ActivatedDevices}/{license.MaxDevices} equipos · " +
                    (license.ExpiresAt?.LocalDateTime.ToString("d") ?? "Sin vencimiento"))).ToList();
                TxtLicensesSummary.Text = $"Últimas {licenses.Count} licencias registradas";
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Admin license list could not be loaded.");
                TxtLicensesSummary.Text = "No se pudo consultar el servidor. Intenta actualizar.";
            }
        }

        private void RefreshCredentialView()
        {
            var hasSavedKey = !string.IsNullOrWhiteSpace(_credentialService.Load());
            CredentialEntryPanel.Visibility = hasSavedKey ? Visibility.Collapsed : Visibility.Visible;
            CredentialSavedPanel.Visibility = hasSavedKey ? Visibility.Visible : Visibility.Collapsed;
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
                var version = GetType().Assembly.GetName().Version?.ToString(3) ?? "0.4.3";

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{buildScript}\" -Version {version} -Channel stable",
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

        private sealed class AdminLicenseSummary
        {
            public string Email { get; set; } = "";
            public string? FullName { get; set; }
            public string Status { get; set; } = "";
            public int MaxDevices { get; set; }
            public int ActivatedDevices { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
        }

        private sealed record AdminLicenseItem(string Email, string FullName, string StatusLabel, string Detail);
    }
}
