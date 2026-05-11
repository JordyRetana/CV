using System.Security.Cryptography;
using System.Text;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(NormalizePostgresConnectionString(GetRequiredSetting(builder.Configuration, "SUPABASE_CONNECTION_STRING"))));
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    product = "CV Desktop Editor License API",
    status = "ok",
    version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "development"
}));

app.MapPost("/licenses/activate", async (
    ActivateLicenseRequest request,
    NpgsqlDataSource db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.LicenseKey) ||
        string.IsNullOrWhiteSpace(request.DeviceHash) ||
        string.IsNullOrWhiteSpace(request.AppVersion))
    {
        return Results.BadRequest(new LicenseErrorResponse("missing_fields", "License key, device hash and app version are required."));
    }

    await using var connection = await db.OpenConnectionAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    var license = await FindLicenseAsync(connection, request.LicenseKey, cancellationToken);
    if (license is null)
    {
        await InsertActivationAsync(connection, null, request.DeviceHash, request.AppVersion, "invalid_key", httpContext, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Unauthorized();
    }

    if (!license.UserIsActive || !license.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
    {
        await InsertActivationAsync(connection, license.Id, request.DeviceHash, request.AppVersion, "inactive", httpContext, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Json(new LicenseActivationResponse(false, "inactive", license.ExpiresAt, null, "License is inactive."), statusCode: StatusCodes.Status403Forbidden);
    }

    if (license.ExpiresAt is not null && license.ExpiresAt <= DateTimeOffset.UtcNow)
    {
        await InsertActivationAsync(connection, license.Id, request.DeviceHash, request.AppVersion, "expired", httpContext, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Json(new LicenseActivationResponse(false, "expired", license.ExpiresAt, null, "License is expired."), statusCode: StatusCodes.Status403Forbidden);
    }

    var deviceCount = await CountDevicesAsync(connection, license.Id, cancellationToken);
    var deviceExists = await DeviceExistsAsync(connection, license.Id, request.DeviceHash, cancellationToken);
    if (!deviceExists && deviceCount >= license.MaxDevices)
    {
        await InsertActivationAsync(connection, license.Id, request.DeviceHash, request.AppVersion, "device_limit", httpContext, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Json(new LicenseActivationResponse(false, "device_limit", license.ExpiresAt, null, "Device limit reached."), statusCode: StatusCodes.Status403Forbidden);
    }

    await UpsertDeviceAsync(connection, license.Id, request.DeviceHash, cancellationToken);
    await InsertActivationAsync(connection, license.Id, request.DeviceHash, request.AppVersion, "activated", httpContext, cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    return Results.Ok(new LicenseActivationResponse(
        true,
        license.Kind,
        license.ExpiresAt,
        CreateActivationToken(license.Id, request.DeviceHash),
        "License activated."));
});

app.MapPost("/admin/licenses", async (
    CreateLicenseRequest request,
    NpgsqlDataSource db,
    HttpContext httpContext,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminRequest(httpContext, configuration))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new LicenseErrorResponse("missing_email", "Email is required."));

    var licenseKey = GenerateLicenseKey();
    var licenseKeyHash = HashSecret(licenseKey);

    await using var connection = await db.OpenConnectionAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    var userId = await UpsertUserAsync(connection, request.Email, request.FullName, cancellationToken);
    var licenseId = await InsertLicenseAsync(
        connection,
        userId,
        licenseKeyHash,
        request.Kind ?? "premium",
        request.MaxDevices <= 0 ? 1 : request.MaxDevices,
        request.ExpiresAt,
        cancellationToken);

    await InsertAuditLogAsync(connection, "admin", "license.created", "license", licenseId.ToString(), cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    return Results.Ok(new CreateLicenseResponse(licenseId, userId, licenseKey, request.ExpiresAt));
});

app.MapPost("/admin/database/initialize", async (
    NpgsqlDataSource db,
    HttpContext httpContext,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminRequest(httpContext, configuration))
        return Results.Unauthorized();

    var schemaPath = Path.Combine(AppContext.BaseDirectory, "database", "001_license_schema.sql");
    if (!File.Exists(schemaPath))
        return Results.Problem($"Schema file was not found at {schemaPath}.");

    var sql = await File.ReadAllTextAsync(schemaPath, cancellationToken);
    await using var connection = await db.OpenConnectionAsync(cancellationToken);
    await using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync(cancellationToken);

    return Results.Ok(new
    {
        status = "initialized",
        schema = "001_license_schema.sql"
    });
});

app.MapGet("/admin/licenses", async (
    NpgsqlDataSource db,
    HttpContext httpContext,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminRequest(httpContext, configuration))
        return Results.Unauthorized();

    const string sql = """
        select l.id, u.email, u.full_name, l.status, l.kind, l.max_devices, l.expires_at, l.created_at,
               count(d.id)::int as activated_devices
        from licenses l
        left join app_users u on u.id = l.user_id
        left join devices d on d.license_id = l.id
        group by l.id, u.email, u.full_name
        order by l.created_at desc
        limit 100;
        """;

    var licenses = new List<AdminLicenseSummary>();
    await using var connection = await db.OpenConnectionAsync(cancellationToken);
    await using var command = new NpgsqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
        licenses.Add(new AdminLicenseSummary(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt32(5),
            reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            reader.GetFieldValue<DateTimeOffset>(7),
            reader.GetInt32(8)));
    }

    return Results.Ok(licenses);
});

app.Run();

static string GetRequiredSetting(IConfiguration configuration, string key)
{
    return configuration[key]
        ?? Environment.GetEnvironmentVariable(key)
        ?? throw new InvalidOperationException($"{key} must be configured.");
}

static string NormalizePostgresConnectionString(string connectionString)
{
    if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var database = uri.AbsolutePath.TrimStart('/');

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = database,
        Username = username,
        Password = password,
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}

static bool IsAdminRequest(HttpContext httpContext, IConfiguration configuration)
{
    var configuredKey = configuration["ADMIN_API_KEY"] ?? Environment.GetEnvironmentVariable("ADMIN_API_KEY");
    if (string.IsNullOrWhiteSpace(configuredKey))
        return false;

    var providedKey = httpContext.Request.Headers["X-Admin-Key"].ToString();
    return FixedTimeEquals(providedKey, configuredKey);
}

static bool FixedTimeEquals(string left, string right)
{
    var leftBytes = Encoding.UTF8.GetBytes(left);
    var rightBytes = Encoding.UTF8.GetBytes(right);
    return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}

static string GenerateLicenseKey()
{
    const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    Span<byte> bytes = stackalloc byte[20];
    RandomNumberGenerator.Fill(bytes);

    var chars = bytes.ToArray().Select(value => alphabet[value % alphabet.Length]).ToArray();
    return $"CVE-{new string(chars[..5])}-{new string(chars[5..10])}-{new string(chars[10..15])}-{new string(chars[15..20])}";
}

static string HashSecret(string value)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
    return Convert.ToHexString(hash);
}

static string CreateActivationToken(Guid licenseId, string deviceHash)
{
    var payload = $"{licenseId:N}:{deviceHash}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
}

static async Task<LicenseRow?> FindLicenseAsync(NpgsqlConnection connection, string licenseKey, CancellationToken cancellationToken)
{
    const string sql = """
        select l.id, l.status, l.kind, l.max_devices, l.expires_at, coalesce(u.is_active, true) as user_is_active
        from licenses l
        left join app_users u on u.id = l.user_id
        where l.license_key_hash = @license_key_hash
        limit 1;
        """;

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("license_key_hash", HashSecret(licenseKey));

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    if (!await reader.ReadAsync(cancellationToken))
        return null;

    return new LicenseRow(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetInt32(3),
        reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
        reader.GetBoolean(5));
}

static async Task<Guid> UpsertUserAsync(NpgsqlConnection connection, string email, string? fullName, CancellationToken cancellationToken)
{
    const string sql = """
        insert into app_users (email, full_name)
        values (@email, @full_name)
        on conflict (email) do update set full_name = coalesce(excluded.full_name, app_users.full_name)
        returning id;
        """;

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
    command.Parameters.AddWithValue("full_name", (object?)fullName ?? DBNull.Value);
    return (Guid)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("User could not be created."));
}

static async Task<Guid> InsertLicenseAsync(
    NpgsqlConnection connection,
    Guid userId,
    string licenseKeyHash,
    string kind,
    int maxDevices,
    DateTimeOffset? expiresAt,
    CancellationToken cancellationToken)
{
    const string sql = """
        insert into licenses (user_id, license_key_hash, kind, max_devices, expires_at)
        values (@user_id, @license_key_hash, @kind, @max_devices, @expires_at)
        returning id;
        """;

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("user_id", userId);
    command.Parameters.AddWithValue("license_key_hash", licenseKeyHash);
    command.Parameters.AddWithValue("kind", kind);
    command.Parameters.AddWithValue("max_devices", maxDevices);
    command.Parameters.AddWithValue("expires_at", (object?)expiresAt ?? DBNull.Value);
    return (Guid)(await command.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("License could not be created."));
}

static async Task<int> CountDevicesAsync(NpgsqlConnection connection, Guid licenseId, CancellationToken cancellationToken)
{
    await using var command = new NpgsqlCommand("select count(*)::int from devices where license_id = @license_id;", connection);
    command.Parameters.AddWithValue("license_id", licenseId);
    return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
}

static async Task<bool> DeviceExistsAsync(NpgsqlConnection connection, Guid licenseId, string deviceHash, CancellationToken cancellationToken)
{
    await using var command = new NpgsqlCommand("select exists(select 1 from devices where license_id = @license_id and device_hash = @device_hash);", connection);
    command.Parameters.AddWithValue("license_id", licenseId);
    command.Parameters.AddWithValue("device_hash", deviceHash);
    return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
}

static async Task UpsertDeviceAsync(NpgsqlConnection connection, Guid licenseId, string deviceHash, CancellationToken cancellationToken)
{
    const string sql = """
        insert into devices (license_id, device_hash)
        values (@license_id, @device_hash)
        on conflict (license_id, device_hash) do update set last_seen_at = now();
        """;

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("license_id", licenseId);
    command.Parameters.AddWithValue("device_hash", deviceHash);
    await command.ExecuteNonQueryAsync(cancellationToken);
}

static async Task InsertActivationAsync(
    NpgsqlConnection connection,
    Guid? licenseId,
    string deviceHash,
    string appVersion,
    string status,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    const string sql = """
        insert into activations (license_id, device_hash, app_version, status, ip_address)
        values (@license_id, @device_hash, @app_version, @status, @ip_address);
        """;

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("license_id", (object?)licenseId ?? DBNull.Value);
    command.Parameters.AddWithValue("device_hash", deviceHash);
    command.Parameters.AddWithValue("app_version", appVersion);
    command.Parameters.AddWithValue("status", status);
    command.Parameters.AddWithValue("ip_address", (object?)httpContext.Connection.RemoteIpAddress?.ToString() ?? DBNull.Value);
    await command.ExecuteNonQueryAsync(cancellationToken);
}

static async Task InsertAuditLogAsync(
    NpgsqlConnection connection,
    string actor,
    string action,
    string entityType,
    string entityId,
    CancellationToken cancellationToken)
{
    const string sql = """
        insert into audit_logs (actor, action, entity_type, entity_id)
        values (@actor, @action, @entity_type, @entity_id);
        """;

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("actor", actor);
    command.Parameters.AddWithValue("action", action);
    command.Parameters.AddWithValue("entity_type", entityType);
    command.Parameters.AddWithValue("entity_id", entityId);
    await command.ExecuteNonQueryAsync(cancellationToken);
}

public sealed record ActivateLicenseRequest(string LicenseKey, string DeviceHash, string AppVersion);
public sealed record LicenseActivationResponse(bool IsActive, string Status, DateTimeOffset? ExpiresAt, string? ActivationToken, string Message);
public sealed record LicenseErrorResponse(string Code, string Message);
public sealed record CreateLicenseRequest(string Email, string? FullName, string? Kind, int MaxDevices, DateTimeOffset? ExpiresAt);
public sealed record CreateLicenseResponse(Guid LicenseId, Guid UserId, string LicenseKey, DateTimeOffset? ExpiresAt);
public sealed record AdminLicenseSummary(Guid Id, string Email, string? FullName, string Status, string Kind, int MaxDevices, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt, int ActivatedDevices);
public sealed record LicenseRow(Guid Id, string Status, string Kind, int MaxDevices, DateTimeOffset? ExpiresAt, bool UserIsActive);
