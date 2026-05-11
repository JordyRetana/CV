using System.Text;
using System.Text.RegularExpressions;
using CVDesktopEditor.Models;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;

namespace CVDesktopEditor.Services
{
    public class PdfImportService
    {
        private static readonly string[] SummaryHeaders =
        {
            "PROFESSIONAL SUMMARY", "SUMMARY", "PROFILE", "ABOUT ME",
            "RESUMEN PROFESIONAL", "RESUMEN", "PERFIL", "SOBRE MI", "SOBRE M\u00CD"
        };

        private static readonly string[] ExperienceHeaders =
        {
            "PROFESSIONAL EXPERIENCE", "WORK EXPERIENCE", "EXPERIENCE", "EMPLOYMENT",
            "EXPERIENCIA PROFESIONAL", "EXPERIENCIA LABORAL", "EXPERIENCIA"
        };

        private static readonly string[] ProjectHeaders =
        {
            "ACADEMIC & PERSONAL PROJECTS", "PROJECTS", "PERSONAL PROJECTS",
            "PROYECTOS ACAD\u00C9MICOS Y PERSONALES", "PROYECTOS ACADEMICOS Y PERSONALES", "PROYECTOS"
        };

        private static readonly string[] EducationHeaders =
        {
            "EDUCATION", "ACADEMIC BACKGROUND", "EDUCACI\u00D3N", "EDUCACION", "FORMACI\u00D3N", "FORMACION"
        };

        private static readonly string[] SkillHeaders =
        {
            "ADDITIONAL SKILLS", "TECHNICAL SKILLS", "SKILLS", "TOOLS",
            "HABILIDADES ADICIONALES", "SKILLS ADICIONALES", "HABILIDADES", "COMPETENCIAS", "TECNOLOG\u00CDAS", "TECNOLOGIAS"
        };

        public ResumeLanguageData ImportFromPdf(string pdfPath, bool isEnglish)
        {
            var rawText = ExtractText(pdfPath);
            var normalizedText = NormalizeText(rawText);

            return ParseResume(normalizedText);
        }

        private string ExtractText(string pdfPath)
        {
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(pdfPath);
            foreach (var page in document.GetPages())
            {
                var words = page.GetWords()
                    .OrderByDescending(word => word.BoundingBox.Bottom)
                    .ThenBy(word => word.BoundingBox.Left)
                    .ToList();

                var lines = new List<List<Word>>();

                foreach (var word in words)
                {
                    var line = lines.FirstOrDefault(existing =>
                        Math.Abs(existing[0].BoundingBox.Bottom - word.BoundingBox.Bottom) < 3.2);

                    if (line == null)
                    {
                        line = new List<Word>();
                        lines.Add(line);
                    }

                    line.Add(word);
                }

                foreach (var line in lines.OrderByDescending(line => line.Average(word => word.BoundingBox.Bottom)))
                {
                    var orderedWords = line.OrderBy(word => word.BoundingBox.Left).ToList();
                    sb.AppendLine(string.Join(" ", orderedWords.Select(word => word.Text)));
                }
            }

            return sb.ToString();
        }

        private ResumeLanguageData ParseResume(string text)
        {
            var data = new ResumeLanguageData();
            var lines = ToLines(text);

            if (lines.Count == 0)
                return data;

            ParseHeader(lines, data);
            ParseSummary(text, data);
            ParseExperience(text, data);
            ParseProjects(text, data);
            ParseEducation(text, data);
            ParseSkills(text, data);

            return data;
        }

        private void ParseHeader(List<string> lines, ResumeLanguageData data)
        {
            var firstSectionIndex = lines.FindIndex(IsKnownHeader);
            var headerLines = firstSectionIndex > 0
                ? lines.Take(firstSectionIndex).ToList()
                : lines.Take(8).ToList();

            if (headerLines.Count == 0)
                return;

            var fullNameParts = new List<string>();
            foreach (var token in headerLines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Contains(",") || token.Contains("|") || token.Contains("@") || token.StartsWith("+") || token.Contains("http", StringComparison.OrdinalIgnoreCase))
                    break;

                fullNameParts.Add(token);
            }

            data.FullName = fullNameParts.Count > 0 ? string.Join(" ", fullNameParts) : headerLines[0];

            var allHeaderText = string.Join(" ", headerLines);

            var emailMatch = Regex.Match(allHeaderText, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");
            if (emailMatch.Success)
                data.Email = emailMatch.Value.Trim();

            var phoneMatch = Regex.Match(allHeaderText, @"(?:\+\d{1,3}[\s.-]?)?(?:\(?\d{3,4}\)?[\s.-]?)?\d{3,4}[\s.-]?\d{4}");
            if (phoneMatch.Success)
                data.Phone = phoneMatch.Value.Trim();

            var urlMatches = Regex.Matches(allHeaderText, @"(?:https?://|www\.)[^\s|,;]+");
            foreach (Match match in urlMatches)
            {
                var value = match.Value.Trim().TrimEnd('.', ',', ';', '|');
                if (value.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
                    data.LinkedIn = value;
                else if (string.IsNullOrWhiteSpace(data.Portfolio))
                    data.Portfolio = value;
            }

            var locationSource = headerLines.FirstOrDefault(line => line.Contains(",")) ?? "";
            data.Location = CleanContactLine(locationSource, data);
        }

        private void ParseSummary(string text, ResumeLanguageData data)
        {
            var section = GetSection(text, SummaryHeaders, AllHeadersExcept(SummaryHeaders));
            var lines = ToLines(section)
                .Where(line => !IsKnownHeader(line))
                .Where(line => !LooksLikeContactLine(line))
                .ToList();

            data.ProfessionalSummary = string.Join(" ", lines).Trim();
        }

        private void ParseExperience(string text, ResumeLanguageData data)
        {
            var section = GetSection(text, ExperienceHeaders, AllHeadersExcept(ExperienceHeaders));
            var lines = ToLines(section);
            if (lines.Count == 0)
                return;

            var item = BuildExperienceItem(lines);
            if (!string.IsNullOrWhiteSpace(item.Company) || !string.IsNullOrWhiteSpace(item.Position))
                data.Experience.Add(item);
        }

        private void ParseProjects(string text, ResumeLanguageData data)
        {
            var section = GetSection(text, ProjectHeaders, AllHeadersExcept(ProjectHeaders));
            var lines = ToLines(section);
            if (lines.Count == 0)
                return;

            var item = BuildProjectItem(lines);
            if (!string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Role) || item.Details.Count > 0)
                data.Projects.Add(item);
        }

        private void ParseEducation(string text, ResumeLanguageData data)
        {
            var section = GetSection(text, EducationHeaders, AllHeadersExcept(EducationHeaders));
            var lines = ToLines(section);
            if (lines.Count == 0)
                return;

            var edu = BuildEducationItem(lines);
            if (!string.IsNullOrWhiteSpace(edu.Institution) || !string.IsNullOrWhiteSpace(edu.Degree))
                data.Education.Add(edu);
        }

        private void ParseSkills(string text, ResumeLanguageData data)
        {
            var section = GetSection(text, SkillHeaders, Array.Empty<string>());
            var lines = ToLines(section);
            var currentSkill = "";

            foreach (var line in lines)
            {
                if (StartsWithBullet(line))
                {
                    if (!string.IsNullOrWhiteSpace(currentSkill))
                        data.Skills.Add(new SkillItem { Value = currentSkill.Trim() });

                    currentSkill = CleanBullet(line);
                }
                else if (!string.IsNullOrWhiteSpace(currentSkill))
                {
                    currentSkill = $"{currentSkill} {line.Trim()}";
                }
                else
                {
                    currentSkill = line.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(currentSkill))
                data.Skills.Add(new SkillItem { Value = currentSkill.Trim() });
        }

        private ExperienceItem BuildExperienceItem(List<string> lines)
        {
            var period = ExtractPeriod(lines);
            var location = ExtractLikelyLocation(lines);
            var companyLine = lines.FirstOrDefault(line => !LooksLikePeriod(line) && !line.Equals(location, StringComparison.OrdinalIgnoreCase)) ?? "";
            var positionLine = lines.FirstOrDefault(line =>
                !line.Equals(companyLine, StringComparison.OrdinalIgnoreCase) &&
                !line.Equals(location, StringComparison.OrdinalIgnoreCase) &&
                !LooksLikeSentence(RemoveKnownPart(RemoveKnownPart(line, location), period))) ?? "";

            var item = new ExperienceItem
            {
                Company = RemoveKnownPart(RemoveKnownPart(companyLine, location), period),
                Position = RemoveKnownPart(RemoveKnownPart(positionLine, location), period),
                Location = location,
                Period = period
            };

            foreach (var line in lines.SkipWhile(line => !line.Equals(positionLine, StringComparison.OrdinalIgnoreCase)).Skip(1))
            {
                if (!LooksLikePeriod(line) && !line.Equals(location, StringComparison.OrdinalIgnoreCase))
                    item.Responsibilities.Add(new TextLineItem { Value = CleanBullet(line) });
            }

            return item;
        }

        private ProjectItem BuildProjectItem(List<string> lines)
        {
            var location = ExtractLikelyLocation(lines);
            var titleLine = lines.FirstOrDefault(line =>
                !line.Equals(location, StringComparison.OrdinalIgnoreCase) &&
                !LooksLikeSentence(RemoveKnownPart(line, location))) ?? "";
            var roleLine = lines.FirstOrDefault(line =>
                !line.Equals(titleLine, StringComparison.OrdinalIgnoreCase) &&
                !line.Equals(location, StringComparison.OrdinalIgnoreCase) &&
                !LooksLikeSentence(RemoveKnownPart(line, location))) ?? "";

            var item = new ProjectItem
            {
                Title = RemoveKnownPart(titleLine, location),
                Role = roleLine,
                Location = location
            };

            foreach (var line in lines.SkipWhile(line => !line.Equals(roleLine, StringComparison.OrdinalIgnoreCase)).Skip(1))
            {
                if (!line.Equals(location, StringComparison.OrdinalIgnoreCase))
                    item.Details.Add(new TextLineItem { Value = CleanBullet(line) });
            }

            return item;
        }

        private EducationItem BuildEducationItem(List<string> lines)
        {
            var period = ExtractPeriod(lines);
            var location = ExtractLikelyLocation(lines);
            var remaining = lines
                .Select(line => RemoveKnownPart(RemoveKnownPart(line, location), period))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            return new EducationItem
            {
                Institution = remaining.ElementAtOrDefault(0) ?? "",
                Degree = remaining.ElementAtOrDefault(1) ?? "",
                Description = remaining.Count > 2 ? string.Join(" ", remaining.Skip(2)) : "",
                Location = location,
                Period = period
            };
        }

        private string GetSection(string text, string[] startHeaders, string[] endHeaders)
        {
            var start = FindFirstHeader(text, startHeaders);
            if (start.Index < 0)
                return string.Empty;

            var startIndex = start.Index + start.Value.Length;
            var endIndex = text.Length;

            foreach (var endHeader in endHeaders)
            {
                var index = IndexOfHeader(text, endHeader, startIndex);
                if (index >= 0 && index < endIndex)
                    endIndex = index;
            }

            return text[startIndex..endIndex].Trim();
        }

        private (int Index, string Value) FindFirstHeader(string text, string[] headers)
        {
            var matches = headers
                .Select(header => (Index: IndexOfHeader(text, header, 0), Value: header))
                .Where(match => match.Index >= 0)
                .OrderBy(match => match.Index)
                .ToList();

            return matches.FirstOrDefault((-1, ""));
        }

        private int IndexOfHeader(string text, string header, int startIndex)
        {
            var pattern = $@"(^|\n)\s*{Regex.Escape(header).Replace("\\ ", @"\s+")}\s*(\n|$)";
            var match = Regex.Match(text[startIndex..], pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success ? startIndex + match.Index + match.Groups[1].Length : -1;
        }

        private List<string> ToLines(string text)
        {
            return text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Regex.Replace(line.Trim(), @"\s{2,}", " "))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        private bool IsKnownHeader(string line)
        {
            return GetAllHeaders().Any(header => line.Equals(header, StringComparison.OrdinalIgnoreCase));
        }

        private string[] AllHeadersExcept(string[] excluded)
        {
            return GetAllHeaders()
                .Where(header => !excluded.Contains(header, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        private string[] GetAllHeaders()
        {
            return SummaryHeaders
                .Concat(ExperienceHeaders)
                .Concat(ProjectHeaders)
                .Concat(EducationHeaders)
                .Concat(SkillHeaders)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private string ExtractPeriod(List<string> lines)
        {
            var periodPattern = @"(?:\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Ene|Feb|Mar|Abr|May|Jun|Jul|Ago|Sep|Oct|Nov|Dic)[a-z]*\.?\s*)?(?:19|20)\d{2}\s*(?:-|\u2013|\u2014|to|a|al)\s*(?:(?:\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Ene|Feb|Mar|Abr|May|Jun|Jul|Ago|Sep|Oct|Nov|Dic)[a-z]*\.?\s*)?(?:19|20)\d{2}|Present|Presente|Current|Actualidad|Now|Ahora)|\b(?:19|20)\d{2}\b";
            var match = lines
                .Select(line => Regex.Match(line, periodPattern, RegexOptions.IgnoreCase))
                .FirstOrDefault(match => match.Success);

            return match?.Value.Trim() ?? "";
        }

        private string ExtractLikelyLocation(List<string> lines)
        {
            return lines.FirstOrDefault(line =>
                !LooksLikePeriod(line) &&
                (line.Contains(",") ||
                 Regex.IsMatch(line, @"\b(Remote|On-site|Hybrid|Remoto|Presencial|Hibrido)\b", RegexOptions.IgnoreCase)))
                is string locationLine && !string.IsNullOrWhiteSpace(locationLine)
                ? ExtractLocationFromLine(locationLine)
                : "";
        }

        private bool LooksLikePeriod(string line)
        {
            return Regex.IsMatch(line, @"\b(19|20)\d{2}\b") ||
                   line.Contains("Present", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("Presente", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("Actualidad", StringComparison.OrdinalIgnoreCase);
        }

        private bool LooksLikeSentence(string line)
        {
            return line.EndsWith(".") || line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 6;
        }

        private string ExtractLocationFromLine(string line)
        {
            var remoteMatch = Regex.Match(line, @"\b(Remote|On-site|Hybrid|Remoto|Presencial|Hibrido)\b", RegexOptions.IgnoreCase);
            if (remoteMatch.Success && !line.Contains(","))
                return remoteMatch.Value;

            var commaIndex = line.LastIndexOf(',');
            if (commaIndex < 0)
                return line.Trim();

            var beforeComma = line[..commaIndex].Trim();
            var afterComma = line[(commaIndex + 1)..].Trim();
            var words = beforeComma
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Trim(',', '.', '|'))
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .ToList();

            if (words.Count == 0)
                return line.Trim();

            var city = words[^1];
            if (words.Count >= 2 && IsCityPrefix(words[^2]))
                city = $"{words[^2]} {words[^1]}";

            return $"{city}, {afterComma}".Trim();
        }

        private bool IsCityPrefix(string value)
        {
            return value.Equals("San", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Santa", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Santo", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Los", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Las", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Sao", StringComparison.OrdinalIgnoreCase);
        }

        private bool LooksLikeContactLine(string line)
        {
            return line.Contains("@") ||
                   line.Contains("linkedin", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("portfolio", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(line, @"(?:\+\d{1,3}[\s.-]?)?(?:\(?\d{3,4}\)?[\s.-]?)?\d{3,4}[\s.-]?\d{4}");
        }

        private string CleanBullet(string line)
        {
            return Regex.Replace(line.Trim(), @"^(\u25CF|\u2022|-|\*)\s*", "").Trim();
        }

        private bool StartsWithBullet(string line)
        {
            return Regex.IsMatch(line.TrimStart(), @"^(\u25CF|\u2022|-|\*)\s+");
        }

        private string CleanContactLine(string line, ResumeLanguageData data)
        {
            var cleaned = line;
            cleaned = RemoveKnownPart(cleaned, data.FullName);
            cleaned = RemoveKnownPart(cleaned, data.Phone);
            cleaned = RemoveKnownPart(cleaned, data.Email);
            cleaned = RemoveKnownPart(cleaned, data.Portfolio);
            cleaned = RemoveKnownPart(cleaned, data.LinkedIn);
            cleaned = cleaned.Replace("\u2022", " ").Replace("|", " ").Replace(":", " ");
            cleaned = Regex.Replace(cleaned, @"\b(Portfolio|Portafolio|LinkedIn|Email|Correo|Phone|Telefono)\b", " ", RegexOptions.IgnoreCase);
            return Regex.Replace(cleaned, @"\s{2,}", " ").Trim();
        }

        private string RemoveKnownPart(string text, string knownPart)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(knownPart))
                return text.Trim();

            var cleaned = text.Replace(knownPart, "", StringComparison.OrdinalIgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*(\||-|\u2013|\u2014|,)\s*$", "");
            cleaned = Regex.Replace(cleaned, @"^\s*(\||-|\u2013|\u2014|,)\s*", "");
            return cleaned.Trim();
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.Replace("\r", "\n");

            text = Regex.Replace(text, @"([a-z0-9])https?://", "$1\nhttp", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+(\u25CF|\u2022|-|\*)\s+", "\n$1 ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n\s+", "\n");
            text = Regex.Replace(text, @"\n{2,}", "\n");

            return text.Trim();
        }
    }
}
