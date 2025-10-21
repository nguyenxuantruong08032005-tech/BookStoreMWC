// Models/ViewModels/HomeViewModel.cs
using BookStoreMVC.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models.ViewModels
{
    public class HomeViewModel
    {
        // Featured content for homepage
        public List<BookViewModel> FeaturedBooks { get; set; } = new();
        public List<BookViewModel> NewBooks { get; set; } = new();
        public List<BookViewModel> BestSellers { get; set; } = new();
        public List<Category> PopularCategories { get; set; } = new();

        // Statistics for homepage display
        public int TotalBooks { get; set; }
        public int TotalCategories { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }

        // Recent activity
        public List<ReviewViewModel> RecentReviews { get; set; } = new();

        // Homepage banners/promotions
        public List<PromotionBanner> Banners { get; set; } = new();

        // Testimonials
        public List<CustomerTestimonial> Testimonials { get; set; } = new();

        // Newsletter signup stats
        public int NewsletterSubscribers { get; set; }

        // Special offers
        public List<BookViewModel> DiscountedBooks { get; set; } = new();

        // Categories with book counts for display
        public List<CategoryWithCount> CategoriesWithCounts { get; set; } = new();
    }

    // Supporting classes for HomePage
    public class PromotionBanner
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string LinkUrl { get; set; } = string.Empty;
        public string ButtonText { get; set; } = "Learn More";
        public bool IsActive { get; set; } = true;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string BackgroundColor { get; set; } = "#3B82F6";
        public string TextColor { get; set; } = "#FFFFFF";
    }

    public class CustomerTestimonial
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerTitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; } = 5;
        public string CustomerImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Calculated properties
        public string StarDisplay => new string('★', Rating) + new string('☆', 5 - Rating);
        public string DisplayName => !string.IsNullOrEmpty(CustomerTitle)
            ? $"{CustomerName}, {CustomerTitle}"
            : CustomerName;
    }

    public class CategoryWithCount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public bool IsActive { get; set; }

        // For display purposes
        public string DisplayText => $"{Name} ({BookCount})";
        public bool HasBooks => BookCount > 0;
    }
}

// Models/ViewModels/HomePageSectionViewModel.cs - For partial views
namespace BookStoreMVC.Models.ViewModels
{
    public class FeaturedBooksSection
    {
        public string SectionTitle { get; set; } = "Featured Books";
        public string SectionDescription { get; set; } = "Discover our hand-picked selection of must-read books";
        public List<BookViewModel> Books { get; set; } = new();
        public string ViewAllUrl { get; set; } = "/Books";
        public int MaxDisplayCount { get; set; } = 8;
    }

    public class NewBooksSection
    {
        public string SectionTitle { get; set; } = "New Arrivals";
        public string SectionDescription { get; set; } = "Fresh picks just added to our collection";
        public List<BookViewModel> Books { get; set; } = new();
        public string ViewAllUrl { get; set; } = "/Books?sortBy=newest";
        public int MaxDisplayCount { get; set; } = 8;
    }

    public class BestSellersSection
    {
        public string SectionTitle { get; set; } = "Best Sellers";
        public string SectionDescription { get; set; } = "Most popular books loved by our readers";
        public List<BookViewModel> Books { get; set; } = new();
        public string ViewAllUrl { get; set; } = "/Books?sortBy=popularity";
        public int MaxDisplayCount { get; set; } = 8;
    }

    public class CategoriesSection
    {
        public string SectionTitle { get; set; } = "Browse Categories";
        public string SectionDescription { get; set; } = "Explore books by your favorite genres";
        public List<CategoryWithCount> Categories { get; set; } = new();
        public string ViewAllUrl { get; set; } = "/Books";
        public int MaxDisplayCount { get; set; } = 8;
    }

    public class ReviewsSection
    {
        public string SectionTitle { get; set; } = "What Our Readers Say";
        public string SectionDescription { get; set; } = "Real reviews from book lovers like you";
        public List<ReviewViewModel> Reviews { get; set; } = new();
        public string ViewAllUrl { get; set; } = "/Reviews";
        public int MaxDisplayCount { get; set; } = 6;

        // Statistics
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }

    public class PromotionsSection
    {
        public string SectionTitle { get; set; } = "Special Offers";
        public string SectionDescription { get; set; } = "Don't miss these amazing deals";
        public List<PromotionBanner> Promotions { get; set; } = new();
        public List<BookViewModel> DiscountedBooks { get; set; } = new();
        public int MaxDisplayCount { get; set; } = 4;
    }
}

// Models/ViewModels/HomeStatsViewModel.cs - For statistics display
namespace BookStoreMVC.Models.ViewModels
{
    public class HomeStatsViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalCategories { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int HappyCustomers { get; set; }
        public int BooksSold { get; set; }
        public int YearsInBusiness { get; set; }

        // Formatted display properties
        public string FormattedRevenue => TotalRevenue >= 1000000
            ? $"${TotalRevenue / 1000000:F1}M+"
            : $"${TotalRevenue / 1000:F0}K+";

        public string FormattedBooksSold => BooksSold >= 1000000
            ? $"{BooksSold / 1000000:F1}M+"
            : $"{BooksSold / 1000:F0}K+";

        public string FormattedCustomers => HappyCustomers >= 1000000
            ? $"{HappyCustomers / 1000000:F1}M+"
            : $"{HappyCustomers / 1000:F0}K+";
    }
}

// Models/ViewModels/NewsletterViewModel.cs - For newsletter signup
namespace BookStoreMVC.Models.ViewModels
{
    public class NewsletterSignupViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Preferred Categories")]
        public List<int> PreferredCategoryIds { get; set; } = new();

        public List<Category> AvailableCategories { get; set; } = new();

        [Display(Name = "I agree to receive promotional emails")]
        public bool AgreeToEmails { get; set; } = true;

        // Success/Error states
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}