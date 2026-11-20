using BookNest.Core.Entities;
using BookNest.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace BookNest.IntegrationTests;

[Trait("Category", "Race")]
public class DatabaseConcurrencyTests : IAsyncLifetime
{
    private MsSqlContainer _container = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Apply migrations
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    [DockerFact]
    public async Task Books_DuplicateISBN_SimultaneousInsert_OnlyOneSucceeds()
    {
        // Arrange
        const string isbn = "9781234567890";

        int authorId;
        int categoryId;

        await using (var context = CreateContext())
        {
            var author = new Author { Name = "Test Author" };
            var category = new Category { Name = "Test Category", NameAr = "تصنيف تجريبي" };
            context.Authors.Add(author);
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            authorId = author.Id;
            categoryId = category.Id;
        }

        // Act - خمس محاولات إدخال بنفس الـ ISBN في نفس اللحظة
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            try
            {
                await using var ctx = CreateContext();

                ctx.Books.Add(new Book
                {
                    Title = $"Book {i}",
                    ISBN = isbn,
                    Price = 100,
                    Stock = 10,
                    AuthorId = authorId,
                    CategoryId = categoryId
                });

                await ctx.SaveChangesAsync();
                return (Success: true, Error: (string?)null);
            }
            catch (DbUpdateException ex)
            {
                return (Success: false, Error: ex.InnerException?.Message ?? ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Count(r => r.Success).Should().Be(1,
            "Only one insert should succeed with duplicate ISBN");

        await using var verifyContext = CreateContext();
        var booksWithIsbn = await verifyContext.Books.CountAsync(b => b.ISBN == isbn);
        booksWithIsbn.Should().Be(1, "Database should contain exactly one book with this ISBN");
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
