// Models/ViewModels/BookViewModel.cs - CẬP NHẬT
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class BookViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        [Display(Name = "Book Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required")]
        [StringLength(100, ErrorMessage = "Author name cannot be longer than 100 characters")]
        [Display(Name = "Author")]
        public string Author { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(1000, 9999000, ErrorMessage = "Price must be between 1000 and 9999000")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Range(0.01, 9999.99, ErrorMessage = "Discount price must be between 0.01 and 9999.99")]
        [Display(Name = "Discount Price")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [StringLength(100, ErrorMessage = "Publisher name cannot be longer than 100 characters")]
        [Display(Name = "Publisher")]
        public string? Publisher { get; set; }

        [Display(Name = "Publish Date")]
        public DateTime? PublishDate { get; set; }

        [Display(Name = "Date Create")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Update last")]
        public DateTime? LastUpdatedDate { get; set; }

        [Range(1, 9999, ErrorMessage = "Page count must be between 1 and 9999")]
        [Display(Name = "Page Count")]
        public int? PageCount { get; set; }

        [StringLength(50, ErrorMessage = "Language cannot be longer than 50 characters")]
        [Display(Name = "Language")]
        public string? Language { get; set; }

        // Image properties
        [Display(Name = "Book Cover Image")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Upload New Image")]
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Remove Current Image")]
        [NotMapped]
        public bool RemoveImage { get; set; }

        [NotMapped]
        public string? ImageFileName { get; set; }

        [NotMapped]
        public string? ImageContentType { get; set; }

        [NotMapped]
        public long? ImageFileSize { get; set; }

        // ====== THÊM MỚI: Gallery Images Properties ======
        [StringLength(2000)]
        [Display(Name = "Gallery Images (JSON)")]
        public string? AdditionalImages { get; set; }

        [NotMapped]
        public List<string> GalleryImageUrls
        {
            get
            {
                if (string.IsNullOrEmpty(AdditionalImages))
                    return new List<string>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(AdditionalImages)
                        ?? new List<string>();
                }
                catch (Exception)
                {
                    return new List<string>();
                }
            }
            set
            {
                AdditionalImages = value?.Any() == true
                    ? System.Text.Json.JsonSerializer.Serialize(value)
                    : null;
            }
        }

        [NotMapped]
        public bool HasGalleryImages => GalleryImageUrls.Any();

        [NotMapped]
        public int GalleryImageCount => GalleryImageUrls.Count;

        [NotMapped]
        [Display(Name = "Upload Gallery Images")]
        public IFormFileCollection? GalleryImageFiles { get; set; }
        // ====== KẾT THÚC PHẦN THÊM MỚI ======

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties for display
        public Category? Category { get; set; }
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Calculated properties
        public decimal AverageRating => Reviews.Any() ? (decimal)Reviews.Average(r => r.Rating) : 0;
        public int ReviewCount => Reviews.Count();
        public bool InStock => StockQuantity > 0;
        public decimal DisplayPrice => DiscountPrice ?? Price;
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
        public decimal DiscountPercentage => HasDiscount
            ? Math.Round(((Price - DiscountPrice.Value) / Price) * 100, 0)
            : 0;

        // Image helper properties
        public string DefaultImageUrl => ImageUrl ?? "/images/books/default-book.jpg";
        public bool HasImage => !string.IsNullOrEmpty(ImageUrl);
        public string ImageAlt => $"Cover image of {Title} by {Author}";
        public string FormattedFileSize => ImageFileSize.HasValue
            ? FormatFileSize(ImageFileSize.Value)
            : "Unknown";

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    // Các class khác giữ nguyên...
    public class BookListViewModel
    {
        public IEnumerable<BookViewModel> Books { get; set; } = new List<BookViewModel>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        // Filtering
        public int? CategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public bool InStock { get; set; }
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public string? Language { get; set; }
        public DateTime? CreatedAfter { get; set; }


        // Sorting
        public string SortBy { get; set; } = "title";
         public string SortOrder { get; set; } = string.Empty;

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int CurrentPage => PageNumber;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // Display options
        public string ViewMode { get; set; } = "grid";

        public Dictionary<string, string> SortOptions => new()
        {
            {"title", "Title A-Z"},
            {"title_desc", "Title Z-A"},
            {"author", "Author A-Z"},
            {"author_desc", "Author Z-A"},
            {"price", "Price Low to High"},
            {"price_desc", "Price High to Low"},
            {"rating", "Rating Low to High"},
            {"rating_desc", "Rating High to Low"},
            {"newest", "Newest First"},
            {"oldest", "Oldest First"},
            {"popularity", "Most Popular"}
        };

        public string GetActiveFiltersCount()
        {
            var count = 0;
            if (CategoryId.HasValue) count++;
            if (!string.IsNullOrEmpty(SearchTerm)) count++;
            if (MinPrice.HasValue) count++;
            if (MaxPrice.HasValue) count++;
            if (MinRating.HasValue) count++;
            if (InStock) count++;
            if (!string.IsNullOrEmpty(Author)) count++;
            if (!string.IsNullOrEmpty(Publisher)) count++;
            if (!string.IsNullOrEmpty(Language)) count++;

            return count > 0 ? $"({count})" : "";
        }
    }

    public class BookDetailsViewModel
    {
        public BookViewModel Book { get; set; } = new();
        public IEnumerable<BookViewModel> RelatedBooks { get; set; } = new List<BookViewModel>();
        public ReviewCreateViewModel ReviewForm { get; set; } = new();
        public bool CanReview { get; set; }
        public bool HasPurchased { get; set; }
        public bool AlreadyReviewed { get; set; }
        public bool IsInWishlist { get; set; }
        public int CartQuantity { get; set; }
    }
}