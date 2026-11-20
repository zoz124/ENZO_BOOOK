using BookNest.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace BookNest.IntegrationTests;

[Trait("Category", "Mvc")]
public class MigrationTests : IAsyncLifetime
{
    private MsSqlContainer _container = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync();
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [DockerFact]
    public async Task Migrations_ApplySuccessfully_OnEmptyDatabase()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var action = async () => await context.Database.MigrateAsync();

        // Assert
        await action.Should().NotThrowAsync("Migrations should apply without errors");

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().NotBeEmpty("At least one migration should be applied");
    }

    [DockerFact]
    public async Task Database_AllTablesCreated()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Act
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG = DB_NAME()";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        // Assert
        tables.Should().Contain(new[]
        {
            "Books", "Authors", "Categories", "Orders", "OrderItems",
            "OrderStatusHistories", "CartItems", "WishlistItems", "Addresses", "StockReservations"
        });
    }

    [DockerFact]
    public async Task Database_UniqueConstraints_AreCreated()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Act
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT i.name AS IndexName, t.name AS TableName
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.is_unique = 1 AND i.name IS NOT NULL";

        var indexes = new List<(string Table, string Index)>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            indexes.Add((reader.GetString(1), reader.GetString(0)));
        }

        // Assert
        indexes.Should().Contain(i => i.Table == "Books" && i.Index.Contains("ISBN"));
        indexes.Should().Contain(i => i.Table == "Orders" && i.Index.Contains("OrderNumber"));
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
