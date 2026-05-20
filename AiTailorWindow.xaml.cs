using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using CVDesktopEditor.Models;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class AiTailorWindow : Window
    {
        private readonly ResumeStorageService _storageService = new();
        private readonly ResumeAiTailorService _aiTailorService = new();
        private ResumeStore _store;
        private AiTailorResult? _lastResult;
        private bool _lastResultIsEnglish;

        public AiTailorWindow()
        {
            InitializeComponent();
            _store = _storageService.Load();
            TxtStatus.Text = "Listo. La IA solo ajusta contenido editable; revisa antes de exportar.";
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtJobDescription.Text))
            {
                AppDialogWindow.ShowWarning(this, "IA", "Pega primero la descripcion completa del empleo.");
                return;
            }

            var isEnglish = GetSelectedLanguage() == "en";
            var resume = isEnglish ? _store.English : _store.Spanish;
            if (string.IsNullOrWhiteSpace(resume.FullName) && string.IsNullOrWhiteSpace(resume.ProfessionalSummary))
            {
                AppDialogWindow.ShowWarning(this, "IA", "Carga o edita primero el CV que quieres ajustar.");
                return;
            }

            try
            {
                BtnGenerate.IsEnabled = false;
                BtnApply.IsEnabled = false;
                TxtStatus.Text = "Generando ajuste con IA...";

                _lastResult = await _aiTailorService.TailorAsync(resume, TxtJobDescription.Text, isEnglish);
                _lastResultIsEnglish = isEnglish;
                TxtPreview.Text = FormatPreview(_lastResult);
                BtnApply.IsEnabled = true;
                TxtStatus.Text = "Ajuste generado. Revisa el resultado antes de aplicarlo.";
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "AI tailoring failed.");
                AppDialogWindow.ShowError(this, "IA", $"No se pudo generar el ajuste.\n\n{ex.Message}");
                TxtStatus.Text = "No se pudo generar el ajuste.";
            }
            finally
            {
                BtnGenerate.IsEnabled = true;
            }
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (_lastResult == null)
                return;

            _store = _storageService.Load();
            var resume = _lastResultIsEnglish ? _store.English : _store.Spanish;
            _aiTailorService.ApplyToResume(resume, _lastResult);
            _storageService.Save(_store);
            AppDialogWindow.ShowInfo(this, "IA", "Cambios aplicados al CV. Abre el editor para revisar y ajustar detalles.");
            TxtStatus.Text = "Cambios aplicados correctamente.";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetSelectedLanguage()
        {
            return (CmbLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "es";
        }

        private string FormatPreview(AiTailorResult result)
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
