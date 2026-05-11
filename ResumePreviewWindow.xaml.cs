using System.Linq;
using System.Windows;
using Microsoft.Win32;
using CVDesktopEditor.Models;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class ResumePreviewWindow : Window
    {
        private readonly ResumeLanguageData _resume;
        private readonly bool _isEnglish;
        private readonly PdfExportService _pdfExportService;

        public ResumePreviewWindow(ResumeLanguageData resume, bool isEnglish)
        {
            InitializeComponent();

            _resume = resume;
            _isEnglish = isEnglish;
            _pdfExportService = new PdfExportService();

            TxtHeader.Text = isEnglish ? "Resume Preview" : "Vista previa del CV";
            TxtSubHeader.Text = isEnglish
                ? "Harvard-style preview with ATS-friendly text."
                : "Vista previa estilo Harvard con texto compatible con ATS.";

            DataContext = new ResumePreviewViewModel
            {
                FullName = resume.FullName,
                ContactLine1 = $"{resume.Location}   |   {resume.Phone}   |   {resume.Email}",
                ContactLine2 = $"{resume.Portfolio}   |   {resume.LinkedIn}",
                SummaryTitle = isEnglish ? "PROFESSIONAL SUMMARY" : "RESUMEN PROFESIONAL",
                ExperienceTitle = isEnglish ? "PROFESSIONAL EXPERIENCE" : "EXPERIENCIA PROFESIONAL",
                ProjectsTitle = isEnglish ? "PROJECTS" : "PROYECTOS",
                EducationTitle = isEnglish ? "EDUCATION" : "EDUCACIÓN",
                SkillsTitle = isEnglish ? "SKILLS" : "HABILIDADES",
                ProfessionalSummary = resume.ProfessionalSummary,
                Experience = resume.Experience
                    .Where(x => !string.IsNullOrWhiteSpace(x.Company) || !string.IsNullOrWhiteSpace(x.Position))
                    .ToList(),
                Projects = resume.Projects
                    .Where(x => !string.IsNullOrWhiteSpace(x.Title) || !string.IsNullOrWhiteSpace(x.Role))
                    .ToList(),
                Education = resume.Education
                    .Where(x => !string.IsNullOrWhiteSpace(x.Institution) || !string.IsNullOrWhiteSpace(x.Degree))
                    .ToList(),
                Skills = resume.Skills
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .ToList()
            };
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
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

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _pdfExportService.ExportResumeToPdfHarvard(_resume, dialog.FileName, _isEnglish);

                    MessageBox.Show(
                        _isEnglish ? "Harvard PDF exported successfully." : "PDF Harvard exportado correctamente.",
                        "PDF",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ocurrió un error al exportar el PDF:\n\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ResumePreviewViewModel
    {
        public string FullName { get; set; } = "";
        public string ContactLine1 { get; set; } = "";
        public string ContactLine2 { get; set; } = "";
        public string SummaryTitle { get; set; } = "";
        public string ExperienceTitle { get; set; } = "";
        public string ProjectsTitle { get; set; } = "";
        public string EducationTitle { get; set; } = "";
        public string SkillsTitle { get; set; } = "";
        public string ProfessionalSummary { get; set; } = "";

        public List<ExperienceItem> Experience { get; set; } = new();
        public List<ProjectItem> Projects { get; set; } = new();
        public List<EducationItem> Education { get; set; } = new();
        public List<SkillItem> Skills { get; set; } = new();
    }
}