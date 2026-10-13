using System.Windows;
using System.Windows.Media;

namespace CVDesktopEditor.Services
{
    public class ThemeService
    {
        private readonly AppConfigurationService _configurationService = new();

        public AppConfiguration ApplySavedTheme()
        {
            var configuration = _configurationService.Load();
            ApplyTheme(configuration.ThemeMode);
            return configuration;
        }

        public AppConfiguration ToggleTheme()
        {
            var configuration = _configurationService.Load();
            configuration.ThemeMode = IsLight(configuration.ThemeMode) ? "dark" : "light";
            _configurationService.Save(configuration);
            ApplyTheme(configuration.ThemeMode);
            return configuration;
        }

        public static bool IsLight(string themeMode)
        {
            return themeMode.Equals("light", StringComparison.OrdinalIgnoreCase);
        }

        public static void ApplyTheme(string themeMode)
        {
            if (IsLight(themeMode))
            {
                Set("PrimaryBrush", "#27627E");
                Set("PrimaryTextBrush", "#FFFFFF");
                Set("AccentBrush", "#9E6724");
                Set("TextBrush", "#1E2B30");
                Set("MutedBrush", "#5F7076");
                Set("BorderBrushSoft", "#C5D0D3");
                Set("CardBrush", "#FFFFFF");
                Set("AppBgBrush", "#E9EFF0");
                Set("SoftBrush", "#DDE7E9");
                Set("PanelBrush", "#F5F8F8");
                Set("InputBrush", "#FFFFFF");
                Set("SidebarBrush", "#DCE6E8");
                Set("SidebarPanelBrush", "#CAD9DD");
                Set("SidebarTextBrush", "#1E2B30");
                Set("ChromeBrush", "#DFE9EB");
                Set("HoverBrush", "#CEDDE0");
                return;
            }

            Set("PrimaryBrush", "#75BFE0");
            Set("PrimaryTextBrush", "#102027");
            Set("AccentBrush", "#E5B96F");
            Set("TextBrush", "#F2F4F4");
            Set("MutedBrush", "#A9B4B8");
            Set("BorderBrushSoft", "#354147");
            Set("CardBrush", "#20272B");
            Set("AppBgBrush", "#111618");
            Set("SoftBrush", "#283338");
            Set("PanelBrush", "#1A2226");
            Set("InputBrush", "#151D20");
            Set("SidebarBrush", "#192125");
            Set("SidebarPanelBrush", "#253035");
            Set("SidebarTextBrush", "#F2F4F4");
            Set("ChromeBrush", "#171E21");
            Set("HoverBrush", "#34434A");
        }

        private static void Set(string key, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            if (Application.Current.Resources[key] is SolidColorBrush brush && !brush.IsFrozen && !brush.IsSealed)
            {
                brush.Color = color;
                return;
            }

            Application.Current.Resources[key] = new SolidColorBrush(color);
        }
    }
}
