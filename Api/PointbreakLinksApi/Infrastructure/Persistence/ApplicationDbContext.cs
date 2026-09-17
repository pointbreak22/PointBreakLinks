using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<PaymentSetting> PaymentSettings => Set<PaymentSetting>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<PurchasedSite> PurchasedSites => Set<PurchasedSite>();
    public DbSet<Link> Links => Set<Link>();
    public DbSet<DynamicStat> DynamicStats => Set<DynamicStat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<FavoriteSite> FavoriteSites => Set<FavoriteSite>();
    public DbSet<SiteReview> SiteReviews => Set<SiteReview>();
    public DbSet<PurchasedSiteEvent> PurchasedSiteEvents => Set<PurchasedSiteEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<BalanceTransaction> BalanceTransactions => Set<BalanceTransaction>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<ModerationAuditEntry> ModerationAuditEntries => Set<ModerationAuditEntry>();
    public DbSet<SavedPayoutMethod> SavedPayoutMethods => Set<SavedPayoutMethod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        TouchUpdatedAt();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchUpdatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchUpdatedAt()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
