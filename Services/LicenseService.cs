using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CVDesktopEditor.Services
{
    public class LicenseService
    {
        public const int MaxTrialPdfExportsPerLanguage = 4;

        private readonly string _licensePath;

        public LicenseService()
        {
            var baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CVDesktopEditor");

            Directory.CreateDirectory(baseFolder);
            _licensePath = Path.Combine(baseFolder, "license-state.json");
        }

        public LicenseStatus GetStatus()
        {
            try
            {
                if (!File.Exists(_licensePath))
                    return LicenseStatus.NotActivated();

                var json = File.ReadAllText(_licensePath);
                var state = JsonSerializer.Deserialize<LocalLicenseState>(json);
                if (state == null)
                    return LicenseStatus.Tampered("License state could not be read.");

                if (!ValidateIntegrity(state))
                    return LicenseStatus.Tampered("License integrity check failed.");

                var now = DateTimeOffset.UtcNow;
                if (state.LastSeenUtc > now.AddMinutes(10))
                    return LicenseStatus.Tampered("System clock moved backwards.");

                state.LastSeenUtc = now;
                SaveState(state);

                if (state.Kind != LicenseKind.Trial && state.ExpiresUtc <= now)
                    return LicenseStatus.Expired(state.ExpiresUtc);

                return state.Kind == LicenseKind.Trial
                    ? LicenseStatus.Trial(
                        MaxTrialPdfExportsPerLanguage - state.SpanishPdfExports,
                        MaxTrialPdfExportsPerLanguage - state.EnglishPdfExports)
                    : LicenseStatus.Active(state.ExpiresUtc);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to validate local license state.");
                return LicenseStatus.Tampered("License validation failed.");
            }
        }

        public LicenseStatus StartTrial()
        {
            var now = DateTimeOffset.UtcNow;
            var existing = GetStatus();
            if (existing.State is LicenseState.Trial or LicenseState.Active)
                return existing;

            var state = new LocalLicenseState
            {
                Kind = LicenseKind.Trial,
                DeviceId = GetDeviceId(),
                IssuedUtc = now,
                LastSeenUtc = now,
                ExpiresUtc = DateTimeOffset.MaxValue
            };

            SaveState(state);
            AppLogger.Info("Usage-based local trial started.");
            return LicenseStatus.Trial(MaxTrialPdfExportsPerLanguage, MaxTrialPdfExportsPerLanguage);
        }

        public ExportPermission CheckPdfExportPermission(bool isEnglish)
        {
            var status = GetStatus();
            if (status.State == LicenseState.Active)
                return ExportPermission.Allowed("Licensed export allowed.");

            if (status.State != LicenseState.Trial)
                return ExportPermission.Blocked(status.Message);

            var state = LoadState();
            if (state == null)
                return ExportPermission.Blocked("Trial state could not be read.");

            var used = isEnglish ? state.EnglishPdfExports : state.SpanishPdfExports;
            var remaining = MaxTrialPdfExportsPerLanguage - used;

            return remaining > 0
                ? ExportPermission.Allowed($"Trial export allowed. Remaining exports for this language: {remaining}.")
                : ExportPermission.Blocked("Trial limit reached for this CV language.");
        }

        public LicenseStatus RegisterPdfExport(bool isEnglish)
        {
            var state = LoadState();
            if (state == null)
                return GetStatus();

            if (state.Kind == LicenseKind.Trial)
            {
                if (isEnglish)
                    state.EnglishPdfExports++;
                else
                    state.SpanishPdfExports++;

                SaveState(state);
                AppLogger.Info($"Trial PDF export registered. English={isEnglish}.");
            }

            return GetStatus();
        }

        public LicenseStatus ActivateOfflineDeveloperLicense(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                return LicenseStatus.NotActivated();

            var normalizedKey = licenseKey.Trim();
            var now = DateTimeOffset.UtcNow;

            var state = new LocalLicenseState
            {
                Kind = LicenseKind.Developer,
                DeviceId = GetDeviceId(),
                IssuedUtc = now,
                LastSeenUtc = now,
                ExpiresUtc = now.AddYears(1),
                LicenseKeyFingerprint = Fingerprint(normalizedKey)
            };

            SaveState(state);
            AppLogger.Info("Offline developer license activated.");
            return LicenseStatus.Active(state.ExpiresUtc);
        }

        public async Task<LicenseStatus> ActivateOnlineLicenseAsync(string licenseKey, string apiBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                return LicenseStatus.NotActivated();

            if (string.IsNullOrWhiteSpace(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri))
                return LicenseStatus.Tampered("License API URL is invalid.");

            try
            {
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(baseUri.ToString().TrimEnd('/') + "/"),
                    Timeout = TimeSpan.FromSeconds(20)
                };

                var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "development";
                var response = await httpClient.PostAsJsonAsync("licenses/activate", new
                {
                    LicenseKey = licenseKey.Trim(),
                    DeviceHash = GetDeviceId(),
                    AppVersion = appVersion
                });

                if (!response.IsSuccessStatusCode)
                    return LicenseStatus.Tampered($"License server rejected the request ({(int)response.StatusCode}).");

                var activation = await response.Content.ReadFromJsonAsync<OnlineLicenseActivationResponse>();
                if (activation?.IsActive != true)
                    return LicenseStatus.Tampered(activation?.Message ?? "License was not activated.");

                var now = DateTimeOffset.UtcNow;
                var state = new LocalLicenseState
                {
                    Kind = activation.Status.Equals("developer", StringComparison.OrdinalIgnoreCase)
                        ? LicenseKind.Developer
                        : LicenseKind.Premium,
                    DeviceId = GetDeviceId(),
                    IssuedUtc = now,
                    LastSeenUtc = now,
                    ExpiresUtc = activation.ExpiresAt ?? now.AddYears(1),
                    LicenseKeyFingerprint = Fingerprint(licenseKey.Trim()),
                    ActivationTokenFingerprint = string.IsNullOrWhiteSpace(activation.ActivationToken)
                        ? null
                        : Fingerprint(activation.ActivationToken)
                };

                SaveState(state);
                AppLogger.Info("Online license activated.");
                return LicenseStatus.Active(state.ExpiresUtc);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Online license activation failed.");
                return LicenseStatus.Tampered("Could not reach the license server.");
            }
        }

        private void SaveState(LocalLicenseState state)
        {
            state.IntegrityHash = ComputeIntegrityHash(state);

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_licensePath, json);
        }

        private LocalLicenseState? LoadState()
        {
            if (!File.Exists(_licensePath))
                return null;

            var json = File.ReadAllText(_licensePath);
            var state = JsonSerializer.Deserialize<LocalLicenseState>(json);

            return state != null && ValidateIntegrity(state) ? state : null;
        }

        private bool ValidateIntegrity(LocalLicenseState state)
        {
            if (!state.DeviceId.Equals(GetDeviceId(), StringComparison.Ordinal))
                return false;

            var expectedHash = ComputeIntegrityHash(state);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash),
                Encoding.UTF8.GetBytes(state.IntegrityHash ?? ""));
        }

        private string ComputeIntegrityHash(LocalLicenseState state)
        {
            var material = string.Join("|",
                state.Kind,
                state.DeviceId,
                state.IssuedUtc.ToUnixTimeSeconds(),
                state.LastSeenUtc.ToUnixTimeSeconds(),
                state.ExpiresUtc.ToUnixTimeSeconds(),
                state.SpanishPdfExports,
                state.EnglishPdfExports,
                state.LicenseKeyFingerprint ?? "",
                state.ActivationTokenFingerprint ?? "",
                "CVDesktopEditor.LocalLicense.v1");

            return Fingerprint(material);
        }

        private string GetDeviceId()
        {
            var material = string.Join("|",
                Environment.MachineName,
                Environment.UserName,
                Environment.OSVersion.VersionString);

            return Fingerprint(material);
        }

        private string Fingerprint(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash);
        }
    }

    public enum LicenseKind
    {
        Trial,
        Premium,
        Developer
    }

    public enum LicenseState
    {
        NotActivated,
        Trial,
        Active,
        Expired,
        Tampered
    }

    public class LicenseStatus
    {
        public LicenseState State { get; init; }
        public DateTimeOffset? ExpiresUtc { get; init; }
        public string Message { get; init; } = "";

        public static LicenseStatus NotActivated() => new()
        {
            State = LicenseState.NotActivated,
            Message = "No license active"
        };

        public static LicenseStatus Trial(int spanishExportsRemaining, int englishExportsRemaining) => new()
        {
            State = LicenseState.Trial,
            Message = $"Trial: {Math.Max(0, spanishExportsRemaining)} ES exports, {Math.Max(0, englishExportsRemaining)} EN exports remaining"
        };

        public static LicenseStatus Active(DateTimeOffset expiresUtc) => new()
        {
            State = LicenseState.Active,
            ExpiresUtc = expiresUtc,
            Message = $"License active until {expiresUtc.LocalDateTime:g}"
        };

        public static LicenseStatus Expired(DateTimeOffset expiresUtc) => new()
        {
            State = LicenseState.Expired,
            ExpiresUtc = expiresUtc,
            Message = $"License expired on {expiresUtc.LocalDateTime:g}"
        };

        public static LicenseStatus Tampered(string reason) => new()
        {
            State = LicenseState.Tampered,
            Message = reason
        };
    }

    public class LocalLicenseState
    {
        public LicenseKind Kind { get; set; }
        public string DeviceId { get; set; } = "";
        public DateTimeOffset IssuedUtc { get; set; }
        public DateTimeOffset LastSeenUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public int SpanishPdfExports { get; set; }
        public int EnglishPdfExports { get; set; }
        public string? LicenseKeyFingerprint { get; set; }
        public string? ActivationTokenFingerprint { get; set; }
        public string? IntegrityHash { get; set; }
    }

    public class ExportPermission
    {
        public bool IsAllowed { get; init; }
        public string Message { get; init; } = "";

        public static ExportPermission Allowed(string message) => new()
        {
            IsAllowed = true,
            Message = message
        };

        public static ExportPermission Blocked(string message) => new()
        {
            IsAllowed = false,
            Message = message
        };
    }

    internal class OnlineLicenseActivationResponse
    {
        public bool IsActive { get; set; }
        public string Status { get; set; } = "";
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? ActivationToken { get; set; }
        public string Message { get; set; } = "";
    }
}
