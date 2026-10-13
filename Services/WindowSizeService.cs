using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace CVDesktopEditor.Services
{
    internal static class WindowSizeService
    {
        public static void KeepTaskbarVisible(Window window)
        {
            window.SourceInitialized += (_, _) =>
            {
                var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source?.AddHook(HandleMinMaxInfo);
            };
        }

        public static void FitToWorkArea(Window window)
        {
            window.SourceInitialized += (_, _) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                var monitor = MonitorFromWindow(handle, 2);
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (!GetMonitorInfo(monitor, ref info))
                    return;

                var source = PresentationSource.FromVisual(window);
                var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
                var topLeft = transform.Transform(new Point(info.Work.Left, info.Work.Top));
                var bottomRight = transform.Transform(new Point(info.Work.Right, info.Work.Bottom));
                var availableWidth = bottomRight.X - topLeft.X - 24;
                var availableHeight = bottomRight.Y - topLeft.Y - 24;

                window.MinWidth = Math.Min(window.MinWidth, availableWidth);
                window.MinHeight = Math.Min(window.MinHeight, availableHeight);
                window.Width = Math.Min(window.Width, availableWidth);
                window.Height = Math.Min(window.Height, availableHeight);
                window.Left = topLeft.X + (bottomRight.X - topLeft.X - window.Width) / 2;
                window.Top = topLeft.Y + (bottomRight.Y - topLeft.Y - window.Height) / 2;
            };
        }

        private static IntPtr HandleMinMaxInfo(IntPtr handle, int message, IntPtr wParam,
            IntPtr lParam, ref bool handled)
        {
            if (message != 0x0024)
                return IntPtr.Zero;

            var monitor = MonitorFromWindow(handle, 2);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return IntPtr.Zero;

            var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            minMax.MaxPosition.X = info.Work.Left - info.Monitor.Left;
            minMax.MaxPosition.Y = info.Work.Top - info.Monitor.Top;
            minMax.MaxSize.X = info.Work.Right - info.Work.Left;
            minMax.MaxSize.Y = info.Work.Bottom - info.Work.Top;
            Marshal.StructureToPtr(minMax, lParam, false);
            handled = true;
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaxSize;
            public NativePoint MaxPosition;
            public NativePoint MinTrackSize;
            public NativePoint MaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public uint Flags;
        }
    }
}
