using InventarioCPR.Components;
using InventarioCPR.Application.Photos;
using InventarioCPR.Application.Inventory;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Photos;
using InventarioCPR.Infrastructure.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CloudinaryDotNet;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--check-lan-preparation", StringComparer.Ordinal))
{
    Console.WriteLine($"LAN: autorización configurada={builder.Configuration.GetValue<bool>("Inventory:LanAccessApproved")}; " +
        $"certificado explícito configurado={!string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Path"]) && !string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Password"])}. Valores omitidos.");
    return;
}

if (args.Contains("--bootstrap-security", StringComparer.Ordinal))
    throw new InvalidOperationException("El bootstrap temporal fue retirado tras crear el administrador de prueba.");

if (args.Contains("--check-local-identity-config", StringComparer.Ordinal))
{
    var configuration = builder.Configuration;
    var certificateConfigured = !string.IsNullOrWhiteSpace(configuration["Kestrel:Certificates:Default:Path"]) &&
        !string.IsNullOrWhiteSpace(configuration["Kestrel:Certificates:Default:Password"]);
    var httpsConfigured = Uri.TryCreate(configuration["Inventory:ListenUrl"], UriKind.Absolute, out var address) &&
        address.Scheme == Uri.UriSchemeHttps;
    Console.WriteLine($"Identity local: habilitada={configuration.GetValue("Security:EnableIdentity", true)}; " +
        $"HTTPS configurado={httpsConfigured}; " +
        $"certificado explícito configurado={certificateConfigured}. Valores y credenciales omitidos.");
    return;
}

if (args.Contains("--check-auth-test-schema", StringComparer.Ordinal))
{
    try
    {
        await using var authDb = new AccessDbContextFactory().CreateDbContext(["--auth-test"]);
        var connection = (NpgsqlConnection)authDb.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var database = new NpgsqlCommand("SELECT current_database()", connection);
        var name = (string?)await database.ExecuteScalarAsync();
        await using var tables = new NpgsqlCommand(
            "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname = 'public' AND tablename IN " +
            "('access_users','access_roles','access_user_roles','access_user_claims','access_role_claims','access_user_logins','access_user_tokens')", connection);
        var count = (long?)await tables.ExecuteScalarAsync();
        await using var history = new NpgsqlCommand(
            "SELECT to_regclass('public.\"__AuthMigrationsHistory\"') IS NOT NULL", connection);
        var historyExists = (bool?)await history.ExecuteScalarAsync() == true;
        Console.WriteLine($"Identity en rama de pruebas: base={name}; tablas={count}/7; historial={(historyExists ? "existe" : "ausente")}. Credenciales omitidas.");
        if (count == 7 && historyExists)
        {
            await using var entries = new NpgsqlCommand(
                "SELECT (SELECT count(*) FROM public.access_users), (SELECT count(*) FROM public.access_roles)", connection);
            await using var reader = await entries.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                Console.WriteLine($"Identity en rama de pruebas: cuentas={reader.GetInt64(0)}; roles={reader.GetInt64(1)}.");
            await reader.CloseAsync();
            await using var assignment = new NpgsqlCommand(
                "SELECT (SELECT count(*) FROM public.access_roles WHERE \"Name\" IN ('Administrador','Sistemas','Consulta')), " +
                "(SELECT count(*) FROM public.access_users u JOIN public.access_user_roles ur ON ur.\"UserId\" = u.\"Id\" " +
                "JOIN public.access_roles r ON r.\"Id\" = ur.\"RoleId\" " +
                "WHERE r.\"Name\" = 'Administrador' AND u.\"IsActive\" AND u.\"PasswordHash\" IS NOT NULL)", connection);
            await using var assignmentReader = await assignment.ExecuteReaderAsync();
            if (await assignmentReader.ReadAsync())
                Console.WriteLine($"Identity en rama de pruebas: roles provisionales={assignmentReader.GetInt64(0)}/3; " +
                    $"administrador activo con hash y rol={assignmentReader.GetInt64(1)}.");
            await assignmentReader.CloseAsync();
            await using var accountState = new NpgsqlCommand(
                "SELECT COALESCE(MAX(\"AccessFailedCount\"), 0), " +
                "COUNT(*) FILTER (WHERE \"LockoutEnd\" > now()) FROM public.access_users", connection);
            await using var stateReader = await accountState.ExecuteReaderAsync();
            if (await stateReader.ReadAsync())
                Console.WriteLine($"Identity en rama de pruebas: intentos fallidos (máximo)={stateReader.GetInt32(0)}; " +
                    $"cuentas bloqueadas actualmente={stateReader.GetInt64(1)}.");
        }
    }
    catch (Exception ex) { Console.WriteLine($"Identity en rama de pruebas: no verificada ({ex.GetType().Name})."); }
    return;
}

if (args.Contains("--check-development-app", StringComparer.Ordinal))
{
    try
    {
        var connectionString = NeonConnection.DevelopmentApp(builder.Configuration);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var check = new NpgsqlCommand(
            "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'", connection);
        var count = (long?)await check.ExecuteScalarAsync();
        await using var history = new NpgsqlCommand(
            "SELECT count(*) FROM public.\"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%_InitialInventorySchema'", connection);
        var applied = (long?)await history.ExecuteScalarAsync() == 1;
        Console.WriteLine($"Aplicación development: {(count == 20 && applied ? "esquema verificado" : "esquema inesperado")}; " +
            $"tablas={count}; migración={(applied ? "presente" : "ausente")}. Host y credenciales omitidos.");
    }
    catch (Exception ex) { Console.WriteLine($"Aplicación development: conexión no verificada ({ex.GetType().Name})."); }
    return;
}

if (args.Contains("--check-migration-test-config", StringComparer.Ordinal))
{
    Console.WriteLine(string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("NeonMigrationTest"))
        ? "NeonMigrationTest: no configurada."
        : "NeonMigrationTest: configurada; rama aún sin verificar.");
    return;
}

if (args.Contains("--check-migration-test-connection", StringComparer.Ordinal))
{
    var raw = builder.Configuration.GetConnectionString("NeonMigrationTest");
    var expectedHost = builder.Configuration["NeonMigrationTest:ExpectedHost"];
    if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(expectedHost))
    {
        Console.WriteLine("NeonMigrationTest: configuración incompleta.");
        return;
    }

    try
    {
        var test = new NpgsqlConnectionStringBuilder(raw) { SslMode = SslMode.VerifyFull, Timeout = 10 };
        var host = (test.Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var expected = expectedHost.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var defaultRaw = builder.Configuration.GetConnectionString("NeonDirect");
        var defaultHost = string.IsNullOrWhiteSpace(defaultRaw) ? "" :
            (new NpgsqlConnectionStringBuilder(defaultRaw).Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(host, expected, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, defaultHost, StringComparison.OrdinalIgnoreCase) || test.Database != "neondb")
            throw new InvalidOperationException("El destino de prueba no supera la comparación local de host y base.");

        await using var connection = new NpgsqlConnection(test.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT current_database()", connection);
        var database = (string?)await command.ExecuteScalarAsync();
        Console.WriteLine(database == "neondb"
            ? "NeonMigrationTest: SELECT correcto en neondb con TLS VerifyFull; verificar host contra Neon."
            : "NeonMigrationTest: base inesperada; rama sin verificar.");
        if (database == "neondb")
        {
            await using var inventory = new NpgsqlCommand(
                "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname = 'public' " +
                "AND tablename <> '__EFMigrationsHistory'", connection);
            Console.WriteLine($"NeonMigrationTest: tablas de usuario en public: {await inventory.ExecuteScalarAsync()}.");
            await using var history = new NpgsqlCommand(
                "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL", connection);
            Console.WriteLine((bool?)await history.ExecuteScalarAsync() == true
                ? "NeonMigrationTest: historial EF existente."
                : "NeonMigrationTest: historial EF ausente.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"NeonMigrationTest: conexión no verificada ({ex.GetType().Name}).");
    }
    return;
}

if (args.Contains("--check-connections", StringComparer.Ordinal))
{
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("NeonDirect")))
    {
        Console.WriteLine("Neon: credenciales no configuradas.");
    }
    else
    {
        var neonStep = "validación de configuración";
        try
        {
            var secure = new NpgsqlConnectionStringBuilder(NeonConnection.Direct(builder.Configuration))
            {
                Timeout = 5,
                CommandTimeout = 5
            };
            await using var connection = new NpgsqlConnection(secure.ConnectionString);
            neonStep = "apertura de conexión";
            await connection.OpenAsync();
            neonStep = "SELECT 1";
            await using (var command = new NpgsqlCommand("SELECT 1", connection))
            {
                if ((int?)await command.ExecuteScalarAsync() != 1)
                    throw new InvalidOperationException("Consulta de comprobación fallida.");
            }

            neonStep = "consulta de base conectada";
            await using (var command = new NpgsqlCommand("SELECT current_database()", connection))
            {
                var database = (string?)await command.ExecuteScalarAsync();
                if (string.IsNullOrWhiteSpace(database))
                    throw new InvalidOperationException("No se pudo identificar la base conectada.");
                Console.WriteLine("Neon: conexión TLS con certificado verificado y SELECT 1 correcto.");
                Console.WriteLine($"Base conectada: {database}");
            }

            neonStep = "lectura de tablas";
            await using (var command = new NpgsqlCommand(
                "SELECT schemaname, tablename FROM pg_catalog.pg_tables " +
                "WHERE schemaname NOT IN ('pg_catalog', 'information_schema') " +
                "ORDER BY schemaname, tablename", connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                var found = false;
                while (await reader.ReadAsync())
                {
                    found = true;
                    Console.WriteLine($"Tabla: {reader.GetString(0)}.{reader.GetString(1)}");
                }
                if (!found)
                    Console.WriteLine("Tablas de usuario: ninguna visible para esta cuenta.");
            }

            neonStep = "historial de migraciones";
            await using (var command = new NpgsqlCommand(
                "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL", connection))
            {
                var historyExists = (bool?)await command.ExecuteScalarAsync() == true;
                Console.WriteLine(historyExists
                    ? "Historial EF en public: existe; revisar migraciones antes de generar una inicial."
                    : "Historial EF en public: no existe.");
            }
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            Console.WriteLine("Neon: contraseña o usuario rechazados por PostgreSQL.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            Console.WriteLine("Neon: la base de datos indicada no existe.");
        }
        catch (Exception ex) when (ex is System.Security.Authentication.AuthenticationException ||
                                   ex.InnerException is System.Security.Authentication.AuthenticationException)
        {
            Console.WriteLine("Neon: fallo al verificar el certificado TLS.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Neon: fallo en {neonStep} ({ex.GetType().Name}" +
                              (ex.InnerException is null ? ")." : $", {ex.InnerException.GetType().Name})."));
        }
    }

    var cloudName = builder.Configuration["Cloudinary:CloudName"];
    var apiKey = builder.Configuration["Cloudinary:ApiKey"];
    var apiSecret = builder.Configuration["Cloudinary:ApiSecret"];
    if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        Console.WriteLine("Cloudinary: credenciales no configuradas; API no consultada.");
    else
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
            var usage = await cloudinary.GetUsageAsync(timeout.Token);
            Console.WriteLine(usage.Error is null
                ? "Cloudinary: consulta autenticada de solo lectura correcta."
                : "Cloudinary: consulta autenticada no verificada.");
        }
        catch
        {
            Console.WriteLine("Cloudinary: consulta autenticada no verificada; revise credenciales y red localmente.");
        }
    }
    return;
}

// Add services to the container.
var identityEnabled = builder.Configuration.GetValue("Security:EnableIdentity", true);
var anonymousLocalTests = builder.Configuration.GetValue<bool>("Inventory:AllowAnonymousLocalTests");
var renderEnvironment = builder.Configuration.GetValue<bool>("RENDER");
var renderDeployment = !builder.Environment.IsDevelopment() &&
    (renderEnvironment || builder.Configuration["Deployment:Provider"] == "Render");
if (!identityEnabled && (!builder.Environment.IsDevelopment() || !anonymousLocalTests))
    throw new InvalidOperationException("Identity es obligatoria; el modo anónimo exige opt-in explícito de pruebas en Development.");
if (identityEnabled && anonymousLocalTests)
    throw new InvalidOperationException("No combine Identity con el modo anónimo de pruebas.");
if (!builder.Environment.IsDevelopment() && !renderDeployment)
    throw new InvalidOperationException("Production solo admite el proveedor de despliegue revisado.");
if (renderEnvironment && builder.Configuration["RENDER_SERVICE_TYPE"] is { Length: > 0 } serviceType && serviceType != "web")
    throw new InvalidOperationException("InventarioCPR requiere un Web Service de Render.");
if (renderDeployment && (builder.Configuration["AllowedHosts"] is null or "" or "*"))
{
    var renderHost = builder.Configuration["RENDER_EXTERNAL_HOSTNAME"];
    if (string.IsNullOrWhiteSpace(renderHost))
        throw new InvalidOperationException("Configure AllowedHosts con el hostname público de Render.");
    builder.Configuration["AllowedHosts"] = renderHost;
}

var port = 0;
var portValue = builder.Configuration["PORT"] ?? (renderEnvironment ? "10000" : null);
if (renderDeployment && (!int.TryParse(portValue, out port) || port is < 1 or > 65535))
    throw new InvalidOperationException("Render debe proporcionar un PORT válido.");
var listenUrl = renderDeployment
    ? $"http://0.0.0.0:{port}"
    : builder.Configuration["Inventory:ListenUrl"] ??
      (identityEnabled ? "https://localhost:7228" : "http://127.0.0.1:5275");
if (!Uri.TryCreate(listenUrl, UriKind.Absolute, out var localAddress) ||
    (renderDeployment && (localAddress.Scheme != Uri.UriSchemeHttp || localAddress.Host != "0.0.0.0")) ||
    (!identityEnabled && (localAddress.Scheme != Uri.UriSchemeHttp || localAddress.Host != "127.0.0.1")) ||
    (identityEnabled && !renderDeployment && localAddress.Scheme != Uri.UriSchemeHttps))
    throw new InvalidOperationException("Pruebas anónimas solo en HTTP 127.0.0.1; Identity requiere HTTPS.");
var localIdentity = identityEnabled && !renderDeployment &&
    localAddress.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
System.Net.IPNetwork? allowedNetwork = null;
if (identityEnabled && !localIdentity && !renderDeployment)
{
    if (!builder.Configuration.GetValue<bool>("Inventory:LanAccessApproved"))
        throw new InvalidOperationException("El perfil LAN está preparado, pero requiere autorización explícita para habilitarse.");
    if (!System.Net.IPNetwork.TryParse(builder.Configuration["Inventory:AllowedOfficeNetwork"], out var parsedNetwork) ||
        !IPAddress.TryParse(localAddress.Host, out var listenIp) ||
        parsedNetwork.PrefixLength < 16 || !IsPrivateIpv4(parsedNetwork.BaseAddress) ||
        !parsedNetwork.Contains(listenIp) || !IsPrivateIpv4(listenIp))
        throw new InvalidOperationException("Configure un IP privado y la subred de oficina permitida para HTTPS.");
    allowedNetwork = parsedNetwork;
}
// Localhost uses the already-trusted ASP.NET Core dev certificate; LAN requires an explicit certificate.
if (localIdentity && (!string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Path"]) ||
                      !string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Password"])))
    throw new InvalidOperationException("El perfil HTTPS localhost debe utilizar el certificado de desarrollo de .NET.");
if (identityEnabled && !localIdentity && !renderDeployment && (string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Path"]) ||
                          string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Password"])))
    throw new InvalidOperationException("Configure un certificado HTTPS confiable antes de habilitar Identity en LAN.");
builder.WebHost.UseUrls(listenUrl);
if (renderDeployment)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
    var keysPath = builder.Configuration["DataProtection:KeysPath"] ??
        (renderEnvironment ? "/var/data/keys" : null);
    if (string.IsNullOrWhiteSpace(keysPath) || !Path.IsPathRooted(keysPath))
        throw new InvalidOperationException("Configure DataProtection:KeysPath con una ruta absoluta persistente.");
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("InventarioCPR")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
if (identityEnabled)
{
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
    builder.Services.AddDbContext<AccessDbContext>(options =>
        options.UseNpgsql(NeonConnection.Runtime(builder.Configuration, builder.Environment.IsDevelopment()),
            x => x.MigrationsHistoryTable("__AuthMigrationsHistory")));
    builder.Services.AddIdentity<AccessUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 14;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    }).AddEntityFrameworkStores<AccessDbContext>().AddDefaultTokenProviders();
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/acceso";
        options.AccessDeniedPath = "/acceso-denegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
        options.ValidationInterval = TimeSpan.FromMinutes(5));
    builder.Services.AddAuthorization(InventoryPolicies.Configure);
    builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    }));
    builder.Services.AddScoped<IInventoryAccess, IdentityInventoryAccess>();
}
else builder.Services.AddScoped<IInventoryAccess, LocalTestInventoryAccess>(); // Only after explicit local-test opt-in.

builder.Services.AddDbContextFactory<InventoryDbContext>(options =>
    options.UseNpgsql(NeonConnection.Runtime(builder.Configuration, builder.Environment.IsDevelopment())));
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IPhotoStorage, CloudinaryPhotoStorage>();

var app = builder.Build();

if (renderDeployment) app.UseForwardedHeaders();

if (identityEnabled)
{
    await using var scope = app.Services.CreateAsyncScope();
    var accessDb = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
    if ((await accessDb.Database.GetPendingMigrationsAsync()).Any())
        throw new InvalidOperationException("La migración Identity está pendiente. Revísela y apruébela antes de habilitar el acceso autenticado.");
}

app.Use(async (context, next) =>
{
    var remote = context.Connection.RemoteIpAddress;
    if (remote?.IsIPv4MappedToIPv6 == true) remote = remote.MapToIPv4();
    if (!renderDeployment && (remote is null || ((!identityEnabled || localIdentity) && !IPAddress.IsLoopback(remote)) ||
        (identityEnabled && !localIdentity && !IPAddress.IsLoopback(remote) &&
            (!IsPrivateIpv4(remote) || allowedNetwork?.Contains(remote) != true))))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next(context);
});
if (identityEnabled)
{
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var users = context.RequestServices.GetRequiredService<UserManager<AccessUser>>();
            if ((await users.GetUserAsync(context.User))?.IsActive != true)
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }
        await next(context);
    });
    app.UseAuthorization();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// HTTPS is mandatory when Identity is enabled; the unauthenticated fictitious-data mode stays loopback-only.
if (identityEnabled || !app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/healthz", () => Results.NoContent()).AllowAnonymous();
var components = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
if (identityEnabled)
{
    components.RequireAuthorization(InventoryPolicies.Read);
    SecurityEndpoints.Map(app);
}

app.Run();

static bool IsPrivateIpv4(IPAddress address)
{
    if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
    var bytes = address.GetAddressBytes();
    return bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
           (bytes[0] == 192 && bytes[1] == 168);
}
