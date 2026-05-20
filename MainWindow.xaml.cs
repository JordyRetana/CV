using System.Windows;
using Microsoft.Win32;
using CVDesktopEditor.Models;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class MainWindow : Window
    {
        private readonly ResumeStorageService _storageService;
        private readonly PdfImportService _pdfImportService;
        private readonly LicenseService _licenseService;
        private readonly ThemeService _themeService;
        private ResumeStore _store;
        private LicenseStatus _licenseStatus;
        private AppConfiguration _configuration;

        public MainWindow()
        {
            InitializeComponent();
            AppLogger.Info("Main window initialized.");
            _storageService = new ResumeStorageService();
            _pdfImportService = new PdfImportService();
            _licenseService = new LicenseService();
            _themeService = new ThemeService();
            _configuration = _themeService.ApplySavedTheme();
            _store = _storageService.Load();
            _licenseStatus = _licenseService.GetStatus();
            if (_licenseStatus.State == LicenseState.NotActivated)
                _licenseStatus = _licenseService.StartTrial();

            _ = new LicenseServerWakeService().WakeAsync();

#if DEBUG || ADMIN_BUILD
            BtnAdmin.Visibility = Visibility.Visible;
#endif

            AppLogger.Info($"License status: {_licenseStatus.State}.");
            RefreshStatus();
            RefreshThemeButton();
        }

        private void RefreshStatus()
        {
            var esLoaded = string.IsNullOrWhiteSpace(_store.SpanishPdfPath) ? "No cargado" : "Cargado";
            var enLoaded = string.IsNullOrWhiteSpace(_store.EnglishPdfPath) ? "No cargado" : "Cargado";

            TxtStatus.Text = $"CV Español: {esLoaded}\nCV Inglés: {enLoaded}\nLicencia: {_licenseStatus.Message}";
        }

        private void BtnImportSpanish_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Selecciona tu CV en Español"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    AppLogger.Info($"Importing Spanish CV from {dialog.FileName}.");
                    var savedPath = _storageService.SavePdfCopy(dialog.FileName, "es");
                    var parsedData = _pdfImportService.ImportFromPdf(savedPath, false);

                    _store.SpanishPdfPath = savedPath;
                    _store.Spanish = parsedData;

                    _storageService.Save(_store);
                    RefreshStatus();

                    AppDialogWindow.ShowInfo(this, "Importación", BuildImportSummary(parsedData, false));
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Spanish CV import failed.");
                    AppDialogWindow.ShowError(this, "Importación", $"No se pudo importar el CV en español.\n\n{ex.Message}");
                }
            }
        }

        private void BtnImportEnglish_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Select your English CV"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    AppLogger.Info($"Importing English CV from {dialog.FileName}.");
                    var savedPath = _storageService.SavePdfCopy(dialog.FileName, "en");
                    var parsedData = _pdfImportService.ImportFromPdf(savedPath, true);

                    _store.EnglishPdfPath = savedPath;
                    _store.English = parsedData;

                    _storageService.Save(_store);
                    RefreshStatus();

                    AppDialogWindow.ShowInfo(this, "Import", BuildImportSummary(parsedData, true));
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "English CV import failed.");
                    AppDialogWindow.ShowError(this, "Import", $"The English CV could not be imported.\n\n{ex.Message}");
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var confirmed = AppDialogWindow.Confirm(
                this,
                "Confirmar",
                "Esto eliminará los PDFs guardados y los datos locales. ¿Deseas continuar?");

            if (confirmed)
            {
                _storageService.ClearAll();
                AppLogger.Info("Local resume data cleared.");
                _store = _storageService.Load();
                RefreshStatus();
                AppDialogWindow.ShowInfo(this, "Datos locales", "Datos eliminados correctamente.");
            }
        }

        private void BtnEditSpanish_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ResumeEditorWindow("es");
            editor.ShowDialog();
            ReloadState();
        }

        private void BtnEditEnglish_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ResumeEditorWindow("en");
            editor.ShowDialog();
            ReloadState();
        }

        private void BtnPreviewSpanish_Click(object sender, RoutedEventArgs e)
        {
            _store = _storageService.Load();
            var preview = new ResumeWebPreviewWindow(_store.Spanish, false);
            preview.ShowDialog();
            ReloadState();
        }

        private void BtnPreviewEnglish_Click(object sender, RoutedEventArgs e)
        {
            _store = _storageService.Load();
            var preview = new ResumeWebPreviewWindow(_store.English, true);
            preview.ShowDialog();
            ReloadState();
        }

        private void BtnActivateLicense_Click(object sender, RoutedEventArgs e)
        {
            var activationWindow = new LicenseActivationWindow
            {
                Owner = this
            };

            if (activationWindow.ShowDialog() == true && activationWindow.ActivatedStatus != null)
            {
                _licenseStatus = activationWindow.ActivatedStatus;
                RefreshStatus();
            }
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            var adminWindow = new AdminLicenseWindow
            {
                Owner = this
            };

            adminWindow.ShowDialog();
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            _configuration = _themeService.ToggleTheme();
            RefreshThemeButton();
        }

        private void BtnAiTailor_Click(object sender, RoutedEventArgs e)
        {
            ReloadState();
            if (_licenseStatus.State != LicenseState.Active)
            {
                AppDialogWindow.ShowWarning(
                    this,
                    "Función premium",
                    $"El ajuste con IA solo está disponible con una licencia activa.\n\n{AppConfigurationService.PurchaseMessage}");
                return;
            }

            var aiWindow = new AiTailorWindow
            {
                Owner = this
            };

            aiWindow.ShowDialog();
            ReloadState();
        }

        private string BuildImportSummary(ResumeLanguageData data, bool isEnglish)
        {
            if (isEnglish)
            {
                return
                    "English CV imported.\n\n" +
                    $"Detected: {data.Experience.Count} experience item(s), {data.Projects.Count} project(s), {data.Education.Count} education item(s), {data.Skills.Count} skill(s).\n\n" +
                    "Open the editor tabs to review and adjust anything the PDF format did not expose clearly.";
            }

            return
                "CV en español importado.\n\n" +
                $"Detectado: {data.Experience.Count} experiencia(s), {data.Projects.Count} proyecto(s), {data.Education.Count} educación(es), {data.Skills.Count} habilidad(es).\n\n" +
                "Abre las pestañas del editor para revisar y ajustar lo que el formato del PDF no haya dejado claro.";
        }

        private void ReloadState()
        {
            _store = _storageService.Load();
            _licenseStatus = _licenseService.GetStatus();
            RefreshStatus();
        }

        private void RefreshThemeButton()
        {
            BtnTheme.Content = ThemeService.IsLight(_configuration.ThemeMode) ? "Modo noche" : "Modo claro";
        }
    }
}
