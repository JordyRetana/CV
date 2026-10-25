using System;
using CVDesktopEditor.Services;
using Velopack;

namespace CVDesktopEditor
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            VelopackApp.Build()
                .OnAfterInstallFastCallback(_ => InstalledShortcutService.Repair())
                .OnAfterUpdateFastCallback(_ => InstalledShortcutService.Repair())
                .Run();

            InstalledShortcutService.Repair();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
