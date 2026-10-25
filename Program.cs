using System;
using System.Runtime.InteropServices;
using Velopack;

namespace CVDesktopEditor
{
    public static class Program
    {
#if ADMIN_BUILD
        internal const string AppUserModelId = "velopack.CVDesktopEditorAdmin";
#else
        internal const string AppUserModelId = "velopack.CVDesktopEditor";
#endif

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

        [STAThread]
        public static void Main()
        {
            Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(AppUserModelId));
            VelopackApp.Build().Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
