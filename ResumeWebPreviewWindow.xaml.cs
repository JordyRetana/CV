using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using CVDesktopEditor.Models;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class ResumeWebPreviewWindow : Window
    {
        private readonly ResumeLanguageData _resume;
        private readonly bool _isEnglish;
        private readonly ResumeHtmlTemplateService _htmlTemplateService;
        private readonly LicenseService _licenseService;

        private string _currentHtml = "";
        private bool _htmlLoaded = false;

        public ResumeWebPreviewWindow(ResumeLanguageData resume, bool isEnglish)
        {
            InitializeComponent();

            _resume = resume;
            _isEnglish = isEnglish;
            _htmlTemplateService = new ResumeHtmlTemplateService();
            _licenseService = new LicenseService();

            TxtHeader.Text = isEnglish ? "Harvard Resume Preview" : "Vista previa Harvard";
            TxtSubHeader.Text = isEnglish
                ? "Preview and PDF come from the same ATS-friendly document."
                : "El preview y el PDF salen del mismo documento compatible con ATS.";

            Loaded += ResumeWebPreviewWindow_Loaded;
        }

        private async void ResumeWebPreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var webViewFolder = Path.Combine(localAppData, "CVDesktopEditor", "WebView2");

                if (!Directory.Exists(webViewFolder))
                    Directory.CreateDirectory(webViewFolder);

                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: webViewFolder);

                await PreviewWebView.EnsureCoreWebView2Async(environment);

                PreviewWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                PreviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                PreviewWebView.NavigationCompleted += PreviewWebView_NavigationCompleted;

                _currentHtml = _htmlTemplateService.BuildHarvardHtml(_resume, _isEnglish);
                PreviewWebView.NavigateToString(_currentHtml);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo inicializar la vista previa HTML.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PreviewWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _htmlLoaded = e.IsSuccess;
            BtnExportPdf.IsEnabled = _htmlLoaded;
        }

        private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (!_htmlLoaded || PreviewWebView.CoreWebView2 == null)
                return;

            var exportPermission = _licenseService.CheckPdfExportPermission(_isEnglish);
            if (!exportPermission.IsAllowed)
            {
                MessageBox.Show(
                    _isEnglish
                        ? $"PDF export is blocked.\n\n{exportPermission.Message}"
                        : $"La exportación PDF está bloqueada.\n\n{exportPermission.Message}",
                    "Licencia",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var safeName = string.IsNullOrWhiteSpace(_resume.FullName)
                ? (_isEnglish ? "resume" : "curriculum")
                : string.Concat(_resume.FullName
                    .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_'))
                    .Trim()
                    .Replace(" ", "_");

            var defaultFileName = _isEnglish
                ? $"{safeName}_Harvard_EN.pdf"
                : $"{safeName}_Harvard_ES.pdf";

            var dialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = defaultFileName,
                DefaultExt = ".pdf",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var settings = PreviewWebView.CoreWebView2.Environment.CreatePrintSettings();
                settings.ShouldPrintBackgrounds = true;
                settings.ShouldPrintHeaderAndFooter = false;
                settings.MarginTop = 0;
                settings.MarginBottom = 0;
                settings.MarginLeft = 0;
                settings.MarginRight = 0;

                var ok = await PreviewWebView.CoreWebView2.PrintToPdfAsync(dialog.FileName, settings);

                if (ok)
                {
                    var licenseStatus = _licenseService.RegisterPdfExport(_isEnglish);
                    AppLogger.Info($"ATS PDF exported to {dialog.FileName}.");
                    MessageBox.Show(
                        _isEnglish
                            ? $"PDF exported successfully.\n\n{licenseStatus.Message}"
                            : $"PDF exportado correctamente.\n\n{licenseStatus.Message}",
                        "PDF",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    AppLogger.Warn($"WebView2 returned false while exporting PDF to {dialog.FileName}.");
                    MessageBox.Show(
                        _isEnglish ? "The PDF could not be generated." : "No se pudo generar el PDF.",
                        "PDF",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "PDF export failed.");
                MessageBox.Show(
                    $"Ocurrió un error al exportar el PDF:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
