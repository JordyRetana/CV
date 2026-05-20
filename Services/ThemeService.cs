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
                Set("AccentBrush", "#B8860B");
                Set("TextBrush", "#101828");
                Set("MutedBrush", "#475467");
                Set("BorderBrushSoft", "#D0D5DD");
                Set("CardBrush", "#FFFFFF");
                Set("AppBgBrush", "#EEF2F7");
                Set("SoftBrush", "#E8EEF8");
                Set("PanelBrush", "#F8FAFC");
                Set("InputBrush", "#FFFFFF");
                Set("SidebarBrush", "#101828");
                Set("SidebarPanelBrush", "#182235");
                Set("SidebarTextBrush", "#F8FAFC");
                return;
            }

            Set("AccentBrush", "#D6B36A");
            Set("TextBrush", "#F8FAFC");
            Set("MutedBrush", "#A7B0C0");
            Set("BorderBrushSoft", "#303B52");
            Set("CardBrush", "#121A2A");
            Set("AppBgBrush", "#080D18");
            Set("SoftBrush", "#1B2740");
            Set("PanelBrush", "#182235");
            Set("InputBrush", "#0D1424");
            Set("SidebarBrush", "#0C1324");
            Set("SidebarPanelBrush", "#131D31");
            Set("SidebarTextBrush", "#F8FAFC");
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
