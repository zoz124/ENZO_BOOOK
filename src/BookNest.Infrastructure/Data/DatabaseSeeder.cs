using System.Data;
using BookNest.Core.Entities;
using BookNest.Infrastructure.Data.Seed;
using BookNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookNest.Infrastructure.Data;

/// <summary>
/// Idempotent demo-data seeder.
///
/// Race safety (T-02): the existence check and the insert are not atomic on their
/// own, so two app instances starting at the same time could both observe an empty
/// database and both insert. The whole seed therefore runs inside one transaction
/// that first takes a SQL Server application lock (sp_getapplock). The second caller
/// blocks until the first commits, then re-reads and skips.
/// </summary>
public class DatabaseSeeder
{
    public const string AdminRole = "Admin";
    public const string CustomerRole = "Customer";
    internal const string SeedLockResource = "BookNest:DatabaseSeeder";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            await AcquireSeedLockAsync(cancellationToken);

            await SeedRolesAsync();
            await SeedAdminAsync();
            await SeedCategoriesAsync(cancellationToken);
            await SeedAuthorsAsync(cancellationToken);
            await SeedBooksAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task AcquireSeedLockAsync(CancellationToken cancellationToken)
    {
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = SeedLockResource };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', " +
            "@LockOwner = 'Transaction', @LockTimeout = 120000;",
            new object[] { result, resource },
            cancellationToken);

        var status = result.Value as int? ?? -999;

        if (status < 0)
        {
            throw new InvalidOperationException(
                $"Seed aborted: could not acquire '{SeedLockResource}' (sp_getapplock returned {status}).");
        }

        _logger.LogDebug("Seed lock '{Resource}' acquired (status {Status}).", SeedLockResource, status);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in new[] { AdminRole, CustomerRole })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    _logger.LogError("Could not create role {Role}: {Errors}", roleName,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedAdminAsync()
    {
        var email = _configuration["Seed:AdminEmail"];
        // Never hard-code the admin password: it comes from configuration / environment
        // (Seed__AdminPassword) and the seeder skips the admin when it is missing.
        var password = _configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "Skipping admin seed: set Seed:AdminEmail and Seed__AdminPassword to create the default admin.");
            return;
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await _userManager.IsInRoleAsync(existing, AdminRole))
            {
                await _userManager.AddToRoleAsync(existing, AdminRole);
            }

            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "BookNest",
            LastName = "Admin",
            IsActive = true
        };

        var created = await _userManager.CreateAsync(admin, password);
        if (!created.Succeeded)
        {
            _logger.LogError("Could not create the default admin: {Errors}",
                string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(admin, AdminRole);
        _logger.LogInformation("Seeded default admin {Email} with role {Role}", email, AdminRole);
    }

    private async Task SeedDemoCustomerAsync()
    {
        var email = _configuration["Seed:CustomerEmail"];
        var password = _configuration["Seed:CustomerPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var customer = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Demo",
            LastName = "Customer",
            IsActive = true
        };

        var created = await _userManager.CreateAsync(customer, password);
        if (created.Succeeded)
        {
            await _userManager.AddToRoleAsync(customer, CustomerRole);
            _logger.LogInformation("Seeded demo customer {Email}", email);
        }
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = SeedData.Categories.Select(c => new Category
        {
            Name = c.Name,
            NameAr = c.NameAr,
            Slug = c.Slug,
            Description = c.Description
        });

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAuthorsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Authors.AnyAsync(cancellationToken))
        {
            return;
        }

        var authors = SeedData.Authors.Select(a => new Author
        {
            Name = a.Name,
            Biography = a.Biography
        });

        _context.Authors.AddRange(authors);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedBooksAsync(CancellationToken cancellationToken)
    {
        if (await _context.Books.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = await _context.Categories.ToDictionaryAsync(c => c.Slug!, c => c.Id, cancellationToken);
        var authors = await _context.Authors.ToDictionaryAsync(a => a.Name, a => a.Id, cancellationToken);

        var books = new List<Book>(SeedData.Books.Count);

        for (var index = 0; index < SeedData.Books.Count; index++)
        {
            var seed = SeedData.Books[index];

            if (!authors.TryGetValue(seed.AuthorName, out var authorId))
            {
                _logger.LogWarning("Seed book {Title} skipped: unknown author {Author}", seed.Title, seed.AuthorName);
                continue;
            }

            if (!categories.TryGetValue(seed.CategorySlug, out var categoryId))
            {
                _logger.LogWarning("Seed book {Title} skipped: unknown category {Category}", seed.Title, seed.CategorySlug);
                continue;
            }

            books.Add(new Book
            {
                Title = seed.Title,
                ISBN = seed.Isbn,
                Price = seed.Price,
                DiscountPrice = seed.DiscountPrice,
                Stock = seed.Stock,
                Pages = seed.Pages,
                Publisher = seed.Publisher,
                Language = seed.Language,
                PublishedDate = new DateTime(seed.PublishedYear, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                AuthorId = authorId,
                CategoryId = categoryId,
                SearchText = SeedData.SearchTextFor(seed),
                CoverImageUrl = SeedData.CoverFor(index),
                Description = $"{seed.Title} — {seed.Publisher}."
            });
        }

        _context.Books.AddRange(books);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} books", books.Count);
    }

    /// <summary>Optional demo customer, seeded after the catalogue exists.</summary>
    public async Task SeedDemoAccountsAsync(CancellationToken cancellationToken = default)
        => await SeedDemoCustomerAsync();

    /// <summary>
    /// Backfills the normalized search column for rows created before T-67.
    /// Arabic normalization cannot be expressed in T-SQL, so it runs through the domain helper.
    /// </summary>
    public async Task BackfillSearchTextAsync(CancellationToken cancellationToken = default)
    {
        var books = await _context.Books
            .IgnoreQueryFilters()
            .Include(b => b.Author)
            .Where(b => b.SearchText == "")
            .ToListAsync(cancellationToken);

        if (books.Count == 0)
        {
            return;
        }

        foreach (var book in books)
        {
            book.SearchText = Core.Utils.ArabicText.BuildSearchText(book.Title, book.Author.Name, book.ISBN);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Backfilled search text for {Count} books", books.Count);
    }

    /// <summary>
    /// Adds the editorial cover photography to rows seeded before covers existed.
    /// The URL is derived from the seed row, so it stays stable across restarts.
    /// </summary>
    public async Task BackfillCoversAsync(CancellationToken cancellationToken = default)
    {
        var covers = SeedData.Books
            .Select((book, index) => new { book.Isbn, Url = SeedData.CoverFor(index) })
            .ToDictionary(x => x.Isbn, x => x.Url);

        var books = await _context.Books
            .IgnoreQueryFilters()
            .Where(b => b.CoverImageUrl == null || b.CoverImageUrl == "")
            .ToListAsync(cancellationToken);

        var updated = 0;

        foreach (var book in books)
        {
            if (covers.TryGetValue(book.ISBN, out var url))
            {
                book.CoverImageUrl = url;
                updated++;
            }
        }

        if (updated == 0)
        {
            return;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Backfilled covers for {Count} books", updated);
    }
}
