using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace CVDesktopEditor.Services
{
    internal static class InstalledShortcutService
    {
#if ADMIN_BUILD
        private const string PackageId = "CVDesktopEditorStudioAdmin";
        private const string ShortcutName = "CV Desktop Editor Admin.lnk";
        private const string ExecutableName = "CVDesktopEditorStudioAdmin.exe";
#else
        private const string PackageId = "CVDesktopEditorClient";
        private const string ShortcutName = "CV Desktop Editor.lnk";
        private const string ExecutableName = "CVDesktopEditor.exe";
#endif

        private static readonly Guid ShellLinkClsid = new("00021401-0000-0000-C000-000000000046");

        public static void Repair()
        {
            try
            {
                var executable = Environment.ProcessPath;
                var installDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageId, "current");
                if (string.IsNullOrWhiteSpace(executable) ||
                    !Path.GetFullPath(executable).Equals(
                        Path.GetFullPath(Path.Combine(installDirectory, ExecutableName)),
                        StringComparison.OrdinalIgnoreCase))
                    return;

                var icon = Path.Combine(installDirectory, "Assets", "shortcut-icon.ico");
                if (!File.Exists(icon))
                    return;

                var startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", ShortcutName);
                var desktop = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortcutName);

                var launcher = Path.Combine(Path.GetDirectoryName(installDirectory)!, "Update.exe");
                if (!File.Exists(launcher))
                    return;

                RepairShortcut(startMenu, executable, launcher, icon);
                RepairShortcut(desktop, executable, launcher, icon);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Could not repair installed shortcuts.");
            }
        }

        private static void RepairShortcut(string path, string executable, string launcher, string icon)
        {
            if (!File.Exists(path))
                return;

            object? existing = null;
            object? replacement = null;
            try
            {
                existing = Activator.CreateInstance(Type.GetTypeFromCLSID(ShellLinkClsid)!);
                ((IPersistFile)existing!).Load(path, 0);
                var currentTarget = new StringBuilder(1024);
                var existingLink = (IShellLinkW)existing;
                existingLink.GetPath(currentTarget, currentTarget.Capacity, IntPtr.Zero, 0);
                var target = Path.GetFullPath(currentTarget.ToString());
                var arguments = new StringBuilder(1024);
                existingLink.GetArguments(arguments, arguments.Capacity);
                var samePackageLauncher =
                    string.Equals(Path.GetFileName(target), "Update.exe", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Path.GetFileName(Path.GetDirectoryName(target)), PackageId, StringComparison.OrdinalIgnoreCase) &&
                    arguments.ToString().Equals($"start {ExecutableName}", StringComparison.Ordinal);
                if (!target.Equals(Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase) &&
                    !target.Equals(Path.GetFullPath(launcher), StringComparison.OrdinalIgnoreCase) &&
                    !samePackageLauncher)
                    return;

                if (target.Equals(Path.GetFullPath(launcher), StringComparison.OrdinalIgnoreCase))
                {
                    var iconPath = new StringBuilder(1024);
                    existingLink.GetIconLocation(iconPath, iconPath.Capacity, out var iconIndex);
                    if (arguments.ToString().Equals($"start {ExecutableName}", StringComparison.Ordinal) &&
                        iconPath.ToString().Equals(icon, StringComparison.OrdinalIgnoreCase) &&
                        iconIndex == 0)
                        return;
                }

                replacement = Activator.CreateInstance(Type.GetTypeFromCLSID(ShellLinkClsid)!);
                var link = (IShellLinkW)replacement!;
                link.SetPath(launcher);
                link.SetArguments($"start {ExecutableName}");
                link.SetWorkingDirectory(Path.GetDirectoryName(launcher)!);
                link.SetDescription("CV Desktop Editor");
                link.SetIconLocation(icon, 0);
                ((IPersistFile)replacement!).Save(path, true);
                AppLogger.Info($"Repaired shortcut icon: {path}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Could not repair shortcut: {path}");
            }
            finally
            {
                if (existing is not null)
                    Marshal.ReleaseComObject(existing);
                if (replacement is not null)
                    Marshal.ReleaseComObject(replacement);
            }
        }

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file,
                int maxPath, IntPtr findData, uint flags);
            void GetIDList(out IntPtr idList);
            void SetIDList(IntPtr idList);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int maxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int maxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int maxArgs);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
            void GetHotkey(out short hotkey);
            void SetHotkey(short hotkey);
            void GetShowCmd(out int showCommand);
            void SetShowCmd(int showCommand);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder iconPath,
                int maxPath, out int iconIndex);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr window, uint flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }
    }
}
