using Microsoft.Data.SqlClient;

namespace BookNest.Infrastructure.Data;

/// <summary>
/// Production must never silently fall back to a developer database. This guard
/// fails startup when the configured server is local or the connection is not
/// encrypted, so a missing environment variable cannot ship a LocalDB site.
/// </summary>
public static class DatabaseConnectionGuard
{
    private static readonly string[] LocalHostNames =
    {
        "(localdb)",
        "(local)",
        "localdb",
        "localhost",
        "127.0.0.1",
        "::1",
        "host.docker.internal",
        "."
    };

    public static void EnsureProductionConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required in Production. " +
                "Set it through an environment variable or a secret store, never appsettings.json.");
        }

        SqlConnectionStringBuilder parsed;
        try
        {
            parsed = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("The production connection string is not valid for SQL Server.", ex);
        }

        if (IsLocalHost(parsed.DataSource))
        {
            throw new InvalidOperationException(
                $"Production must not use a local database server ('{parsed.DataSource}'). " +
                "Point ConnectionStrings__DefaultConnection at the remote server.");
        }

        if (!parsed.Encrypt)
        {
            throw new InvalidOperationException("Production connections must set Encrypt=True.");
        }

        if (parsed.TrustServerCertificate)
        {
            throw new InvalidOperationException(
                "Production connections must set TrustServerCertificate=False and use a valid server certificate.");
        }
    }

    public static bool IsLocalHost(string? dataSource)
    {
        var value = dataSource?.Trim().Trim('"') ?? string.Empty;
        if (value.Length == 0)
        {
            return true;
        }

        // Strip ",port" and "\instance" before comparing the host name.
        var host = value.Split(',')[0].Split('\\')[0].Trim().ToLowerInvariant();

        return LocalHostNames.Contains(host) ||
               value.StartsWith("(local", StringComparison.OrdinalIgnoreCase) ||
               host.StartsWith("127.", StringComparison.Ordinal);
    }
}
