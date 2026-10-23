using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CVDesktopEditor.Services
{
    internal static class TaskbarIconService
    {
        private const int WmSetIcon = 0x0080;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr size, IntPtr icon);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconEx(string file, int index, out IntPtr largeIcon, out IntPtr smallIcon, uint count);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr icon);

        public static void Apply(Window window)
        {
            var smallIcon = IntPtr.Zero;
            var largeIcon = IntPtr.Zero;

            window.Loaded += (_, _) =>
            {
                var executable = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executable) ||
                    ExtractIconEx(executable, 0, out largeIcon, out smallIcon, 1) == 0)
                    return;

                var handle = new WindowInteropHelper(window).Handle;
                if (smallIcon != IntPtr.Zero)
                    SendMessage(handle, WmSetIcon, IntPtr.Zero, smallIcon);
                if (largeIcon != IntPtr.Zero)
                    SendMessage(handle, WmSetIcon, new IntPtr(1), largeIcon);
            };

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
