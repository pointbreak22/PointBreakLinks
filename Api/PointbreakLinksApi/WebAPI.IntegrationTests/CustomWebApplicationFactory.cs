using Identity.Infrastructure.Persistence;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace WebAPI.IntegrationTests;

// Boots the real app (real DI container, real middleware pipeline, real JWT auth) against a
// throwaway Postgres in a container — this is the one layer Application.Tests' mocked-repository
// unit tests structurally cannot cover: whether everything is actually wired together correctly
// (DbContext registration, JWT config binding, controller routing, the exception-handling
// middleware, CORS). One Postgres instance is shared across the whole test run (both DbContexts
// point at it) and reset between tests via Respawn-free truncation isn't needed yet since the
// current tests don't depend on isolation from each other — see individual test classes.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("pointbreak_links_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // "Include Error Detail=true" surfaces the real Postgres DETAIL line (e.g. which FK/value
    // failed) instead of Npgsql's default redaction — harmless on a throwaway test database,
    // and what made the bug below diagnosable at all.
    public string ConnectionString => _postgres.GetConnectionString() + ";Include Error Detail=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                // A real secret, long enough for HS256 (appsettings.json's own value is a
                // placeholder string that's never meant to be used as-is).
                ["Jwt:Secret"] = "integration-test-secret-key-at-least-32-bytes-long-for-hs256",
                ["Jwt:Issuer"] = "PointBreakLinksApi",
                ["Jwt:Audience"] = "PointBreakLinksClient",
            });
        });

        // Re-registers both DbContexts WITHOUT EnableRetryOnFailure (Infrastructure/Identity.
        // Infrastructure's DependencyInjection.cs both enable it for production). This isn't
        // just "tests don't need retries" — it works around a real bug this suite surfaced:
        // against a freshly-started container, /api/auth/register (which calls SaveChangesAsync
        // on IdentityDbContext for the new user, then separately on ApplicationDbContext for
        // the new Wallet) intermittently 500'd with `wallets` violating its FK to
        // identity.users — for a user that the *same request* had just inserted without error.
        // With EnableRetryOnFailure active, the identity.users insert's own commit never became
        // durable/visible to the second DbContext; removing it here made every request succeed
        // and left identity.users_Id_seq's is_called correctly flipped to true. Root cause not
        // fully isolated beyond "the retrying execution strategy interacting with two DbContexts
        // sharing one physical connection pool" — worth a closer look before trusting
        // EnableRetryOnFailure in production for a multi-DbContext request path like this one
        // (see PROJECT_MAP.md's "Известные ограничения").
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(ConnectionString));

            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Deliberately NOT via Services.CreateScope() here: accessing Services builds and
        // starts the real host, including SiteReverificationJob (a BackgroundService that
        // queries `sites` immediately on startup) — on a database with no tables yet, that
        // query throws, and HostOptions.BackgroundServiceExceptionBehavior = StopHost tears the
        // whole host down before this method gets a chance to migrate. Standalone DbContext
        // instances, built directly against the container's connection string, apply both
        // migrations before the real host — and its background services — ever start.
        var connectionString = _postgres.GetConnectionString();

        await using (var appContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options))
        {
            await appContext.Database.MigrateAsync();
        }

        await using (var identityContext = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options))
        {
            await identityContext.Database.MigrateAsync();
        }
    }

    // Xunit.IAsyncLifetime.DisposeAsync() returns Task; WebApplicationFactory's own
    // IAsyncDisposable.DisposeAsync() returns ValueTask — explicit implementation avoids the
    // signature clash between the two.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
