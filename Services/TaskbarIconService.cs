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
        private const ushort VtLpwstr = 31;
        private static readonly Guid AppUserModelFormatId = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr size, IntPtr icon);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW", SetLastError = true)]
        private static extern IntPtr SetClassLongPtr(IntPtr window, int index, IntPtr value);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
        private static extern IntPtr GetClassLongPtr(IntPtr window, int index);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconEx(string file, int index, out IntPtr largeIcon, out IntPtr smallIcon, uint count);

        [DllImport("shell32.dll")]
        private static extern int SHGetPropertyStoreForWindow(IntPtr window, ref Guid interfaceId,
            [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);

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

                    SetTaskbarIdentity(handle, executable, iconFile);
                    HwndSource.FromHwnd(handle)?.AddHook((IntPtr hwnd, int message,
                        IntPtr wParam, IntPtr lParam, ref bool handled) =>
                    {
                        if (message == WmDestroy)
                        {
                            try
                            {
                                SetClassLongPtr(hwnd, GclpHiconSmall, previousSmallClassIcon);
                                SetClassLongPtr(hwnd, GclpHicon, previousLargeClassIcon);
                                ClearTaskbarIdentity(hwnd);
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Error(ex, "Could not release taskbar identity.");
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

        private static void SetTaskbarIdentity(IntPtr handle, string executable, string iconFile)
        {
            var interfaceId = typeof(IPropertyStore).GUID;
            Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(handle, ref interfaceId, out var store));
            try
            {
                SetString(store, 2, $"\"{executable}\"");
                SetString(store, 3, $"{iconFile},0");
#if ADMIN_BUILD
                SetString(store, 4, "CV Desktop Editor Admin");
#else
                SetString(store, 4, "CV Desktop Editor");
#endif
                SetString(store, 5, Program.AppUserModelId);
                store.Commit();
            }
            finally
            {
                Marshal.ReleaseComObject(store);
            }
        }

        private static void ClearTaskbarIdentity(IntPtr handle)
        {
            var interfaceId = typeof(IPropertyStore).GUID;
            Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(handle, ref interfaceId, out var store));
            try
            {
                for (uint propertyId = 2; propertyId <= 5; propertyId++)
                {
                    var key = new PropertyKey(AppUserModelFormatId, propertyId);
                    var empty = new PropVariant();
                    store.SetValue(ref key, ref empty);
                }

                store.Commit();
            }
            finally
            {
                Marshal.ReleaseComObject(store);
            }
        }

        private static void SetString(IPropertyStore store, uint propertyId, string value)
        {
            var key = new PropertyKey(AppUserModelFormatId, propertyId);
            var variant = new PropVariant { Type = VtLpwstr, Value = Marshal.StringToCoTaskMemUni(value) };
            try
            {
                store.SetValue(ref key, ref variant);
            }
            finally
            {
                Marshal.FreeCoTaskMem(variant.Value);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey(Guid formatId, uint propertyId)
        {
            public Guid FormatId = formatId;
            public uint PropertyId = propertyId;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort Type;
            [FieldOffset(8)] public IntPtr Value;
        }

        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint count);
            void GetAt(uint index, out PropertyKey key);
            void GetValue(ref PropertyKey key, out PropVariant value);
            void SetValue(ref PropertyKey key, ref PropVariant value);
            void Commit();
        }
    }
}
