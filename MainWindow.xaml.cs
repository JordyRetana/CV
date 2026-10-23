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
        private readonly ResumeAiImportService _aiImportService;
        private readonly LicenseService _licenseService;
        private readonly ThemeService _themeService;
        private ResumeStore _store;
        private LicenseStatus _licenseStatus;
        private AppConfiguration _configuration;

        public MainWindow()
        {
            AppLogger.Info("Main window initialized.");
            _storageService = new ResumeStorageService();
            _pdfImportService = new PdfImportService();
            _aiImportService = new ResumeAiImportService();
            _licenseService = new LicenseService();
            _themeService = new ThemeService();
            _configuration = _themeService.ApplySavedTheme();

            InitializeComponent();
            WindowSizeService.KeepTaskbarVisible(this);
            TaskbarIconService.Apply(this);
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
            TxtSpanishStatus.Text = string.IsNullOrWhiteSpace(_store.SpanishPdfPath) ? "Sin PDF" : "PDF importado";
            TxtEnglishStatus.Text = string.IsNullOrWhiteSpace(_store.EnglishPdfPath) ? "Sin PDF" : "PDF importado";
            TxtLicenseStatus.Text = _licenseStatus.State switch
            {
                LicenseState.Active => "Premium activa",
                LicenseState.Trial => $"Prueba: {_licenseStatus.ExportsRemaining} exportaciones disponibles",
                LicenseState.Expired => "Licencia vencida",
                _ => "Activación necesaria"
            };
        }

        private async void BtnImportSpanish_Click(object sender, RoutedEventArgs e)
        {
            await ImportResumeAsync(false);
        }

        private async void BtnImportEnglish_Click(object sender, RoutedEventArgs e)
        {
            await ImportResumeAsync(true);
        }

        private async Task ImportResumeAsync(bool isEnglish)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = isEnglish ? "Select your English CV" : "Selecciona tu CV en Español"
            };
            if (dialog.ShowDialog() != true)
                return;

            var useAi = AppDialogWindow.Confirm(
                this,
                "Importar PDF",
                "La importación con IA es gratuita. El texto de este CV se enviará a Cloudflare y Groq para organizar los datos. " +
                "Si prefieres no enviarlo, elige No para analizarlo solo en este equipo. ¿Usar IA?");

            try
            {
                BtnImportSpanish.IsEnabled = false;
                BtnImportEnglish.IsEnabled = false;
                var status = isEnglish ? TxtEnglishStatus : TxtSpanishStatus;
                status.Text = "Analizando...";

                var text = await Task.Run(() => _pdfImportService.ExtractTextFromPdf(dialog.FileName));
                if (text.Trim().Length < 40)
                {
                    AppDialogWindow.ShowWarning(this, "Importación",
                        "El PDF no contiene texto seleccionable. Parece un escaneo; conviértelo con OCR antes de importarlo.");
                    return;
                }

                ResumeLanguageData parsedData;
                var usedAi = false;
                if (useAi && text.Length >= 250)
                {
                    try
                    {
                        parsedData = await _aiImportService.ImportAsync(text, isEnglish);
                        usedAi = true;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(ex, "AI CV import failed; local import is available.");
                        if (!AppDialogWindow.Confirm(this, "Importación con IA",
                            $"No se pudo completar el análisis con IA.\n\n{ex.Message}\n\n¿Deseas intentar la importación local?"))
                            return;
                        parsedData = _pdfImportService.ImportFromText(text);
                    }
                }
                else
                {
                    if (useAi)
                        AppDialogWindow.ShowInfo(this, "Importación", "Este CV tiene muy poco texto para IA. Se analizará en este equipo.");
                    parsedData = _pdfImportService.ImportFromText(text);
                }

                var review = new ResumeImportReviewWindow(parsedData, System.IO.Path.GetFileName(dialog.FileName), usedAi)
                {
                    Owner = this
                };
                if (review.ShowDialog() != true)
                    return;

                _store = _storageService.Load();
                var savedPath = _storageService.SavePdfCopy(dialog.FileName, isEnglish ? "en" : "es");
                if (isEnglish)
                {
                    _store.EnglishPdfPath = savedPath;
                    _store.English = parsedData;
                }
                else
                {
                    _store.SpanishPdfPath = savedPath;
                    _store.Spanish = parsedData;
                }

                _storageService.Save(_store);
                AppLogger.Info($"CV imported with {(usedAi ? "AI" : "local parsing")}.");
                AppDialogWindow.ShowInfo(this, "Importación", BuildImportSummary(parsedData, isEnglish));
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "CV import failed.");
                AppDialogWindow.ShowError(this, "Importación", $"No se pudo importar el CV.\n\n{ex.Message}");
            }
            finally
            {
                BtnImportSpanish.IsEnabled = true;
                BtnImportEnglish.IsEnabled = true;
                RefreshStatus();
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

            if (!_licenseService.TryGetOnlineCredentials(out _, out _))
            {
                AppDialogWindow.ShowWarning(this, "IA", "Para usar IA, activa de nuevo tu licencia en esta computadora. La clave se guardara protegida para las proximas sesiones.");
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
