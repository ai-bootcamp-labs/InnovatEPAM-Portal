using System.Text;
using FluentValidation;
using InnovatEpam.Portal.Api.Auth;
using InnovatEpam.Portal.Api.Conventions;
using InnovatEpam.Portal.Api.Cors;
using InnovatEpam.Portal.Api.ErrorHandling;
using InnovatEpam.Portal.Api.Logging;
using InnovatEpam.Portal.Application.Attachments;
using InnovatEpam.Portal.Application.Auth;
using InnovatEpam.Portal.Application.Decisions;
using InnovatEpam.Portal.Application.Ideas;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Application.Scoring;
using InnovatEpam.Portal.Application.Storage;
using InnovatEpam.Portal.Domain.Identity;
using InnovatEpam.Portal.Infrastructure.Persistence;
using InnovatEpam.Portal.Infrastructure.Persistence.Interceptors;
using InnovatEpam.Portal.Infrastructure.Seeding;
using InnovatEpam.Portal.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ─── Logging (T027) ──────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Destructure.With<SensitivePropertyScrubbingPolicy>()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "InnovatEpam.Portal.Api")
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter(renderMessage: true));
});

// ─── EF Core / PostgreSQL (T021, T022) ───────────────────────────────────────
builder.Services.AddSingleton<AuditFieldsInterceptor>();
builder.Services.AddDbContext<PortalDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");
    options
        .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", PortalDbContext.SchemaName))
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(sp.GetRequiredService<AuditFieldsInterceptor>());
});

// ─── ASP.NET Core Identity (T023) ────────────────────────────────────────────
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = false; // FR-006/7 — Phase 1 leaves lockout off.
    })
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<PortalDbContext>()
    .AddDefaultTokenProviders();

// ─── JWT bearer authentication (T024) ────────────────────────────────────────
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// ─── Authorization (T025) ────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole(AppRole.Admin));
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ─── ProblemDetails + global exception handler (T026) ────────────────────────
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// ─── Validation ──────────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(typeof(IAttachmentStorage).Assembly);

// ─── Attachment storage (T031, T032) ─────────────────────────────────────────
builder.Services
    .AddOptions<AttachmentStorageOptions>()
    .Bind(builder.Configuration.GetSection("Attachments"));
builder.Services.AddSingleton<IAttachmentStorage, FileSystemAttachmentStorage>();

// ─── Application services (T062-T064) ────────────────────────────────────────
builder.Services.AddScoped<IPortalDbContext>(sp => sp.GetRequiredService<PortalDbContext>());
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<IAliasService, AliasService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IdeaService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<DecisionService>();
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<InnovatEpam.Portal.Application.Notifications.NotificationService>();

// ─── Health checks (T108) ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionStringFactory: sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured."),
        name: "postgres",
        tags: new[] { "db", "ready" });

// ─── CORS (T029) ─────────────────────────────────────────────────────────────
builder.Services
    .AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName));
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
builder.Services.AddCors(o => o.AddPolicy(CorsOptions.DefaultPolicyName, policy =>
{
    if (corsOptions.AllowedOrigins.Length > 0)
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    }
}));

// ─── MVC + route convention (T030) ───────────────────────────────────────────
builder.Services.AddControllers(o => o.Conventions.Add(new ApiVersionRouteConvention()))
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// ─── Body / form size limits (FR-010) ────────────────────────────────────────
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 10 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
// ─── HSTS (T030a) ─────────────────────────────────────────────────────────────────────
builder.Services.AddHsts(options =>
{
    var maxAgeDays = builder.Configuration.GetValue<int?>("Hsts:MaxAgeDays") ?? 365;
    options.MaxAge = TimeSpan.FromDays(maxAgeDays);
    options.IncludeSubDomains = builder.Configuration.GetValue<bool?>("Hsts:IncludeSubDomains") ?? true;
    options.Preload = builder.Configuration.GetValue<bool?>("Hsts:Preload") ?? false;
});
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = builder.Configuration.GetValue<int?>("HttpsRedirection:RedirectStatusCode") ?? 308;
    var httpsPort = builder.Configuration.GetValue<int?>("HttpsRedirection:HttpsPort");
    if (httpsPort.HasValue)
    {
        options.HttpsPort = httpsPort.Value;
    }
});
// ─── Swashbuckle (T028) ──────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InnovatEPAM Portal API",
        Version = "v1",
        Description = "Phase 1 MVP — innovation idea capture, review, and decision.",
    });

    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
    foreach (var xmlPath in xmlFiles)
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT bearer token. Format: 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme },
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ─── Pipeline ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
        var userId = httpContext.User?.FindFirst("sub")?.Value
            ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId)) diagnosticContext.Set("UserId", userId);
        diagnosticContext.Set("Endpoint", httpContext.GetEndpoint()?.DisplayName);
    };
});

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts(); // T030a — production transport hardening.
}

app.UseHttpsRedirection();
app.UseCors(CorsOptions.DefaultPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }))
   .AllowAnonymous()
   .WithName("Health");

app.MapHealthChecks("/api/v1/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).AllowAnonymous();

app.MapHealthChecks("/api/v1/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

app.MapControllers();

// ─── Admin seeding (T107) ───────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    // Apply any pending EF Core migrations before seeding. In container-based
    // dev (docker compose up) the host no longer runs `dotnet ef database update`
    // manually, so the seeder would otherwise hit tables that don't exist yet
    // and crash the process before Kestrel binds — surfacing as
    // ERR_CONNECTION_REFUSED on the frontend.
    var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
    await db.Database.MigrateAsync();

    await AdminUserSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

/// <summary>Test discoverability for <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;

// trigger ci pipeline
