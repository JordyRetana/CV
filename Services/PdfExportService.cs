using System;
using System.IO;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using CVDesktopEditor.Models;

namespace CVDesktopEditor.Services
{
    public class PdfExportService
    {
        public void ExportResumeToPdfHarvard(ResumeLanguageData data, string outputPath, bool isEnglish)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var document = new PdfDocument();
            document.Info.Title = isEnglish ? "Resume" : "Curriculum Vitae";

            PdfPage page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;

            XGraphics gfx = XGraphics.FromPdfPage(page);

            var titleFont = new XFont("Segoe UI", 18, XFontStyleEx.Bold);
            var contactFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var sectionFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var bodyFont = new XFont("Segoe UI", 10, XFontStyleEx.Regular);
            var bodyBoldFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var metaFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);

            const double marginLeft = 48;
            const double marginRight = 48;
            const double marginTop = 42;
            const double marginBottom = 42;

            double PageWidth() => page.Width.Point;
            double PageHeight() => page.Height.Point;

            double usableWidth = PageWidth() - marginLeft - marginRight;
            double y = marginTop;

            void AddPage()
            {
                page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = marginTop;
            }

            void EnsureSpace(double neededHeight)
            {
                if (y + neededHeight <= PageHeight() - marginBottom)
                    return;

                AddPage();
            }

            double DrawWrappedText(string text, XFont font, double x, double yPos, double maxWidth, double lineHeight)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return yPos;

                var paragraphs = text.Replace("\r", "")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p));

                foreach (var paragraph in paragraphs)
                {
                    var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string currentLine = string.Empty;

                    foreach (var word in words)
                    {
                        var candidate = string.IsNullOrWhiteSpace(currentLine) ? word : $"{currentLine} {word}";
                        var size = gfx.MeasureString(candidate, font);

                        if (size.Width > maxWidth && !string.IsNullOrWhiteSpace(currentLine))
                        {
                            if (yPos + lineHeight > PageHeight() - marginBottom)
                            {
                                AddPage();
                                yPos = y;
                            }

                            gfx.DrawString(currentLine, font, XBrushes.Black,
                                new XRect(x, yPos, maxWidth, lineHeight), XStringFormats.TopLeft);
                            yPos += lineHeight;
                            currentLine = word;
                        }
                        else
                        {
                            currentLine = candidate;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(currentLine))
                    {
                        if (yPos + lineHeight > PageHeight() - marginBottom)
                        {
                            AddPage();
                            yPos = y;
                        }

                        gfx.DrawString(currentLine, font, XBrushes.Black,
                            new XRect(x, yPos, maxWidth, lineHeight), XStringFormats.TopLeft);
                        yPos += lineHeight;
                    }
                }

                return yPos;
            }

            void DrawSectionHeader(string title)
            {
                EnsureSpace(24);
                gfx.DrawLine(XPens.Gray, marginLeft, y + 3, PageWidth() - marginRight, y + 3);
                gfx.DrawString(title.ToUpperInvariant(), sectionFont, XBrushes.Black,
                    new XRect(marginLeft, y + 7, usableWidth, 16), XStringFormats.TopLeft);
                y += 24;
            }

            void DrawRightAligned(string text, XFont font, double yPos)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return;

                gfx.DrawString(text, font, XBrushes.Black,
                    new XRect(marginLeft, yPos, usableWidth, 14), XStringFormats.TopRight);
            }

            void DrawEntry(string leftTitle, string leftSubtitle, string rightLine1, string rightLine2, string[] bullets)
            {
                EnsureSpace(42);

                if (!string.IsNullOrWhiteSpace(leftTitle))
                {
                    gfx.DrawString(leftTitle, bodyBoldFont, XBrushes.Black,
                        new XRect(marginLeft, y, usableWidth * 0.62, 14), XStringFormats.TopLeft);
                }

                if (!string.IsNullOrWhiteSpace(rightLine1))
                {
                    DrawRightAligned(rightLine1, metaFont, y);
                }

                y += 14;

                if (!string.IsNullOrWhiteSpace(leftSubtitle))
                {
                    gfx.DrawString(leftSubtitle, bodyFont, XBrushes.Black,
                        new XRect(marginLeft, y, usableWidth * 0.62, 14), XStringFormats.TopLeft);
                }

                if (!string.IsNullOrWhiteSpace(rightLine2))
                {
                    DrawRightAligned(rightLine2, metaFont, y);
                }

                y += 16;

                foreach (var bullet in bullets.Where(b => !string.IsNullOrWhiteSpace(b)))
                {
                    EnsureSpace(16);
                    y = DrawWrappedText($"• {bullet}", bodyFont, marginLeft + 10, y, usableWidth - 10, 14);
                    y += 2;
                }

                y += 4;
            }

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

            EnsureSpace(60);
            gfx.DrawString(data.FullName ?? "", titleFont, XBrushes.Black,
                new XRect(marginLeft, y, usableWidth, 24), XStringFormats.TopCenter);
            y += 24;

            y = DrawWrappedText(contactLine1, contactFont, marginLeft, y, usableWidth, 12);
            y = DrawWrappedText(contactLine2, contactFont, marginLeft, y + 2, usableWidth, 12);
            y += 12;

            DrawSectionHeader(isEnglish ? "Professional Summary" : "Resumen Profesional");
            y = DrawWrappedText(data.ProfessionalSummary ?? "", bodyFont, marginLeft, y, usableWidth, 14);
            y += 8;

            DrawSectionHeader(isEnglish ? "Professional Experience" : "Experiencia Profesional");
            foreach (var exp in data.Experience.Where(x => !string.IsNullOrWhiteSpace(x.Company) || !string.IsNullOrWhiteSpace(x.Position)))
            {
                DrawEntry(
                    exp.Company ?? "",
                    exp.Position ?? "",
                    exp.Location ?? "",
                    exp.Period ?? "",
                    exp.Responsibilities.Select(r => r.Value ?? "").ToArray()
                );
            }

            DrawSectionHeader(isEnglish ? "Projects" : "Proyectos");
            foreach (var project in data.Projects.Where(x => !string.IsNullOrWhiteSpace(x.Title) || !string.IsNullOrWhiteSpace(x.Role)))
            {
                DrawEntry(
                    project.Title ?? "",
                    project.Role ?? "",
                    project.Location ?? "",
                    "",
                    project.Details.Select(d => d.Value ?? "").ToArray()
                );
            }

            DrawSectionHeader(isEnglish ? "Education" : "Educación");
            foreach (var edu in data.Education.Where(x => !string.IsNullOrWhiteSpace(x.Institution) || !string.IsNullOrWhiteSpace(x.Degree)))
            {
                string subtitle = string.IsNullOrWhiteSpace(edu.Description)
                    ? (edu.Degree ?? "")
                    : $"{edu.Degree} — {edu.Description}";

                DrawEntry(
                    edu.Institution ?? "",
                    subtitle,
                    edu.Location ?? "",
                    edu.Period ?? "",
                    Array.Empty<string>()
                );
            }

            DrawSectionHeader(isEnglish ? "Skills" : "Habilidades");
            foreach (var skill in data.Skills.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
            {
                EnsureSpace(16);
                y = DrawWrappedText($"• {skill.Value}", bodyFont, marginLeft + 10, y, usableWidth - 10, 14);
                y += 2;
            }

            document.Save(outputPath);
        }
    }
}
