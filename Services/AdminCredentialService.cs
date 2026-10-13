using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CVDesktopEditor.Services
{
    internal sealed class AdminCredentialService
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CVDesktopEditor.AdminCredential.v1");
        private readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CVDesktopEditor", "admin-credential.dat");

        public string? Load()
        {
            if (!File.Exists(_path))
                return null;

            try
            {
                var encrypted = File.ReadAllBytes(_path);
                var clear = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clear);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Admin credential could not be unlocked.");
                return null;
            }
        }

        public void Save(string key)
        {
            var directory = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(directory);
            var clear = Encoding.UTF8.GetBytes(key);
            try
            {
                var encrypted = ProtectedData.Protect(clear, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_path, encrypted);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(clear);
            }
        }

        public void Clear()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
    }
}
