using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace CVDesktopEditor.Services
{
    public class AppIntegrityService
    {
        public IntegrityReport CheckRuntimeFiles()
        {
            var report = new IntegrityReport();

            try
            {
                var executablePath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
                {
                    report.CheckedFiles.Add(CreateFileIntegrityInfo(executablePath));
                }

                var assemblyPath = typeof(AppIntegrityService).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
                {
                    report.CheckedFiles.Add(CreateFileIntegrityInfo(assemblyPath));
                }

                report.IsHealthy = report.CheckedFiles.Count > 0 && report.CheckedFiles.All(file => !string.IsNullOrWhiteSpace(file.Sha256));
            }
            catch (Exception ex)
            {
                report.IsHealthy = false;
                report.Message = ex.Message;
                AppLogger.Error(ex, "Integrity check failed.");
            }

            return report;
        }

        private FileIntegrityInfo CreateFileIntegrityInfo(string path)
        {
            using var stream = File.OpenRead(path);
            var hash = SHA256.HashData(stream);
            var version = FileVersionInfo.GetVersionInfo(path);

            return new FileIntegrityInfo
            {
                Path = path,
                Sha256 = Convert.ToHexString(hash),
                Version = version.FileVersion ?? ""
            };
        }
    }

    public class IntegrityReport
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = "";
        public List<FileIntegrityInfo> CheckedFiles { get; set; } = new();
    }

    public class FileIntegrityInfo
    {
        public string Path { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public string Version { get; set; } = "";
    }
}
