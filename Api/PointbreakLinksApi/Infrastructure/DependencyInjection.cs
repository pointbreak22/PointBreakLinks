using Application.Common;
using Domain.Repositories;
using Infrastructure.BackgroundJobs;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(3), errorCodesToAdd: null);
                }));

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IWalletRepository, EfWalletRepository>();
        services.AddScoped<ISiteRepository, EfSiteRepository>();
        services.AddScoped<IPurchasedSiteRepository, EfPurchasedSiteRepository>();
        services.AddScoped<ITopicRepository, EfTopicRepository>();
        services.AddScoped<IDynamicStatRepository, EfDynamicStatRepository>();
        services.AddScoped<IMessageRepository, EfMessageRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<IPaymentSettingRepository, EfPaymentSettingRepository>();
        services.AddScoped<IAdminDashboardRepository, EfAdminDashboardRepository>();
        services.AddScoped<IAnalyticsRepository, EfAnalyticsRepository>();
        services.AddScoped<IBalanceTransactionRepository, EfBalanceTransactionRepository>();
        services.AddScoped<ICountryRepository, EfCountryRepository>();
        services.AddScoped<IFavoriteSiteRepository, EfFavoriteSiteRepository>();
        services.AddScoped<ISiteReviewRepository, EfSiteReviewRepository>();
        services.AddScoped<IPurchasedSiteEventRepository, EfPurchasedSiteEventRepository>();
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<ISupportTicketRepository, EfSupportTicketRepository>();
        services.AddScoped<ISupportMessageRepository, EfSupportMessageRepository>();
        services.AddScoped<INotificationPreferenceRepository, EfNotificationPreferenceRepository>();
        services.AddScoped<IWithdrawalRequestRepository, EfWithdrawalRequestRepository>();
        services.AddScoped<ISavedSearchRepository, EfSavedSearchRepository>();
        services.AddScoped<IModerationAuditRepository, EfModerationAuditRepository>();
        services.AddScoped<ISavedPayoutMethodRepository, EfSavedPayoutMethodRepository>();

        services.AddScoped<IDynamicStatsRefresher, DynamicStatsRefresher>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Short timeout, no automatic redirect-following — HttpSiteVerificationService handles
        // redirects itself so it can re-run its SSRF host check on every hop.
        services.AddHttpClient(HttpSiteVerificationService.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<ISiteVerificationService, HttpSiteVerificationService>();

        services.Configure<SiteReverificationSettings>(configuration.GetSection(SiteReverificationSettings.SectionName));
        services.AddHostedService<SiteReverificationJob>();

        return services;
    }
}
