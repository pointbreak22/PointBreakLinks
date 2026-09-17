using Identity.Application.Common;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Identity.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Defaults to the same physical database as "DefaultConnection" (same Postgres server,
        // different schema — see PROJECT_MAP.md) — kept as its own config key rather than
        // reusing "DefaultConnection" directly so pointing Identity at a genuinely separate
        // database later is a one-line appsettings change, not a code change.
        var connectionString = configuration.GetConnectionString("IdentityConnection")
                                ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                    // Own migration-history table, in the same "identity" schema its tables live
                    // in — keeps it from colliding with the business context's
                    // "__EFMigrationsHistory" (default schema "public"), since both contexts
                    // point at the same physical database.
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(3), errorCodesToAdd: null);
                }));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, EfPasswordResetTokenRepository>();
        services.AddScoped<IEmailChangeTokenRepository, EfEmailChangeTokenRepository>();
        services.AddScoped<ITwoFactorLoginTicketRepository, EfTwoFactorLoginTicketRepository>();
        services.AddScoped<ITwoFactorBackupCodeRepository, EfTwoFactorBackupCodeRepository>();
        services.AddScoped<ITrustedDeviceRepository, EfTrustedDeviceRepository>();
        services.AddScoped<ILoginHistoryRepository, EfLoginHistoryRepository>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<ITotpService, TotpService>();

        services.AddScoped<TokenIssuer>();
        services.AddScoped<NewDeviceLoginNotifier>();

        return services;
    }
}
