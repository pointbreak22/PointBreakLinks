using Identity.Domain.Common;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

// Same physical Postgres database as the business ApplicationDbContext, different schema
// ("identity") — see PROJECT_MAP.md for why a schema split was chosen over a truly separate
// database: real FK constraints from business tables (projects.UserId, sites.SellerId, ...) to
// identity.users still work, since Postgres FKs survive `ALTER TABLE ... SET SCHEMA` and are
// enforced across schemas within one database. Its own independent migration history lives in
// identity.__EFMigrationsHistory (see AddIdentityInfrastructure) so it never collides with the
// business context's history table.
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailChangeToken> EmailChangeTokens => Set<EmailChangeToken>();
    public DbSet<TwoFactorLoginTicket> TwoFactorLoginTickets => Set<TwoFactorLoginTicket>();
    public DbSet<TwoFactorBackupCode> TwoFactorBackupCodes => Set<TwoFactorBackupCode>();
    public DbSet<TrustedDevice> TrustedDevices => Set<TrustedDevice>();
    public DbSet<LoginHistoryEntry> LoginHistory => Set<LoginHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
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
