using BookNest.Core.Entities;
using BookNest.Core.Enums;
using BookNest.Core.Models;
using BookNest.Infrastructure.Data;
using BookNest.Infrastructure.Identity;
using BookNest.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BookStore.Tests;

/// <summary>
/// Real concurrency and data-integrity tests against a throwaway LocalDB database.
///
///   dotnet test tests/BookStore.Tests -c Release --filter "Category=LocalDb"
///
/// The store uses a retrying execution strategy in production, so transactions are
/// exercised through the same strategy here to prove they remain compatible.
/// </summary>
[Trait("Category", "LocalDb")]
public class RaceConditionTests : IAsyncLifetime
{
    private const string BaseConnection =
        @"Server=(localdb)\mssqllocaldb;Trusted_Connection=true;MultipleActiveResultSets=true";

    private readonly string _databaseName = $"BookNestRace_{Guid.NewGuid():N}";
    private string ConnectionString => $"{BaseConnection};Database={_databaseName};";

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    private ApplicationDbContext CreateContext(bool retry = false)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString, b =>
            {
                if (retry)
                {
                    b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null);
                }
            })
            .Options;

        return new ApplicationDbContext(options);
    }

    private static OrderService OrderServiceFor(ApplicationDbContext context)
        => new(context, NullLogger<OrderService>.Instance);

    private async Task<(int BookId, string UserA, string UserB)> SeedCartRaceAsync(int stock, int quantityEach)
    {
        await using var context = CreateContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var author = new Author { Name = $"Race Author {suffix}" };
        var category = new Category { Name = $"Race Category {suffix}", NameAr = "تصنيف" };
        context.Authors.Add(author);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var book = new Book
        {
            Title = $"Last Copy {suffix}",
            ISBN = $"9{Random.Shared.Next(100000000, 999999999)}",
            Price = 100m,
            Stock = stock,
            AuthorId = author.Id,
            CategoryId = category.Id
        };
        context.Books.Add(book);

        var userA = NewUser($"race-a-{suffix}@example.test");
        var userB = NewUser($"race-b-{suffix}@example.test");
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        context.CartItems.AddRange(
            new CartItem { UserId = userA.Id, BookId = book.Id, Quantity = quantityEach },
            new CartItem { UserId = userB.Id, BookId = book.Id, Quantity = quantityEach });
        await context.SaveChangesAsync();

        return (book.Id, userA.Id, userB.Id);
    }

    private static ApplicationUser NewUser(string email) => new()
    {
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = true,
        SecurityStamp = Guid.NewGuid().ToString("N"),
        IsActive = true
    };

    private static CheckoutRequest Checkout(string key) => new()
    {
        FullName = "Race Tester",
        Phone = "01000000000",
        City = "Cairo",
        AddressLine = "1 Test Street",
        PaymentMethod = "Cash",
        IdempotencyKey = key
    };

    [LocalDbFact]
    public async Task Oversell_TwoUsersBuyTheLastCopy_OnlyOneOrderWins()
    {
        var (bookId, userA, userB) = await SeedCartRaceAsync(stock: 1, quantityEach: 1);

        await using var contextA = CreateContext();
        await using var contextB = CreateContext();

        var results = await Task.WhenAll(
            OrderServiceFor(contextA).PlaceOrderAsync(userA, Checkout(Guid.NewGuid().ToString("N"))),
            OrderServiceFor(contextB).PlaceOrderAsync(userB, Checkout(Guid.NewGuid().ToString("N"))));

        results.Count(r => r.Succeeded).Should().Be(1, "the last copy can only be sold once");

        await using var verify = CreateContext();
        (await verify.Books.IgnoreQueryFilters().Where(b => b.Id == bookId).Select(b => b.Stock).SingleAsync())
            .Should().Be(0);
        (await verify.Orders.CountAsync()).Should().Be(1);
    }

    [LocalDbFact]
    public async Task Oversell_TwentySimultaneousBuyers_OneCopy_OnlyOneSucceeds()
    {
        var (bookId, _, _) = await SeedCartRaceAsync(stock: 1, quantityEach: 1);

        var userIds = new List<string>();
        await using (var seed = CreateContext())
        {
            for (var i = 0; i < 20; i++)
            {
                var user = NewUser($"race-{i}-{Guid.NewGuid():N}@example.test");
                seed.Users.Add(user);
                userIds.Add(user.Id);
                seed.CartItems.Add(new CartItem { UserId = user.Id, BookId = bookId, Quantity = 1 });
            }

            await seed.SaveChangesAsync();
        }

        var results = await Task.WhenAll(userIds.Select(async id =>
        {
            await using var context = CreateContext();
            return await OrderServiceFor(context).PlaceOrderAsync(id, Checkout(Guid.NewGuid().ToString("N")));
        }));

        results.Count(r => r.Succeeded).Should().Be(1);
        results.Count(r => !r.Succeeded).Should().Be(19);

        await using var verify = CreateContext();
        (await verify.Books.IgnoreQueryFilters().Where(b => b.Id == bookId).Select(b => b.Stock).SingleAsync())
            .Should().Be(0, "stock must never go negative");
        (await verify.Orders.CountAsync()).Should().Be(1);
    }

    [LocalDbFact]
    public async Task DoubleCheckout_SameIdempotencyKey_TenSubmits_CreateOneOrder()
    {
        var (bookId, userA, _) = await SeedCartRaceAsync(stock: 10, quantityEach: 1);
        var key = Guid.NewGuid().ToString("N");

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = CreateContext();
            return await OrderServiceFor(context).PlaceOrderAsync(userA, Checkout(key));
        }));

        await using var verify = CreateContext();
        (await verify.Orders.CountAsync()).Should().Be(1, "the idempotency key must allow exactly one order");
        (await verify.Books.IgnoreQueryFilters().Where(b => b.Id == bookId).Select(b => b.Stock).SingleAsync())
            .Should().Be(9, "stock is decremented once, not once per submit");
        results.Should().OnlyContain(r => r.Succeeded, "every duplicate submit should resolve to the same order");
        results.Select(r => r.OrderId).Distinct().Should().ContainSingle();
    }

    [LocalDbFact]
    public async Task Review_ConcurrentDuplicate_OnlyOneRowAndOneAccepted()
    {
        var (bookId, userA, _) = await SeedCartRaceAsync(stock: 5, quantityEach: 1);
        var catalog = new StubCatalogService();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
        {
            await using var context = CreateContext();
            var service = new ReviewService(context, catalog, NullLogger<ReviewService>.Instance);
            return await service.AddAsync(bookId, userA, (i % 5) + 1, "race comment");
        }));

        results.Count(r => r.Succeeded).Should().Be(1);
        results.Count(r => !r.Succeeded).Should().Be(7);

        await using var verify = CreateContext();
        (await verify.BookReviews.CountAsync(r => r.BookId == bookId && r.UserId == userA)).Should().Be(1);
    }

    [LocalDbFact]
    public async Task Registration_SameEmailConcurrently_UniqueIndexRejectsTheLoser()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.test";

        var attempts = await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            await using var context = CreateContext();
            context.Users.Add(NewUser(email));
            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }));

        attempts.Count(ok => ok).Should().Be(1, "the database must enforce one account per email");

        await using var verify = CreateContext();
        (await verify.Users.CountAsync(u => u.NormalizedEmail == email.ToUpperInvariant())).Should().Be(1);
    }

    [LocalDbFact]
    public async Task Database_RejectsNegativeStockPriceQuantityAndOutOfRangeRating()
    {
        var (bookId, userA, _) = await SeedCartRaceAsync(stock: 3, quantityEach: 1);

        await using var context = CreateContext();

        Func<Task> negativeStock = async () => await context.Database.ExecuteSqlRawAsync(
            "UPDATE Books SET Stock = -1 WHERE Id = {0}", bookId);
        await negativeStock.Should().ThrowAsync<Exception>("CK_Books_Stock_NonNegative");

        Func<Task> negativePrice = async () => await context.Database.ExecuteSqlRawAsync(
            "UPDATE Books SET Price = -0.01 WHERE Id = {0}", bookId);
        await negativePrice.Should().ThrowAsync<Exception>("CK_Books_Price_NonNegative");

        Func<Task> zeroQuantity = async () => await context.Database.ExecuteSqlRawAsync(
            "UPDATE CartItems SET Quantity = 0 WHERE BookId = {0}", bookId);
        await zeroQuantity.Should().ThrowAsync<Exception>("CK_CartItems_Quantity_Positive");

        context.BookReviews.Add(new BookReview
        {
            BookId = bookId,
            UserId = userA,
            Rating = 0,
            ReviewerName = "Invalid"
        });
        Func<Task> invalidRating = async () => { await context.SaveChangesAsync(); };
        await invalidRating.Should().ThrowAsync<Exception>("CK_BookReviews_Rating_Range");
    }

    [LocalDbFact]
    public async Task Cancel_RestoresStockExactlyOnce_EvenWhenSubmittedTwice()
    {
        var (bookId, userA, _) = await SeedCartRaceAsync(stock: 2, quantityEach: 1);

        int orderId;
        await using (var context = CreateContext())
        {
            var result = await OrderServiceFor(context).PlaceOrderAsync(userA, Checkout(Guid.NewGuid().ToString("N")));
            result.Succeeded.Should().BeTrue();
            orderId = result.OrderId!.Value;
        }

        var cancels = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var context = CreateContext();
            return await OrderServiceFor(context).CancelOrderAsync(orderId, userA, "changed my mind");
        }));

        cancels.Count(r => r.Succeeded).Should().Be(1, "a cancelled order cannot be cancelled again");

        await using var verify = CreateContext();
        (await verify.Books.IgnoreQueryFilters().Where(b => b.Id == bookId).Select(b => b.Stock).SingleAsync())
            .Should().Be(2, "stock is restored once, not once per click");
        (await verify.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync())
            .Should().Be(OrderStatus.Cancelled);
    }

    [LocalDbFact]
    public async Task PlaceOrder_WorksWithRetryingExecutionStrategy()
    {
        var (_, userA, _) = await SeedCartRaceAsync(stock: 2, quantityEach: 1);

        // This is the configuration the web app uses (EnableRetryOnFailure). Without
        // wrapping the transaction in the execution strategy EF throws before any SQL.
        await using var context = CreateContext(retry: true);

        var result = await OrderServiceFor(context)
            .PlaceOrderAsync(userA, Checkout(Guid.NewGuid().ToString("N")));

        result.Succeeded.Should().BeTrue(result.Error);
    }
}
