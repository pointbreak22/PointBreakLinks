using Application.Common;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundJobs;

// Ownership verification (Site.IsVerified/VerificationToken, ISiteVerificationService) only ever
// ran once, the moment a seller clicked "Проверить" — nothing ever checked that the token stayed
// published afterwards. A seller could verify a site, then remove the token (or lose the domain
// entirely) and stay marked verified forever. This periodically re-runs the exact same
// ISiteVerificationService.VerifyAsync check the manual button uses against every currently
// verified site, and un-verifies + notifies the seller the moment it stops matching.
public class SiteReverificationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<SiteReverificationSettings> options,
    ILogger<SiteReverificationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));
        using var timer = new PeriodicTimer(interval);

        // Run once at startup too, not just after the first interval elapses — otherwise a
        // freshly deployed instance would wait a full interval before ever checking anything.
        await RunOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var siteRepository = scope.ServiceProvider.GetRequiredService<ISiteRepository>();
        var verificationService = scope.ServiceProvider.GetRequiredService<ISiteVerificationService>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var verifiedSites = await siteRepository.GetAllVerifiedAsync(cancellationToken);
        logger.LogInformation("Site reverification pass starting for {Count} verified sites.", verifiedSites.Count);

        var revoked = 0;
        foreach (var site in verifiedSites)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            bool stillVerified;
            try
            {
                stillVerified = await verificationService.VerifyAsync(site.Url, site.VerificationToken, cancellationToken);
            }
            catch (Exception ex)
            {
                // A single unreachable site shouldn't abort the whole pass — treat it the same
                // as "token not found" for this run and try again next time.
                logger.LogWarning(ex, "Site reverification check failed for {Url}, treating as not verified this pass.", site.Url);
                stillVerified = false;
            }

            if (stillVerified)
            {
                continue;
            }

            site.IsVerified = false;
            revoked++;
            logger.LogWarning("Site {Url} (id {SiteId}) failed reverification — marking unverified.", site.Url, site.Id);

            await notificationRepository.AddAsync(
                new Notification
                {
                    UserId = site.SellerId,
                    Message = $"Подтверждение владения площадкой {site.Url} утрачено — код проверки больше не найден на странице. Подтвердите владение снова.",
                },
                cancellationToken);
        }

        if (revoked > 0)
        {
            await siteRepository.SaveChangesAsync(cancellationToken);
            await notificationRepository.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Site reverification pass complete: {Revoked} of {Count} lost verification.", revoked, verifiedSites.Count);
    }
}
