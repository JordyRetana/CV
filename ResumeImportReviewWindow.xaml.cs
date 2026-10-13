using System.Text;
using System.Windows;
using CVDesktopEditor.Models;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class ResumeImportReviewWindow : Window
    {
        public ResumeImportReviewWindow(ResumeLanguageData data, string fileName, bool usedAi)
        {
            InitializeComponent();
            WindowSizeService.FitToWorkArea(this);
            TxtOrigin.Text = $"{fileName}  |  {(usedAi ? "Analizado con IA" : "Analizado en este equipo")}";
            TxtCounts.Text = $"{data.Experience.Count} experiencias  |  {data.Projects.Count} proyectos  |  {data.Education.Count} estudios  |  {data.Skills.Count} habilidades";
            TxtReview.Text = BuildReview(data);
        }

        private static string BuildReview(ResumeLanguageData data)
        {
            var text = new StringBuilder();
            text.AppendLine(data.FullName);
            text.AppendLine(string.Join("  |  ", new[] { data.Location, data.Phone, data.Email }
                .Where(value => !string.IsNullOrWhiteSpace(value))));
            text.AppendLine(string.Join("  |  ", new[] { data.Portfolio, data.LinkedIn }
                .Where(value => !string.IsNullOrWhiteSpace(value))));
            text.AppendLine();
            text.AppendLine("RESUMEN PROFESIONAL");
            text.AppendLine(data.ProfessionalSummary);

            text.AppendLine();
            text.AppendLine("EXPERIENCIA");
            foreach (var item in data.Experience)
            {
                text.AppendLine($"{item.Company}  |  {item.Position}  |  {item.Location}  |  {item.Period}");
                foreach (var line in item.Responsibilities)
                    text.AppendLine($"  - {line.Value}");
                text.AppendLine();
            }

            text.AppendLine("PROYECTOS");
            foreach (var item in data.Projects)
            {
                text.AppendLine($"{item.Title}  |  {item.Role}  |  {item.Location}");
                foreach (var line in item.Details)
                    text.AppendLine($"  - {line.Value}");
                text.AppendLine();
            }

            text.AppendLine("EDUCACIÓN");
            foreach (var item in data.Education)
                text.AppendLine($"{item.Institution}  |  {item.Degree}  |  {item.Description}  |  {item.Location}  |  {item.Period}");

            text.AppendLine();
            text.AppendLine("HABILIDADES");
            foreach (var item in data.Skills)
                text.AppendLine($"  - {item.Value}");
            return text.ToString().Trim();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
