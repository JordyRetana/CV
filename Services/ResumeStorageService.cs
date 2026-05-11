using System.IO;
using System.Text.Json;
using CVDesktopEditor.Models;

namespace CVDesktopEditor.Services
{
    public class ResumeStorageService
    {
        private readonly string _baseFolder;
        private readonly string _jsonPath;

        public ResumeStorageService()
        {
            _baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CVDesktopEditor");

            if (!Directory.Exists(_baseFolder))
                Directory.CreateDirectory(_baseFolder);

            _jsonPath = Path.Combine(_baseFolder, "resume-data.json");
        }

        public ResumeStore Load()
        {
            if (!File.Exists(_jsonPath))
            {
                var empty = new ResumeStore();
                Save(empty);
                return empty;
            }

            try
            {
                var json = File.ReadAllText(_jsonPath);
                return JsonSerializer.Deserialize<ResumeStore>(json) ?? new ResumeStore();
            }
            catch (JsonException)
            {
                BackupInvalidStore();
                var empty = new ResumeStore();
                Save(empty);
                return empty;
            }
            catch (IOException)
            {
                return new ResumeStore();
            }
        }

        public void Save(ResumeStore store)
        {
            var json = JsonSerializer.Serialize(store, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_jsonPath, json);
        }

        public string SavePdfCopy(string originalPath, string languageCode)
        {
            var extension = Path.GetExtension(originalPath);
            var destination = Path.Combine(_baseFolder, $"cv-{languageCode}{extension}");
            File.Copy(originalPath, destination, true);
            return destination;
        }

        public void ClearAll()
        {
            if (File.Exists(_jsonPath))
                File.Delete(_jsonPath);

            var esPdf = Path.Combine(_baseFolder, "cv-es.pdf");
            var enPdf = Path.Combine(_baseFolder, "cv-en.pdf");

            if (File.Exists(esPdf))
                File.Delete(esPdf);

            if (File.Exists(enPdf))
                File.Delete(enPdf);
        }

        private void BackupInvalidStore()
        {
            if (!File.Exists(_jsonPath))
                return;

            var backupPath = Path.Combine(
                _baseFolder,
                $"resume-data.invalid-{DateTime.Now:yyyyMMdd-HHmmss}.bak");

            File.Copy(_jsonPath, backupPath, true);
        }
    }
}
