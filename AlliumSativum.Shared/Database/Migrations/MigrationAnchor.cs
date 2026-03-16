using AlliumSativum.Shared.Database;
using DbUp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Shared.Migrations;

// Anchor to reference in startUp
public sealed class MigrationAnchor
{
}

public static class MigrationExtensions
{
    public static void AddCatalogDatabase(this WebApplicationBuilder webApplicationBuilder, string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);
        var upgradeEngine = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationExtensions).Assembly)
            .LogToConsole()
            .Build();

        var result = upgradeEngine.PerformUpgrade();

        if (!result.Successful) throw new ArgumentException($"Could not run postgreSQL database {connectionString}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success! Database is up to date.");
        Console.ResetColor();

        webApplicationBuilder.Services.AddSingleton(new CatalogDatabaseSettings
        {
            ConnectionString = connectionString
        });
        webApplicationBuilder.Services.AddScoped<CatalogDatabase>();
    }
}