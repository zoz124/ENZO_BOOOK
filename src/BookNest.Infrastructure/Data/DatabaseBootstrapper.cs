using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookNest.Infrastructure.Data;

/// <summary>
/// Runs migrations + seeding exactly once across concurrently starting instances.
///
/// EF Core's Migrate() has no cross-process lock of its own, so two instances booting
/// together can race on CREATE TABLE. A session-scoped application lock held on the
/// master database wraps migrations, and the seeder takes its own lock inside its
/// transaction. Both locks are released when the bootstrapper finishes.
/// </summary>
public sealed class DatabaseBootstrapper
{
    internal const string BootstrapLockResource = "BookNest:Bootstrap";
    private const int LockTimeoutMs = 120_000;

    private readonly ApplicationDbContext _context;
    private readonly DatabaseSeeder _seeder;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(
        ApplicationDbContext context,
        DatabaseSeeder seeder,
        ILogger<DatabaseBootstrapper> logger)
    {
        _context = context;
        _seeder = seeder;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

        await using var lockConnection = new SqlConnection(masterBuilder.ConnectionString);
        await lockConnection.OpenAsync(cancellationToken);

        try
        {
            await AcquireLockAsync(lockConnection, cancellationToken);

            _logger.LogInformation("Applying migrations…");
            await _context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Seeding reference data…");
            await _seeder.SeedAsync(cancellationToken);
            await _seeder.SeedDemoAccountsAsync(cancellationToken);
            await _seeder.BackfillSearchTextAsync(cancellationToken);
            await _seeder.BackfillCoversAsync(cancellationToken);
        }
        finally
        {
            await ReleaseLockAsync(lockConnection);
        }
    }

    private async Task AcquireLockAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = @timeout;
            SELECT @result;
            """;
        command.Parameters.AddWithValue("@resource", BootstrapLockResource);
        command.Parameters.AddWithValue("@timeout", LockTimeoutMs);

        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? -999);

        if (status < 0)
        {
            throw new InvalidOperationException(
                $"Bootstrap aborted: could not acquire '{BootstrapLockResource}' (sp_getapplock returned {status}).");
        }

        _logger.LogDebug("Bootstrap lock acquired (status {Status}).", status);
    }

    private static async Task ReleaseLockAsync(SqlConnection connection)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';";
            command.Parameters.AddWithValue("@resource", BootstrapLockResource);
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Releasing the lock is best-effort; closing the connection also releases it.
        }
    }
}
