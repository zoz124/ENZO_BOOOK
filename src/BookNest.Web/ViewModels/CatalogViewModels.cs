using System.ComponentModel.DataAnnotations;
using BookNest.Core.Models;

namespace BookNest.Web.ViewModels;

public class CatalogViewModel
{
    public PagedResult<BookCardDto> Books { get; set; } = PagedResult<BookCardDto>.Empty(12);
    public CatalogFilter Filter { get; set; } = new();
    public IReadOnlyList<LookupDto> Categories { get; set; } = Array.Empty<LookupDto>();
    public IReadOnlyList<LookupDto> Authors { get; set; } = Array.Empty<LookupDto>();
}

public class BookDetailsViewModel
{
    public BookDetailsDto Book { get; set; } = null!;
    public IReadOnlyList<BookCardDto> Similar { get; set; } = Array.Empty<BookCardDto>();
    public bool InWishlist { get; set; }
    public bool IsSignedIn { get; set; }
    public BookReviewsDto Reviews { get; set; } = new(0, 0, Array.Empty<ReviewDto>(), null, false);
}

public class CheckoutViewModel
{
    public CartSummary Cart { get; set; } = new(Array.Empty<CartLine>(), 0, 0, 0);
    public IReadOnlyList<AddressDto> Addresses { get; set; } = Array.Empty<AddressDto>();
    public CheckoutForm Form { get; set; } = new();
}

public class CheckoutForm
{
    [Display(Name = "Saved address")]
    public int? AddressId { get; set; }

    [MaxLength(100), Display(Name = "Full name")]
    public string? FullName { get; set; }

    [RegularExpression("^01[0125][0-9]{8}$", ErrorMessage = "Enter a valid Egyptian mobile number (11 digits).")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [MaxLength(100), Display(Name = "City / governorate")]
    public string? City { get; set; }

    [MaxLength(500), MinLength(10, ErrorMessage = "The address needs at least 10 characters.")]
    [Display(Name = "Address")]
    public string? AddressLine { get; set; }

    [MaxLength(20), Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [Display(Name = "Save this address to my account")]
    public bool SaveAddress { get; set; } = true;

    [Required, Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = "Cash";

    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class OrderDetailsViewModel
{
    public OrderDetailsDto Order { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public IReadOnlyList<Core.Enums.OrderStatus> AllowedNext { get; set; } = Array.Empty<Core.Enums.OrderStatus>();
}

public class AdminBookFormViewModel
{
    public int? Id { get; set; }

    [Required, MaxLength(150), Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(13), MinLength(13, ErrorMessage = "The ISBN must be exactly 13 digits.")]
    [RegularExpression("^[0-9]{13}$", ErrorMessage = "The ISBN accepts digits only (13 digits).")]
    [Display(Name = "ISBN")]
    public string Isbn { get; set; } = string.Empty;

    [MaxLength(2000), Display(Name = "Description")]
    public string? Description { get; set; }

    [Range(1, 100000, ErrorMessage = "The price must be greater than zero."), Display(Name = "Price")]
    public decimal Price { get; set; }

    [Display(Name = "Discount price")]
    public decimal? DiscountPrice { get; set; }

    [Range(0, 100000), Display(Name = "Stock")]
    public int Stock { get; set; }

    [Range(1, 10000), Display(Name = "Pages")]
    public int? Pages { get; set; }

    [MaxLength(100), Display(Name = "Publisher")]
    public string? Publisher { get; set; }

    [MaxLength(10), Display(Name = "Language")]
    public string? Language { get; set; } = "ar";

    [Display(Name = "Published on")]
    [DataType(DataType.Date)]
    public DateTime? PublishedDate { get; set; }

    [Required(ErrorMessage = "Choose an author."), Display(Name = "Author")]
    public int AuthorId { get; set; }

    [Required(ErrorMessage = "Choose a category."), Display(Name = "Category")]
    public int CategoryId { get; set; }

    public string? CoverImageUrl { get; set; }

    [Display(Name = "Cover image")]
    public IFormFile? Cover { get; set; }

    public IReadOnlyList<LookupDto> Categories { get; set; } = Array.Empty<LookupDto>();
    public IReadOnlyList<LookupDto> Authors { get; set; } = Array.Empty<LookupDto>();
}

public class AdminOrdersViewModel
{
    public PagedResult<OrderSummaryDto> Orders { get; set; } = PagedResult<OrderSummaryDto>.Empty(25);
    public Core.Enums.OrderStatus? Status { get; set; }
    public string? Search { get; set; }
}

public class SimpleNameFormViewModel
{
    public int? Id { get; set; }

    [Required, MaxLength(100), Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100), Display(Name = "Arabic name")]
    public string? NameAr { get; set; }

    [MaxLength(1000), Display(Name = "Biography")]
    public string? Biography { get; set; }

    [MaxLength(500), Display(Name = "Description")]
    public string? Description { get; set; }
}
