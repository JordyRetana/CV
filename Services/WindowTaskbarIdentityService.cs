using System.IO;
using System.Runtime.InteropServices;

namespace CVDesktopEditor.Services
{
    internal static class WindowTaskbarIdentityService
    {
        private static readonly Guid AppModelFormat = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
        private static readonly Guid PropertyStoreId = new("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99");

        [DllImport("shell32.dll")]
        private static extern int SHGetPropertyStoreForWindow(
            IntPtr window, ref Guid interfaceId, out IPropertyStore store);

        internal static void Apply(IntPtr window, string iconPath)
        {
            if (window == IntPtr.Zero || !File.Exists(iconPath))
                return;

            var installedExe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CVDesktopEditorClient", "current", "CVDesktopEditor.exe");
            if (!string.Equals(Environment.ProcessPath, installedExe, StringComparison.OrdinalIgnoreCase))
                return;

            IPropertyStore? store = null;
            try
            {
                var interfaceId = PropertyStoreId;
                Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(window, ref interfaceId, out store));
                SetString(store, 3, iconPath + ",0");
                SetString(store, 5, "JordyRetana.CVDesktopEditor.Client");
                store.Commit();
                AppLogger.Info("Client taskbar identity configured.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Could not configure client taskbar identity.");
            }
            finally
            {
                if (store is not null)
                    Marshal.ReleaseComObject(store);
            }
        }

        private static void SetString(IPropertyStore store, uint propertyId, string value)
        {
            var key = new PropertyKey(AppModelFormat, propertyId);
            var variant = new PropVariant
            {
                Type = 31, // VT_LPWSTR
                Value = Marshal.StringToCoTaskMemUni(value)
            };

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

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort Type;
            [FieldOffset(8)] public IntPtr Value;
        }

        [ComImport]
        [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
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
