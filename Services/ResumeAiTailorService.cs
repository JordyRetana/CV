using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CVDesktopEditor.Models;

namespace CVDesktopEditor.Services
{
    public class ResumeAiTailorService
    {
        private const string Model = "llama-3.3-70b-versatile";

        public async Task<AiTailorResult> TailorAsync(
            ResumeLanguageData resume,
            string jobDescription,
            bool isEnglish,
            CancellationToken cancellationToken = default)
        {
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("GROQ_API_KEY is not configured on this computer.");

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.groq.com/openai/v1/"),
                Timeout = TimeSpan.FromSeconds(60)
            };

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var language = isEnglish ? "English" : "Spanish";
            var payload = new
            {
                model = Model,
                temperature = 0.35,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content =
                            "You are a senior resume optimization assistant. Return only valid JSON. " +
                            "Do not invent employers, degrees, dates, certifications, or fake achievements. " +
                            "Use the candidate's real background and align wording to the job description. " +
                            "Keep output ATS friendly."
                    },
                    new
                    {
                        role = "user",
                        content =
                            $"Language: {language}\n\n" +
                            "Return JSON with exactly these properties:\n" +
                            "{\n" +
                            "  \"professionalSummary\": \"string, 3-4 lines max\",\n" +
                            "  \"projects\": [{\"title\":\"string\",\"role\":\"string\",\"location\":\"string\",\"details\":[\"string\"]}],\n" +
                            "  \"skills\": [\"string\"]\n" +
                            "}\n\n" +
                            "Rules:\n" +
                            "- Preserve real project titles when possible.\n" +
                            "- Details must be concise bullet statements.\n" +
                            "- Skills should be keyword-rich, grouped as readable phrases, not a huge paragraph.\n" +
                            "- Do not include markdown.\n\n" +
                            $"Current resume JSON:\n{JsonSerializer.Serialize(resume)}\n\n" +
                            $"Job description:\n{jobDescription}"
                    }
                }
            };

            var response = await httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Groq request failed ({(int)response.StatusCode}). {responseText}");

            var chatResponse = JsonSerializer.Deserialize<GroqChatResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Groq returned an empty response.");

            var result = JsonSerializer.Deserialize<AiTailorResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? throw new InvalidOperationException("The AI response could not be parsed.");
        }

        public void ApplyToResume(ResumeLanguageData resume, AiTailorResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.ProfessionalSummary))
                resume.ProfessionalSummary = result.ProfessionalSummary.Trim();

            if (result.Projects.Count > 0)
            {
                resume.Projects.Clear();
                foreach (var project in result.Projects.Take(4))
                {
                    resume.Projects.Add(new ProjectItem
                    {
                        Title = project.Title.Trim(),
                        Role = project.Role.Trim(),
                        Location = project.Location.Trim(),
                        Details = new ObservableCollection<TextLineItem>(
                            project.Details
                                .Where(detail => !string.IsNullOrWhiteSpace(detail))
                                .Take(5)
                                .Select(detail => new TextLineItem { Value = detail.Trim() }))
                    });
                }
            }

            if (result.Skills.Count > 0)
            {
                resume.Skills.Clear();
                foreach (var skill in result.Skills.Where(skill => !string.IsNullOrWhiteSpace(skill)).Take(12))
                    resume.Skills.Add(new SkillItem { Value = skill.Trim() });
            }
        }
    }

    public class AiTailorResult
    {
        public string ProfessionalSummary { get; set; } = "";
        public List<AiProjectResult> Projects { get; set; } = new();
        public List<string> Skills { get; set; } = new();
    }

    public class AiProjectResult
    {
        public string Title { get; set; } = "";
        public string Role { get; set; } = "";
        public string Location { get; set; } = "";
        public List<string> Details { get; set; } = new();
    }

    internal class GroqChatResponse
    {
        public List<GroqChoice> Choices { get; set; } = new();
    }

    internal class GroqChoice
    {
        public GroqMessage Message { get; set; } = new();
    }

    internal class GroqMessage
    {
        public string Content { get; set; } = "";
    }
}
