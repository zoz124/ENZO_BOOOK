using BookNest.Infrastructure.Data.Seed;
using FluentAssertions;
using Xunit;

namespace BookNest.UnitTests;

[Trait("Category", "Mvc")]
public class SeedCatalogueTests
{
    [Fact]
    public void Catalogue_HasMinimumDemoData()
    {
        SeedData.Categories.Should().HaveCount(8);
        SeedData.Authors.Should().HaveCount(20);
        SeedData.Books.Should().HaveCountGreaterThanOrEqualTo(60);
    }

    [Fact]
    public void Books_IsbnIsThirteenCharacters_AndUnique()
    {
        SeedData.Books.Should().OnlyContain(b => b.Isbn.Length == 13,
            "ISBN-13 is the only length the Book entity allows");

        SeedData.Books.Select(b => b.Isbn).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Books_DiscountPrice_IsLowerThanPrice()
    {
        // BR-03
        SeedData.Books
            .Where(b => b.DiscountPrice.HasValue)
            .Should().OnlyContain(b => b.DiscountPrice!.Value < b.Price);
    }

    [Fact]
    public void Books_ReferenceKnownAuthorsAndCategories()
    {
        var authorNames = SeedData.Authors.Select(a => a.Name).ToHashSet();
        var categorySlugs = SeedData.Categories.Select(c => c.Slug).ToHashSet();

        SeedData.Books.Should().OnlyContain(b => authorNames.Contains(b.AuthorName));
        SeedData.Books.Should().OnlyContain(b => categorySlugs.Contains(b.CategorySlug));
    }

    [Fact]
    public void Books_HavePositivePriceAndNonNegativeStock()
    {
        SeedData.Books.Should().OnlyContain(b => b.Price > 0);
        SeedData.Books.Should().OnlyContain(b => b.Stock >= 0);
        SeedData.Books.Should().OnlyContain(b => b.Stock <= 100, "stock demo amounts should stay realistic");
    }
}
