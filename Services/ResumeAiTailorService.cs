using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CVDesktopEditor.Models;

namespace CVDesktopEditor.Services
{
    public class ResumeAiTailorService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<AiTailorResult> TailorAsync(
            ResumeLanguageData resume,
            string jobDescription,
            bool isEnglish,
            CancellationToken cancellationToken = default)
        {
            var licenseService = new LicenseService();
            if (!licenseService.TryGetOnlineCredentials(out var licenseKey, out var deviceHash))
                throw new InvalidOperationException("Activa de nuevo tu licencia para usar IA.");

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            var payload = new
            {
                licenseKey,
                deviceHash,
                isEnglish,
                jobDescription,
                resume = new
                {
                    resume.ProfessionalSummary,
                    resume.Experience,
                    resume.Projects,
                    resume.Education,
                    resume.Skills
                }
            };

            using var response = await httpClient.PostAsJsonAsync(
                AppConfigurationService.ProductionAiApiUrl, payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<AiErrorResponse>(body, JsonOptions);
                throw new InvalidOperationException(error?.Message ?? $"El servicio de IA respondio con error {(int)response.StatusCode}.");
            }

            var result = JsonSerializer.Deserialize<AiTailorResult>(body, JsonOptions)
                ?? throw new InvalidOperationException("La IA devolvio una respuesta vacia.");

            if (WordCount(result.ProfessionalSummary) < 35 ||
                result.Projects.Count != resume.Projects.Count ||
                result.Projects.Select(project => project.SourceIndex).Distinct().Count() != resume.Projects.Count ||
                result.Projects.Any(project => project.SourceIndex < 0 || project.SourceIndex >= resume.Projects.Count))
                throw new InvalidOperationException("La respuesta de IA quedo incompleta. Intenta generar el ajuste otra vez.");

            return result;
        }

        public void ApplyToResume(ResumeLanguageData resume, AiTailorResult result)
        {
            if (WordCount(result.ProfessionalSummary) >= 35 &&
                WordCount(result.ProfessionalSummary) >= WordCount(resume.ProfessionalSummary) * 0.8)
                resume.ProfessionalSummary = result.ProfessionalSummary.Trim();

            foreach (var suggestion in result.Projects)
            {
                if (suggestion.SourceIndex < 0 || suggestion.SourceIndex >= resume.Projects.Count)
                    continue;

                var project = resume.Projects[suggestion.SourceIndex];
                var details = suggestion.Details
                    .Concat(project.Details.Select(detail => detail.Value))
                    .Where(detail => !string.IsNullOrWhiteSpace(detail))
                    .Select(detail => detail.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(detail => new TextLineItem { Value = detail });
                project.Details = new ObservableCollection<TextLineItem>(details);
            }

            var skills = result.Skills
                .Concat(resume.Skills.Select(skill => skill.Value))
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Select(skill => skill.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(skill => new SkillItem { Value = skill });
            resume.Skills = new ObservableCollection<SkillItem>(skills);
        }

        private static int WordCount(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? 0
                : value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public class AiTailorResult
    {
        public string ProfessionalSummary { get; set; } = "";
        public List<AiProjectResult> Projects { get; set; } = new();
        public List<string> Skills { get; set; } = new();
    }

    public class AiProjectResult
    {
        public int SourceIndex { get; set; }
        public List<string> Details { get; set; } = new();
    }

    internal class AiErrorResponse
    {
        public string? Message { get; set; }
    }
}
