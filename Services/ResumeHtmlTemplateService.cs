using System.Net;
using System.Text;
using CVDesktopEditor.Models;

namespace CVDesktopEditor.Services
{
    public class ResumeHtmlTemplateService
    {
        public string BuildHarvardHtml(ResumeLanguageData data, bool isEnglish)
        {
            string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

            var summaryTitle = isEnglish ? "PROFESSIONAL SUMMARY" : "RESUMEN PROFESIONAL";
            var experienceTitle = isEnglish ? "PROFESSIONAL EXPERIENCE" : "EXPERIENCIA PROFESIONAL";
            var projectsTitle = isEnglish ? "PROJECTS" : "PROYECTOS";
            var educationTitle = isEnglish ? "EDUCATION" : "EDUCACIÓN";
            var skillsTitle = isEnglish ? "SKILLS" : "HABILIDADES";

            string contactLine1 = string.Join(" | ", new[]
            {
                data.Location?.Trim(),
                data.Phone?.Trim(),
                data.Email?.Trim()
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            string contactLine2 = string.Join(" | ", new[]
            {
                data.Portfolio?.Trim(),
                data.LinkedIn?.Trim()
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var html = new StringBuilder();

            html.Append("""
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>ATS Resume</title>
<style>
    * {
        box-sizing: border-box;
    }

    html, body {
        margin: 0;
        padding: 0;
        background: #eef1f6;
        color: #111827;
        font-family: Arial, Helvetica, sans-serif;
    }

    .page-wrap {
        padding: 24px 0;
    }

    .page {
        width: 210mm;
        min-height: 297mm;
        margin: 0 auto;
        background: #ffffff;
        box-shadow: 0 10px 28px rgba(0,0,0,0.10);
    }

    .sheet {
        width: 100%;
        padding: 14mm 14mm 12mm 14mm;
    }

    .name {
        text-align: center;
        font-size: 29px;
        font-weight: 700;
        margin: 0 0 8px 0;
        line-height: 1.1;
    }

    .contact {
        text-align: center;
        font-size: 11px;
        color: #5b6574;
        line-height: 1.45;
        word-break: break-word;
        margin: 0;
        font-style: normal;
    }

    .section {
        margin-top: 18px;
    }

    .section-title {
        font-size: 12px;
        font-weight: 700;
        margin: 0;
        text-transform: uppercase;
        letter-spacing: 0;
    }

    .rule {
        border: none;
        border-top: 1px solid #8f8f8f;
        margin: 6px 0 9px 0;
    }

    .summary-text {
        font-size: 12px;
        line-height: 1.42;
        white-space: pre-line;
        margin: 0;
    }

    .entry {
        margin-bottom: 12px;
    }

    .entry-head {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        align-items: flex-start;
    }

    .entry-left {
        flex: 1 1 auto;
        min-width: 0;
    }

    .entry-right {
        flex: 0 0 185px;
        text-align: right;
        font-size: 11px;
        color: #4b5563;
        line-height: 1.3;
    }

    .entry-title {
        font-size: 12px;
        font-weight: 700;
        margin: 0 0 1px 0;
        line-height: 1.22;
    }

    .entry-subtitle {
        font-size: 12px;
        margin: 0;
        line-height: 1.32;
    }

    ul.bullets {
        margin: 6px 0 0 16px;
        padding: 0;
    }

    ul.bullets li {
        font-size: 12px;
        line-height: 1.38;
        margin-bottom: 3px;
    }

    .skill-line {
        font-size: 12px;
        line-height: 1.38;
        margin: 0 0 3px 16px;
    }

    @page {
        size: A4;
        margin: 0;
    }

    @media print {
        html, body {
            background: #ffffff;
            width: 210mm;
            height: 297mm;
        }

        .page-wrap {
            padding: 0;
            margin: 0;
        }

        .page {
            width: 210mm;
            min-height: 297mm;
            margin: 0;
            box-shadow: none;
            page-break-after: auto;
        }

        .sheet {
            padding: 14mm 14mm 12mm 14mm;
        }
    }
</style>
</head>
<body>
<div class="page-wrap">
    <main class="page">
        <article class="sheet" aria-label="Resume">
""");

            html.Append($"""<h1 class="name">{Encode(data.FullName)}</h1>""");
            html.Append($"""<address class="contact">{Encode(contactLine1)}<br />{Encode(contactLine2)}</address>""");

            html.Append($$"""
<section class="section" aria-label="{{summaryTitle}}">
    <h2 class="section-title">{{summaryTitle}}</h2>
    <hr class="rule" />
    <p class="summary-text">{{Encode(data.ProfessionalSummary)}}</p>
</section>
""");

            html.Append($$"""
<section class="section" aria-label="{{experienceTitle}}">
    <h2 class="section-title">{{experienceTitle}}</h2>
    <hr class="rule" />
""");

            foreach (var exp in data.Experience.Where(x => !string.IsNullOrWhiteSpace(x.Company) || !string.IsNullOrWhiteSpace(x.Position)))
            {
                html.Append("""
<article class="entry">
    <div class="entry-head">
        <div class="entry-left">
""");
                html.Append($"""<div class="entry-title">{Encode(exp.Company)}</div>""");
                html.Append($"""<div class="entry-subtitle">{Encode(exp.Position)}</div>""");
                html.Append("""
        </div>
        <div class="entry-right">
""");
                html.Append($"""<div>{Encode(exp.Location)}</div>""");
                html.Append($"""<div>{Encode(exp.Period)}</div>""");
                html.Append("""
        </div>
    </div>
""");

                var bullets = exp.Responsibilities
                    .Select(r => r.Value)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (bullets.Count > 0)
                {
                    html.Append("""<ul class="bullets">""");
                    foreach (var bullet in bullets)
                    {
                        html.Append($"""<li>{Encode(bullet)}</li>""");
                    }
                    html.Append("""</ul>""");
                }

                html.Append("""</article>""");
            }

            html.Append("""</section>""");

            html.Append($$"""
<section class="section" aria-label="{{projectsTitle}}">
    <h2 class="section-title">{{projectsTitle}}</h2>
    <hr class="rule" />
""");

            foreach (var project in data.Projects.Where(x => !string.IsNullOrWhiteSpace(x.Title) || !string.IsNullOrWhiteSpace(x.Role)))
            {
                html.Append("""
<article class="entry">
    <div class="entry-head">
        <div class="entry-left">
""");
                html.Append($"""<div class="entry-title">{Encode(project.Title)}</div>""");
                html.Append($"""<div class="entry-subtitle">{Encode(project.Role)}</div>""");
                html.Append("""
        </div>
        <div class="entry-right">
""");
                html.Append($"""<div>{Encode(project.Location)}</div>""");
                html.Append("""
        </div>
    </div>
""");

                var details = project.Details
                    .Select(d => d.Value)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (details.Count > 0)
                {
                    html.Append("""<ul class="bullets">""");
                    foreach (var detail in details)
                    {
                        html.Append($"""<li>{Encode(detail)}</li>""");
                    }
                    html.Append("""</ul>""");
                }

                html.Append("""</article>""");
            }

            html.Append("""</section>""");

            html.Append($$"""
<section class="section" aria-label="{{educationTitle}}">
    <h2 class="section-title">{{educationTitle}}</h2>
    <hr class="rule" />
""");

            foreach (var edu in data.Education.Where(x => !string.IsNullOrWhiteSpace(x.Institution) || !string.IsNullOrWhiteSpace(x.Degree)))
            {
                html.Append("""
<article class="entry">
    <div class="entry-head">
        <div class="entry-left">
""");
                html.Append($"""<div class="entry-title">{Encode(edu.Institution)}</div>""");

                var subtitle = string.IsNullOrWhiteSpace(edu.Description)
                    ? (edu.Degree ?? "")
                    : $"{edu.Degree} — {edu.Description}";

                html.Append($"""<div class="entry-subtitle">{Encode(subtitle)}</div>""");

                html.Append("""
        </div>
        <div class="entry-right">
""");
                html.Append($"""<div>{Encode(edu.Location)}</div>""");
                html.Append($"""<div>{Encode(edu.Period)}</div>""");
                html.Append("""
        </div>
    </div>
</article>
""");
            }

            html.Append("""</section>""");

            html.Append($$"""
<section class="section" aria-label="{{skillsTitle}}">
    <h2 class="section-title">{{skillsTitle}}</h2>
    <hr class="rule" />
    <ul class="bullets">
""");

            foreach (var skill in data.Skills.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
            {
                html.Append($"""<li>{Encode(skill.Value)}</li>""");
            }

            html.Append("""
        </ul>
        </section>
        </article>
    </main>
</div>
</body>
</html>
""");

            return html.ToString();
        }
    }
}
