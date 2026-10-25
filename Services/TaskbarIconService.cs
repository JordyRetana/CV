using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CVDesktopEditor.Services
{
    internal static class TaskbarIconService
    {
        private const int WmSetIcon = 0x0080;
        private const int WmDestroy = 0x0002;
        private const int GclpHicon = -14;
        private const int GclpHiconSmall = -34;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr size, IntPtr icon);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW", SetLastError = true)]
        private static extern IntPtr SetClassLongPtr(IntPtr window, int index, IntPtr value);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
        private static extern IntPtr GetClassLongPtr(IntPtr window, int index);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconEx(string file, int index, out IntPtr largeIcon, out IntPtr smallIcon, uint count);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr icon);

        public static void Apply(Window window)
        {
            var smallIcon = IntPtr.Zero;
            var largeIcon = IntPtr.Zero;
            var previousSmallClassIcon = IntPtr.Zero;
            var previousLargeClassIcon = IntPtr.Zero;
            var configured = false;

            void Configure()
            {
                if (configured)
                    return;

                try
                {
                    var executable = Environment.ProcessPath;
                    if (string.IsNullOrWhiteSpace(executable))
                        return;

                    var iconFile = Path.Combine(AppContext.BaseDirectory, "Assets", "appicon.ico");
                    if (!File.Exists(iconFile))
                        iconFile = executable;

                    var handle = new WindowInteropHelper(window).Handle;
                    if (ExtractIconEx(iconFile, 0, out largeIcon, out smallIcon, 1) > 0)
                    {
                        if (smallIcon != IntPtr.Zero)
                        {
                            SendMessage(handle, WmSetIcon, IntPtr.Zero, smallIcon);
                            previousSmallClassIcon = SetClassLongPtr(handle, GclpHiconSmall, smallIcon);
                        }
                        if (largeIcon != IntPtr.Zero)
                        {
                            SendMessage(handle, WmSetIcon, new IntPtr(1), largeIcon);
                            previousLargeClassIcon = SetClassLongPtr(handle, GclpHicon, largeIcon);
                        }
                    }

                    HwndSource.FromHwnd(handle)?.AddHook((IntPtr hwnd, int message,
                        IntPtr wParam, IntPtr lParam, ref bool handled) =>
                    {
                        if (message == WmDestroy)
                        {
                            try
                            {
                                SetClassLongPtr(hwnd, GclpHiconSmall, previousSmallClassIcon);
                                SetClassLongPtr(hwnd, GclpHicon, previousLargeClassIcon);
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Error(ex, "Could not release taskbar icon.");
                            }
                        }

                        return IntPtr.Zero;
                    });
                    configured = true;
                    AppLogger.Info($"Taskbar icon configured; class icons: " +
                        $"{GetClassLongPtr(handle, GclpHiconSmall) != IntPtr.Zero}/" +
                        $"{GetClassLongPtr(handle, GclpHicon) != IntPtr.Zero}.");
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Could not configure taskbar icon.");
                }
            }

            window.SourceInitialized += (_, _) => Configure();
            new WindowInteropHelper(window).EnsureHandle();
            Configure();
            window.Loaded += (_, _) => Configure();

            window.Closed += (_, _) =>
            {
                if (smallIcon != IntPtr.Zero)
                    DestroyIcon(smallIcon);
                if (largeIcon != IntPtr.Zero && largeIcon != smallIcon)
                    DestroyIcon(largeIcon);
            };
        }

    }
}
