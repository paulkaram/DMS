using System.Text.RegularExpressions;
using DMS.Api.BackgroundJobs;
using DMS.Api.Middleware;
using DMS.Api.Validation;
using DMS.BL;
using DMS.DAL;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/dms-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var auditConnectionString = builder.Configuration.GetConnectionString("AuditConnection");

// Register DAL and BL layers
builder.Services.AddDataAccessLayer(connectionString, auditConnectionString);
builder.Services.AddBusinessLogicLayer();

// WORM Storage Provider (ISO 14721 OAIS compliance)
var wormConfig = builder.Configuration.GetSection("WormStorage").Get<DMS.BL.Interfaces.WormStorageConfig>()
    ?? new DMS.BL.Interfaces.WormStorageConfig();
if (wormConfig.Enabled)
{
    builder.Services.AddSingleton<DMS.BL.Interfaces.IStorageProvider>(sp =>
        new DMS.BL.Services.FilesystemWormProvider(
            wormConfig.BasePath,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<DMS.BL.Services.FilesystemWormProvider>()));
}

// Scan configuration
builder.Services.Configure<DMS.BL.DTOs.ScanOptions>(
    builder.Configuration.GetSection(DMS.BL.DTOs.ScanOptions.SectionName));

// Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var identityServerUrl = builder.Configuration["IdentityServer:Url"];
        if (!string.IsNullOrEmpty(identityServerUrl))
        {
            options.Authority = identityServerUrl;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        }
        else
        {
            // Development mode with simple JWT
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DMS",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DMS-API",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32Characters"))
            };
        }
    });

builder.Services.AddAuthorization();

// Configure CORS - Allow any origin for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<ValidationFilter>();

// HttpContextAccessor (for auto-capturing IP address and UserAgent in audit logs)
builder.Services.AddHttpContextAccessor();

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// OpenSearch (conditional - falls back to SQL search if not configured)
var openSearchConfig = builder.Configuration.GetSection(DMS.BL.Interfaces.OpenSearchConfig.SectionName);
if (openSearchConfig.Exists() && !string.IsNullOrWhiteSpace(openSearchConfig["Urls"]))
{
    builder.Services.Configure<DMS.BL.Interfaces.OpenSearchConfig>(openSearchConfig);
    builder.Services.AddHttpClient<DMS.BL.Interfaces.ISearchService, DMS.BL.Services.OpenSearchService>(client =>
    {
        var urls = openSearchConfig["Urls"] ?? "http://localhost:9200";
        client.BaseAddress = new Uri(urls.Split(',')[0].Trim());
        var username = openSearchConfig["Username"];
        var password = openSearchConfig["Password"];
        if (!string.IsNullOrEmpty(username))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }
    });
    // Override the SQL fallback registered in BL DependencyInjection
}

// Background Jobs (built-in .NET BackgroundService)
builder.Services.AddHostedService<RetentionEvaluationJob>();
builder.Services.AddHostedService<IntegrityVerificationJob>();
builder.Services.AddHostedService<StaleCheckoutCleanupJob>();
builder.Services.AddHostedService<SearchIndexingJob>();
builder.Services.AddHostedService<OcrIndexingJob>();
builder.Services.AddHostedService<PdfAConversionJob>();
builder.Services.AddHostedService<OverdueCirculationJob>();
builder.Services.AddHostedService<LegalHoldExpirationJob>();
builder.Services.AddHostedService<PermissionExpirationJob>();
builder.Services.AddHostedService<AccessReviewReminderJob>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting DMS API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Transforms PascalCase controller names to kebab-case routes.
/// e.g. PhysicalLocationsController → physical-locations
/// </summary>
public partial class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is not string s || string.IsNullOrEmpty(s)) return null;
        return KebabRegex().Replace(s, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex KebabRegex();
}
