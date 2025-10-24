using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class ReviewCreateViewModel
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [StringLength(2000, ErrorMessage = "Comment cannot be longer than 2000 characters")]
        [Display(Name = "Review Comment")]
        public string? Comment { get; set; }

       [ValidateNever]
        public BookSummaryViewModel Book { get; set; } = new();
    }

    public class BookSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string? CategoryName { get; set; }

        public static BookSummaryViewModel FromBook(BookViewModel book)
        {
            return new BookSummaryViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ImageUrl = book.ImageUrl,
                AverageRating = book.AverageRating,
                ReviewCount = book.ReviewCount,
                CategoryName = book.Category?.Name
            };
        }
    }

    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int BookId { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public bool IsVerifiedPurchase { get; set; }
        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; } // ✅ Thêm thuộc tính thời gian cập nhật

        // Navigation
        public User User { get; set; } = new();
        public Book Book { get; set; } = new();

        // ✅ Hiển thị tên người dùng
        public string UserDisplayName => User?.Name ?? "Anonymous";

        // ✅ Hiển thị sao
        public string StarDisplay => new string('★', Rating) + new string('☆', 5 - Rating);

        // ✅ Tính thời gian hiển thị (ưu tiên UpdatedAt nếu có)
        public string TimeAgo
        {
            get
            {
                var referenceTime = UpdatedAt ?? CreatedAt;
                var timeSpan = DateTime.UtcNow - referenceTime;

                string suffix = UpdatedAt.HasValue ? " (edited)" : string.Empty;

                if (timeSpan.TotalDays >= 365)
                    return $"{(int)(timeSpan.TotalDays / 365)} year(s) ago{suffix}";
                if (timeSpan.TotalDays >= 30)
                    return $"{(int)(timeSpan.TotalDays / 30)} month(s) ago{suffix}";
                if (timeSpan.TotalDays >= 1)
                    return $"{(int)timeSpan.TotalDays} day(s) ago{suffix}";
                if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours} hour(s) ago{suffix}";
                return $"Just now{suffix}";
            }
        }
    }

    public class ReviewListViewModel
    {
        public IEnumerable<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();

        // Filters
        public int? BookId { get; set; }
        public int? Rating { get; set; }
        public bool ShowUnapproved { get; set; }

        // Sorting & Searching
        public string SortBy { get; set; } = "newest";
        public string? SearchTerm { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // Statistics
        public double AverageRating => Reviews.Any() ? Reviews.Average(r => r.Rating) : 0;
        public int[] RatingDistribution => Enumerable.Range(1, 5)
            .Select(i => Reviews.Count(r => r.Rating == i))
            .ToArray();

        // Sorting options
        public Dictionary<string, string> SortOptions => new()
        {
            {"newest", "Newest First"},
            {"oldest", "Oldest First"},
            {"highest_rating", "Highest Rating"},
            {"lowest_rating", "Lowest Rating"}
        };
    }
}
