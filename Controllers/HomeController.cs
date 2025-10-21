// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;
using System.Diagnostics;

namespace BookStoreMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IUserService _userService;
        private readonly IOrderService _orderService;
        private readonly IReviewService _reviewService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IBookService bookService,
            IUserService userService,
            IOrderService orderService,
            IReviewService reviewService,
            UserManager<User> userManager,
            ILogger<HomeController> logger)
        {
            _bookService = bookService;
            _userService = userService;
            _orderService = orderService;
            _reviewService = reviewService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured content
                var featuredBooks = await _bookService.GetFeaturedBooksAsync(8);
                var newBooks = await _bookService.GetNewBooksAsync(8);
                var bestSellers = await _bookService.GetBestSellersAsync(8);
                var categories = await _bookService.GetCategoriesAsync();

                // Get statistics
                var totalUsers = await _userService.GetTotalUsersCountAsync();
                var (totalRevenue, totalOrders, _) = await _orderService.GetOrderStatisticsAsync();

                // Get recent reviews for testimonials
                var recentReviewsModel = new ReviewListViewModel
                {
                    PageSize = 6,
                    ShowUnapproved = false
                };
                var (recentReviews, _) = await _reviewService.GetReviewsAsync(recentReviewsModel);

                // Create categories with counts
                var categoriesWithStats = await _bookService.GetCategoriesWithStatsAsync();
                var categoriesWithCounts = categoriesWithStats.Take(8).Select(c => new CategoryWithCount
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description ?? "",
                    ImageUrl = c.ImageUrl ?? "",
                    BookCount = c.BookCount,
                    IsActive = c.IsActive
                }).ToList();

                // Get discounted books
                var discountedBooksModel = new BookListViewModel
                {
                    PageSize = 6,
                    SortBy = "discount"
                };
                var (allBooks, _) = await _bookService.GetBooksAsync(discountedBooksModel);
                var discountedBooks = allBooks.Where(b => b.HasDiscount).Take(6).ToList();

                // Sample banners (in real app, these would come from a service/database)
                var banners = GetSampleBanners();

                // Sample testimonials (in real app, these would come from a service/database)
                var testimonials = GetSampleTestimonials();

                var model = new HomeViewModel
                {
                    FeaturedBooks = featuredBooks.ToList(),
                    NewBooks = newBooks.ToList(),
                    BestSellers = bestSellers.ToList(),
                    PopularCategories = categories.Take(8).ToList(),
                    TotalBooks = allBooks.Count(),
                    TotalCategories = categories.Count(),
                    TotalUsers = totalUsers,
                    TotalOrders = totalOrders,
                    RecentReviews = recentReviews.ToList(),
                    DiscountedBooks = discountedBooks,
                    CategoriesWithCounts = categoriesWithCounts,
                    Banners = banners,
                    Testimonials = testimonials,
                    NewsletterSubscribers = 5420 // Sample number, would come from database
                };

                ViewBag.PageTitle = "Welcome to BookStore - Your Literary Adventure Starts Here";
                ViewBag.MetaDescription = "Discover thousands of books across all genres. From bestsellers to hidden gems, find your perfect read and start unforgettable journeys.";

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                return View(new HomeViewModel());
            }
        }

        public IActionResult About()
        {
            ViewBag.PageTitle = "About Us - BookStore";
            ViewBag.MetaDescription = "Learn about BookStore's mission to connect readers with their perfect books and build a community of book lovers.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.PageTitle = "Contact Us - BookStore";
            ViewBag.MetaDescription = "Get in touch with BookStore. We're here to help with any questions about books, orders, or our services.";
            return View();
        }

        public IActionResult Privacy()
        {
            ViewBag.PageTitle = "Privacy Policy - BookStore";
            return View();
        }

        public IActionResult Terms()
        {
            ViewBag.PageTitle = "Terms of Service - BookStore";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribeNewsletter(NewsletterSignupViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please provide a valid email address." });
                }

                // In a real application, you would save this to a newsletter service/database
                // For now, we'll just simulate success
                _logger.LogInformation("Newsletter subscription for email: {Email}", model.Email);

                return Json(new
                {
                    success = true,
                    message = "Thank you for subscribing! You'll receive our latest book recommendations and exclusive offers."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to newsletter");
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        // AJAX endpoints for dynamic content loading
        [HttpGet]
        public async Task<IActionResult> GetFeaturedBooks(int count = 4)
        {
            try
            {
                var books = await _bookService.GetFeaturedBooksAsync(count);
                var result = books.Select(b => new
                {
                    id = b.Id,
                    title = b.Title,
                    author = b.Author,
                    price = b.Price,
                    discountPrice = b.DiscountPrice,
                    displayPrice = b.DisplayPrice,
                    hasDiscount = b.HasDiscount,
                    averageRating = b.AverageRating,
                    reviewCount = b.ReviewCount,
                    inStock = b.InStock,
                    category = b.Category?.Name
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured books");
                return Json(new { success = false, message = "Error loading featured books" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new { suggestions = new List<object>() });
            }

            try
            {
                var books = await _bookService.SearchBooksAsync(term, 5);
                var suggestions = books.Select(b => new
                {
                    id = b.Id,
                    title = b.Title,
                    author = b.Author,
                    displayPrice = b.DisplayPrice,
                    category = b.Category?.Name,
                    url = Url.Action("Details", "Books", new { id = b.Id, title = b.Title.Replace(" ", "-").ToLower() })
                }).ToList();

                return Json(new { success = true, suggestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
                return Json(new { success = false, suggestions = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHomeStats()
        {
            try
            {
                var totalUsers = await _userService.GetTotalUsersCountAsync();
                var (totalRevenue, totalOrders, averageOrderValue) = await _orderService.GetOrderStatisticsAsync();

                // Sample data for some stats (in real app, these would come from database)
                var stats = new HomeStatsViewModel
                {
                    TotalBooks = 15420, // Would come from book service
                    TotalCategories = 25,
                    TotalCustomers = totalUsers,
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    HappyCustomers = totalUsers,
                    BooksSold = totalOrders * 2, // Approximate
                    YearsInBusiness = 15
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting home stats");
                return Json(new { success = false, message = "Error loading statistics" });
            }
        }

        // Helper methods for sample data
        private List<PromotionBanner> GetSampleBanners()
        {
            return new List<PromotionBanner>
            {
                new PromotionBanner
                {
                    Id = 1,
                    Title = "Summer Reading Sale",
                    Description = "Get up to 40% off on bestselling novels perfect for your summer reading list",
                    ImageUrl = "/images/banners/summer-sale.jpg",
                    LinkUrl = "/Books?category=fiction&discount=true",
                    ButtonText = "Shop Now",
                    IsActive = true,
                    StartDate = DateTime.Now.AddDays(-7),
                    EndDate = DateTime.Now.AddDays(30),
                    BackgroundColor = "#F59E0B",
                    TextColor = "#FFFFFF"
                },
                new PromotionBanner
                {
                    Id = 2,
                    Title = "New Arrivals Weekly",
                    Description = "Discover fresh titles added every week from your favorite authors and new voices",
                    ImageUrl = "/images/banners/new-arrivals.jpg",
                    LinkUrl = "/Books?sortBy=newest",
                    ButtonText = "Explore",
                    IsActive = true,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(365),
                    BackgroundColor = "#8B5CF6",
                    TextColor = "#FFFFFF"
                }
            };
        }

        private List<CustomerTestimonial> GetSampleTestimonials()
        {
            return new List<CustomerTestimonial>
            {
                new CustomerTestimonial
                {
                    Id = 1,
                    CustomerName = "Sarah Johnson",
                    CustomerTitle = "Book Blogger",
                    Content = "BookStore has the best selection of books I've ever seen. Their recommendations are always spot-on, and the customer service is exceptional.",
                    Rating = 5,
                    CustomerImageUrl = "/images/testimonials/sarah.jpg",
                    CreatedAt = DateTime.Now.AddDays(-15),
                    IsActive = true
                },
                new CustomerTestimonial
                {
                    Id = 2,
                    CustomerName = "Michael Chen",
                    CustomerTitle = "Teacher",
                    Content = "As an educator, I appreciate how easy it is to find quality educational materials and literature for my students. The bulk ordering process is seamless.",
                    Rating = 5,
                    CustomerImageUrl = "/images/testimonials/michael.jpg",
                    CreatedAt = DateTime.Now.AddDays(-8),
                    IsActive = true
                },
                new CustomerTestimonial
                {
                    Id = 3,
                    CustomerName = "Emma Wilson",
                    CustomerTitle = "Reading Enthusiast",
                    Content = "The wishlist feature and personalized recommendations have helped me discover so many amazing books I wouldn't have found otherwise. Love this store!",
                    Rating = 5,
                    CustomerImageUrl = "/images/testimonials/emma.jpg",
                    CreatedAt = DateTime.Now.AddDays(-22),
                    IsActive = true
                }
            };
        }
    }

    // Error ViewModel
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }
    }
}