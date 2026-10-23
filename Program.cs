using System;
using System.Runtime.InteropServices;
using Velopack;

namespace CVDesktopEditor
{
    public static class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

        [STAThread]
        public static void Main()
        {
            VelopackApp.Build().Run();

#if ADMIN_BUILD
            SetCurrentProcessExplicitAppUserModelID("velopack.CVDesktopEditorAdmin");
#else
            SetCurrentProcessExplicitAppUserModelID("velopack.CVDesktopEditor");
#endif

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
