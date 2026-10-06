using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace SaasApi.Infrastructure.Persistence;

public enum DatabaseProvider
{
    SqlServer,
    Oracle
}

public static class DatabaseProviderExtensions
{
    // SQL Server migrations live in this assembly (the historical default); Oracle
    // migrations live in their own project so each provider keeps an independent
    // model snapshot and migration history.
    public const string OracleMigrationsAssembly = "SaasApi.Migrations.Oracle";

    /// <summary>
    /// Reads <c>Database:Provider</c> ("SqlServer" | "Oracle"). Defaults to SqlServer so
    /// existing deployments keep working without a config change.
    /// </summary>
    public static DatabaseProvider GetDatabaseProvider(this IConfiguration config)
    {
        var value = config["Database:Provider"];
        if (string.IsNullOrWhiteSpace(value))
            return DatabaseProvider.SqlServer;

        return Enum.TryParse<DatabaseProvider>(value, ignoreCase: true, out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"Unsupported Database:Provider '{value}'. Expected one of: {string.Join(", ", Enum.GetNames<DatabaseProvider>())}.");
    }

    public static DbContextOptionsBuilder UseConfiguredDatabase(this DbContextOptionsBuilder opts, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        return config.GetDatabaseProvider() switch
        {
            DatabaseProvider.SqlServer => opts.UseSqlServer(connectionString),
            DatabaseProvider.Oracle => opts.UseOracle(connectionString, o => o
                .MigrationsAssembly(OracleMigrationsAssembly)
                .UseOracleSQLCompatibility(GetOracleCompatibility(config))),
            var p => throw new InvalidOperationException($"Unhandled database provider '{p}'.")
        };
    }

    // Database:OracleVersion — "19" (default, matches Autonomous DB 19c) or "21"/"23".
    // Controls which SQL features the provider emits (e.g. native BOOLEAN on 23ai).
    private static OracleSQLCompatibility GetOracleCompatibility(IConfiguration config) =>
        config["Database:OracleVersion"] switch
        {
            null or "" or "19" => OracleSQLCompatibility.DatabaseVersion19,
            "21" => OracleSQLCompatibility.DatabaseVersion21,
            "23" => OracleSQLCompatibility.DatabaseVersion23,
            var v => throw new InvalidOperationException($"Unsupported Database:OracleVersion '{v}'. Expected 19, 21 or 23.")
        };
}
