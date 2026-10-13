using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CVDesktopEditor.Models;

namespace CVDesktopEditor.Services
{
    public sealed class ResumeAiImportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<ResumeLanguageData> ImportAsync(
            string extractedText, bool isEnglish, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Trim().Length < 250)
                throw new InvalidOperationException("El PDF no contiene suficiente texto seleccionable para analizarlo con IA.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(150) };
            using var response = await client.PostAsJsonAsync(
                AppConfigurationService.ProductionAiImportApiUrl,
                new { text = extractedText, isEnglish }, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string? message = null;
                try
                {
                    message = JsonSerializer.Deserialize<ImportErrorResponse>(body, JsonOptions)?.Message;
                }
                catch (JsonException)
                {
                    // An upstream error may not contain JSON.
                }

                throw new InvalidOperationException(message ?? $"No se pudo analizar el PDF con IA ({(int)response.StatusCode}).");
            }

            AiImportedResume result;
            try
            {
                result = JsonSerializer.Deserialize<AiImportedResume>(body, JsonOptions)
                    ?? throw new InvalidOperationException("La IA devolvio un resultado vacio.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("La IA devolvio un formato que no se pudo leer.", ex);
            }

            if (string.IsNullOrWhiteSpace(result.FullName) ||
                result.Experience == null || result.Projects == null ||
                result.Education == null || result.Skills == null ||
                result.Experience.Any(item => item == null || item.Responsibilities == null) ||
                result.Projects.Any(item => item == null || item.Details == null) ||
                result.Education.Any(item => item == null) ||
                (string.IsNullOrWhiteSpace(result.ProfessionalSummary) &&
                 result.Experience.Count == 0 && result.Projects.Count == 0 && result.Education.Count == 0))
                throw new InvalidOperationException("La IA no pudo identificar suficientes datos del CV.");

            return new ResumeLanguageData
            {
                FullName = result.FullName.Trim(),
                Location = result.Location.Trim(),
                Phone = result.Phone.Trim(),
                Email = result.Email.Trim(),
                Portfolio = result.Portfolio.Trim(),
                LinkedIn = result.LinkedIn.Trim(),
                ProfessionalSummary = result.ProfessionalSummary.Trim(),
                Experience = new ObservableCollection<ExperienceItem>(result.Experience.Select(item => new ExperienceItem
                {
                    Company = item.Company.Trim(),
                    Position = item.Position.Trim(),
                    Location = item.Location.Trim(),
                    Period = item.Period.Trim(),
                    Responsibilities = ToLines(item.Responsibilities)
                })),
                Projects = new ObservableCollection<ProjectItem>(result.Projects.Select(item => new ProjectItem
                {
                    Title = item.Title.Trim(),
                    Role = item.Role.Trim(),
                    Location = item.Location.Trim(),
                    Details = ToLines(item.Details)
                })),
                Education = new ObservableCollection<EducationItem>(result.Education.Select(item => new EducationItem
                {
                    Institution = item.Institution.Trim(),
                    Degree = item.Degree.Trim(),
                    Description = item.Description.Trim(),
                    Location = item.Location.Trim(),
                    Period = item.Period.Trim()
                })),
                Skills = new ObservableCollection<SkillItem>(result.Skills
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => new SkillItem { Value = value.Trim() }))
            };
        }

        private static ObservableCollection<TextLineItem> ToLines(IEnumerable<string> values) =>
            new(values.Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new TextLineItem { Value = value.Trim() }));

        private sealed class ImportErrorResponse
        {
            public string? Message { get; set; }
        }

        private sealed class AiImportedResume
        {
            public string FullName { get; set; } = "";
            public string Location { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public string Portfolio { get; set; } = "";
            public string LinkedIn { get; set; } = "";
            public string ProfessionalSummary { get; set; } = "";
            public List<AiExperience> Experience { get; set; } = new();
            public List<AiProject> Projects { get; set; } = new();
            public List<AiEducation> Education { get; set; } = new();
            public List<string> Skills { get; set; } = new();
        }

        private sealed class AiExperience
        {
            public string Company { get; set; } = "";
            public string Position { get; set; } = "";
            public string Location { get; set; } = "";
            public string Period { get; set; } = "";
            public List<string> Responsibilities { get; set; } = new();
        }

        private sealed class AiProject
        {
            public string Title { get; set; } = "";
            public string Role { get; set; } = "";
            public string Location { get; set; } = "";
            public List<string> Details { get; set; } = new();
        }

        private sealed class AiEducation
        {
            public string Institution { get; set; } = "";
            public string Degree { get; set; } = "";
            public string Description { get; set; } = "";
            public string Location { get; set; } = "";
            public string Period { get; set; } = "";
        }
    }
}
