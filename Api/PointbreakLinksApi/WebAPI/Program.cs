using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Identity.Infrastructure;
using Identity.Infrastructure.Settings;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using WebAPI.Authorization;
using WebAPI.Hubs;
using WebAPI.Middleware;
using WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Without this, enums round-trip as raw ints — PaymentSettingDto.InsuranceType has always
// been silently serializing as 0/1/2 instead of "None"/"LossProtection"/"Full", which
// RequestPublicationRequest's deserialization just surfaced as a hard 400 (the client only ever
// sends the string form).
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(o => o.LowercaseUrls = true);

// CORS — AllowCredentials is required for the httpOnly refresh-token cookie to round-trip;
// that in turn means origins can't be "*", so they're read from config instead.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:4200"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Every /api request gets a baseline per-IP limit; a handful of specifically named endpoints
// (register, forgot-password — see AuthController's [EnableRateLimiting("auth-sensitive")])
// additionally get a much tighter one, since they're open spam/enumeration vectors with no
// account of their own to lock out (unlike /login, which already has LoginCommandHandler's
// 5-attempt account lockout — see AccountLockedException).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitClientKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    // Keyed by IP + path (not just IP, like the global limiter) — register and forgot-password
    // both carry this policy but shouldn't share one bucket, or spamming one would also lock a
    // legitimate user out of the other.
    options.AddPolicy("auth-sensitive", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{RateLimitClientKey(httpContext)}:{httpContext.Request.Path}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    // Same ProblemDetails shape DomainExceptionHandler uses, so the client's existing
    // extractErrorMessage() picks up `detail` here too without any frontend-specific handling.
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Слишком много запросов. Попробуйте снова через минуту.",
            },
            cancellationToken);
    };
});

static string RateLimitClientKey(HttpContext httpContext) =>
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// /health pings both independent DbContexts (business schema `public` + Identity's `identity`,
// see PROJECT_MAP.md) — either one being unreachable means the app can't actually serve
// requests, so a deploy/orchestration readiness probe should fail on either.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Infrastructure.Persistence.ApplicationDbContext>("business-db")
    .AddDbContextCheck<Identity.Infrastructure.Persistence.IdentityDbContext>("identity-db");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Two assemblies: business Application (Sites/Projects/Wallet/...) and Identity.Application
// (Auth) — split into separate class libraries, each with its own DbContext/schema, see
// PROJECT_MAP.md.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(Application.CQRS.Sites.Commands.CreateSite.CreateSiteCommandHandler).Assembly,
        typeof(Identity.Application.CQRS.Auth.Commands.Login.LoginCommandHandler).Assembly));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            // SignalR can't send an Authorization header from the browser client, so the
            // access token travels as a query param on the hub connection URL instead —
            // same convention MessengerAzure uses for /chathub.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("RequireTwoFactor", policy => policy.Requirements.Add(new TwoFactorEnabledRequirement())));
builder.Services.AddSingleton<IAuthorizationHandler, TwoFactorEnabledHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, RequireTwoFactorResultHandler>();

builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddSignalR();
builder.Services.AddScoped<Application.Common.INotificationPusher, SignalRNotificationPusher>();

var app = builder.Build();

app.UseExceptionHandler();

// Baseline hardening headers on every response. Not a Content-Security-Policy — this API
// serves JSON to a separate Angular origin plus a handful of served files (site screenshots,
// message attachments) rather than HTML it renders itself, so there's no inline-script/style
// surface for a CSP to restrict here the way there would be on a server-rendered app.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Dev uses a self-signed localhost cert — HSTS's browser-side "always upgrade to HTTPS, even if
// the user typed http://" would stick around after the app stops running locally and break
// plain http://localhost access for anything else on the machine. Non-dev only, same reasoning
// the default ASP.NET Core template uses.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar/v1"));
app.MapHealthChecks("/health");

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
