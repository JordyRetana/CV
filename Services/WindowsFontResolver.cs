using PdfSharp.Fonts;
using System.IO;

namespace CVDesktopEditor.Services
{
    public class WindowsFontResolver : IFontResolver
    {
        public byte[]? GetFont(string faceName)
        {
            var windowsFonts = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Fonts");

            var file = faceName switch
            {
                "Arial#Regular" => "arial.ttf",
                "Arial#Bold" => "arialbd.ttf",
                "Arial#Italic" => "ariali.ttf",
                "Arial#BoldItalic" => "arialbi.ttf",

                "SegoeUI#Regular" => "segoeui.ttf",
                "SegoeUI#Bold" => "segoeuib.ttf",
                "SegoeUI#Italic" => "segoeuii.ttf",
                "SegoeUI#BoldItalic" => "segoeuiz.ttf",

                _ => "arial.ttf"
            };

            var fullPath = Path.Combine(windowsFonts, file);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"No se encontró la fuente: {fullPath}");

            return File.ReadAllBytes(fullPath);
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            familyName = familyName?.Trim() ?? "";

            if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold && isItalic) return new FontResolverInfo("Arial#BoldItalic");
                if (isBold) return new FontResolverInfo("Arial#Bold");
                if (isItalic) return new FontResolverInfo("Arial#Italic");
                return new FontResolverInfo("Arial#Regular");
            }

            if (familyName.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase) ||
                familyName.Equals("SegoeUI", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold && isItalic) return new FontResolverInfo("SegoeUI#BoldItalic");
                if (isBold) return new FontResolverInfo("SegoeUI#Bold");
                if (isItalic) return new FontResolverInfo("SegoeUI#Italic");
                return new FontResolverInfo("SegoeUI#Regular");
            }

            if (isBold && isItalic) return new FontResolverInfo("Arial#BoldItalic");
            if (isBold) return new FontResolverInfo("Arial#Bold");
            if (isItalic) return new FontResolverInfo("Arial#Italic");
            return new FontResolverInfo("Arial#Regular");
        }
    }
}