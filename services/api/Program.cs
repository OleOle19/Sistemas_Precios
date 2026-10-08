using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Endpoints;
using SistemasPrecios.Api.Services;

var builder = WebApplication.CreateBuilder(args);
LocalSecrets.Load(builder.Configuration, builder.Environment.ContentRootPath);
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var usePostgres = string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase);
var useSqlServer = string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
if (!usePostgres && !useSqlServer && !string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Usa Database:Provider Sqlite, SqlServer o Postgres.");
if (!usePostgres && !useSqlServer)
{
    var sqlite = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (sqlite.DataSource != ":memory:") sqlite.DataSource = Path.GetFullPath(sqlite.DataSource, builder.Environment.ContentRootPath);
    builder.Configuration["ConnectionStrings:DefaultConnection"] = sqlite.ToString();
}
EnsureLocalSqliteDirectory(builder.Configuration, usePostgres || useSqlServer);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.PostConfigure<StorageOptions>(o => o.RootPath = Path.GetFullPath(o.RootPath, builder.Environment.ContentRootPath));
builder.Services.Configure<MatchingOptions>(builder.Configuration.GetSection(MatchingOptions.SectionName));

builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "sistemas-precios-session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/auth/login";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (usePostgres)
    {
        options.UseNpgsql(connectionString);
    }
    else if (useSqlServer)
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

builder.Services.AddScoped<IDocumentJobScheduler, DocumentJobScheduler>();
builder.Services.AddHostedService<DocumentWorker>();
var extractionProvider = PriceExtractionSettings.Provider(builder.Configuration);
if (extractionProvider == "Gemini")
    builder.Services.AddHttpClient<IPriceExtractor, GeminiPriceExtractor>(http => http.Timeout = TimeSpan.FromSeconds(120));
else
    builder.Services.AddHttpClient<IPriceExtractor, OpenAiPriceExtractor>(http => http.Timeout = TimeSpan.FromSeconds(120));
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 11 * 1024 * 1024);
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IProductMatchingService, ProductMatchingService>();
builder.Services.AddScoped<IComparisonQueryService, ComparisonQueryService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("web");
app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) && context.Request.Headers.TryGetValue("Origin", out var origin))
    {
        var allowed = builder.Configuration.GetSection("Web:Origins").Get<string[]>() ?? ["http://localhost:3000", "http://127.0.0.1:3000"];
        if (!allowed.Contains(origin.ToString())) { context.Response.StatusCode = 403; return; }
    }
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/healthz", () => Results.Ok(new
{
    status = "ok",
    service = "SistemasPrecios.Api"
}));

app.MapApiRoutes();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await EnsureApplicationSchemaAsync(db);
    }
    try { await db.Documents.Select(d => d.SourceKind).Take(1).ToListAsync(); }
    catch (Exception) { throw new InvalidOperationException("La base de datos pertenece al prototipo anterior. Conserva una copia y usa una base nueva para v2; no se modifica ni borra automáticamente."); }
    await SeedDataService.InitializeAsync(scope.ServiceProvider);
}

app.Run();

static void EnsureLocalSqliteDirectory(ConfigurationManager configuration, bool usePostgres)
{
    if (usePostgres)
    {
        return;
    }

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource))
    {
        return;
    }

    var fullDatabasePath = Path.GetFullPath(sqliteBuilder.DataSource, Directory.GetCurrentDirectory());
    var directory = Path.GetDirectoryName(fullDatabasePath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

static async Task EnsureApplicationSchemaAsync(ApplicationDbContext db)
{
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.\"Users\"') IS NOT NULL;";
        var result = await command.ExecuteScalarAsync();
        var usersTableExists = result is bool flag && flag;

        if (!usersTableExists)
        {
            var creator = db.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
    }
    finally
    {
        await connection.CloseAsync();
    }
}

public partial class Program;
