using BookNest.Infrastructure.Data;
using FluentAssertions;
using Xunit;

namespace BookStore.Tests;

public class ProductionConnectionGuardTests
{
    [Theory]
    [InlineData("Server=(localdb)\\mssqllocaldb;Database=BookNestDb;Encrypt=True;TrustServerCertificate=False")]
    [InlineData("Server=localhost;Database=BookNestDb;Encrypt=True;TrustServerCertificate=False")]
    [InlineData("Server=127.0.0.1,1433;Database=BookNestDb;Encrypt=True;TrustServerCertificate=False")]
    [InlineData("Server=.;Database=BookNestDb;Encrypt=True;TrustServerCertificate=False")]
    [InlineData("Server=host.docker.internal;Database=BookNestDb;Encrypt=True;TrustServerCertificate=False")]
    [InlineData("Server=(local);Database=BookNestDb;Encrypt=True;TrustServerCertificate=False")]
    public void Production_RejectsLocalServers(string connectionString)
    {
        var act = () => DatabaseConnectionGuard.EnsureProductionConnection(connectionString);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*local database server*");
    }

    [Fact]
    public void Production_RejectsMissingConnectionString()
    {
        var act = () => DatabaseConnectionGuard.EnsureProductionConnection(null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*required in Production*");
    }

    [Fact]
    public void Production_RejectsUnencryptedConnection()
    {
        var act = () => DatabaseConnectionGuard.EnsureProductionConnection(
            "Server=tcp:db.example.com,1433;Database=enzo;User Id=enzo_app;Password=not-a-real-password;Encrypt=False");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encrypt=True*");
    }

    [Fact]
    public void Production_RejectsTrustedServerCertificate()
    {
        var act = () => DatabaseConnectionGuard.EnsureProductionConnection(
            "Server=tcp:db.example.com,1433;Database=enzo;User Id=enzo_app;Password=not-a-real-password;Encrypt=True;TrustServerCertificate=True");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TrustServerCertificate=False*");
    }

    [Fact]
    public void Production_AcceptsEncryptedRemoteServer()
    {
        var act = () => DatabaseConnectionGuard.EnsureProductionConnection(
            "Server=tcp:db.example.com,1433;Database=enzo;User Id=enzo_app;Password=not-a-real-password;Encrypt=True;TrustServerCertificate=False");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("db.example.com", false)]
    [InlineData("db.internal,1433", false)]
    [InlineData("localhost", true)]
    [InlineData("(localdb)\\mssqllocaldb", true)]
    [InlineData("127.0.0.1,1433", true)]
    public void IsLocalHost_ClassifiesDataSources(string dataSource, bool expected)
    {
        DatabaseConnectionGuard.IsLocalHost(dataSource).Should().Be(expected);
    }
}
