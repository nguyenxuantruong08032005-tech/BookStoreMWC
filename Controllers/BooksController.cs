// Controllers/BooksController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
namespace BookStoreMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IReviewService _reviewService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<BooksController> _logger;

        public BooksController(
            IBookService bookService,
            IReviewService reviewService,
            UserManager<User> userManager,
            ILogger<BooksController> logger)
        {
            _bookService = bookService;
            _reviewService = reviewService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(BookListViewModel model)
        {
            try
            {
                NormalizeSorting(Request, model);
                 NormalizePagination(model);
                var (books, totalCount) = await _bookService.GetBooksAsync(model);

                model.Books = books;
                model.TotalCount = totalCount;
                model.Categories = await _bookService.GetCategoriesAsync();
                ViewBag.PageTitle = BuildPageTitle(model);
                ViewBag.PageDescription = BuildPageDescription(model, totalCount);


                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading books page");
                return View(new BookListViewModel());
            }
        }
  public async Task<IActionResult> NewBooks(string? searchTerm, string filter = "all", int page = 1)
        {
            try
            {
                filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.Trim().ToLowerInvariant();
                var newnessThreshold = DateTime.UtcNow.AddDays(-120);

                var listingModel = new BookListViewModel
                {
                    SearchTerm = searchTerm,
                    SortBy = "newest",
                    SortOrder = "desc",
                    PageNumber = Math.Max(page, 1),
                    PageSize = 12,
                    CreatedAfter = newnessThreshold
                };

                switch (filter)
                {
                    case "hot":
                        listingModel.SortBy = "rating";
                        listingModel.SortOrder = "desc";
                        break;
                    case "discount":
                        listingModel.SortBy = "discount";
                        listingModel.SortOrder = "desc";
                        break;
                    case "price":
                        listingModel.SortBy = "price";
                        listingModel.SortOrder = "asc";
                        break;
                }

                var (books, totalCount) = await _bookService.GetBooksAsync(listingModel);
                var booksList = books.ToList();
                listingModel.Books = booksList;
                listingModel.TotalCount = totalCount;

                var categories = (await _bookService.GetCategoriesAsync()).ToList();
                listingModel.Categories = categories;

                var categoriesWithStats = (await _bookService.GetCategoriesWithStatsAsync()).ToList();
                var highlightCategories = categoriesWithStats
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.BookCount)
                    .Take(6)
                    .ToList();

                var spotlightBooks = (await _bookService.GetNewBooksAsync(6)).ToList();
                var trendingBooks = spotlightBooks
                    .OrderByDescending(b => b.ReviewCount)
                    .ThenByDescending(b => b.AverageRating)
                    .Take(6)
                    .ToList();

                var (_, booksAddedThisMonth) = await _bookService.GetBooksAsync(new BookListViewModel
                {
                    CreatedAfter = DateTime.UtcNow.AddDays(-30),
                    SortBy = "newest",
                    SortOrder = "desc",
                    PageSize = 1
                });

                var model = new NewBooksPageViewModel
                {
                    ActiveFilter = filter,
                    Listing = listingModel,
                    SpotlightBooks = spotlightBooks.Take(3).ToList(),
                    TrendingBooks = trendingBooks,
                    HighlightCategories = highlightCategories,
                    TotalNewBooks = totalCount,
                    BooksAddedThisMonth = booksAddedThisMonth,
                    LatestAddedDate = spotlightBooks.FirstOrDefault()?.CreatedDate,
                    ActiveCategories = highlightCategories.Any()
                        ? highlightCategories.Count
                        : categories.Count(c => c.IsActive)
                };

                ViewBag.PageTitle = "Sách mới phát hành";
                ViewBag.PageDescription = $"Khám phá {model.TotalNewBooks:N0} tựa sách mới nhất được cập nhật gần đây.";

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading new books page");
                return View(new NewBooksPageViewModel
                {
                    Listing = new BookListViewModel()
                });
            }
        }

        public async Task<IActionResult> Bestseller(BookListViewModel model)
        {
            try
            {
                model.SortBy = "bestseller";
                NormalizeSorting(Request, model);
                 NormalizePagination(model);

                var (books, totalCount) = await _bookService.GetBooksAsync(model);

                model.Books = books;
                model.TotalCount = totalCount;
                model.Categories = await _bookService.GetCategoriesAsync();

                ViewBag.PageTitle = "Top sách bán chạy";
                ViewBag.PageDescription = $"Khám phá {totalCount.ToString("N0")} tựa sách được độc giả tin tưởng và lựa chọn nhiều nhất.";

                return View("Bestseller", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bestseller books page");

                ViewBag.PageTitle = "Top sách bán chạy";
                ViewBag.PageDescription = "Khám phá những tựa sách bán chạy và được cộng đồng yêu thích nhất.";

                return View("Bestseller", new BookListViewModel
                {
                    SortBy = "bestseller",
                    SortOrder = "desc"
                });
            }
        }

        public async Task<IActionResult> Promotions(BookListViewModel model)
        {
            try
            {
                model.SortBy = "discount";
                NormalizeSorting(Request, model);
                 NormalizePagination(model);

                var (books, totalCount) = await _bookService.GetBooksAsync(model);

                model.Books = books;
                model.TotalCount = totalCount;
                model.Categories = await _bookService.GetCategoriesAsync();

                ViewBag.PageTitle = "Ưu đãi & Khuyến mãi";
                ViewBag.PageDescription = $"Săn ưu đãi hấp dẫn với {totalCount.ToString("N0")} đầu sách đang giảm giá sâu hôm nay.";

                return View("Promotions", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading promotions page");

                ViewBag.PageTitle = "Ưu đãi & Khuyến mãi";
                ViewBag.PageDescription = "Khám phá các chương trình khuyến mãi sách nổi bật nhất.";

                return View("Promotions", new BookListViewModel
                {
                    SortBy = "discount",
                    SortOrder = "desc"
                });
            }
        }

        private static void NormalizeSorting(HttpRequest request, BookListViewModel model)
        {
            var sortBy = string.IsNullOrWhiteSpace(model.SortBy)
                ? "title"
                : model.SortBy.Trim().ToLowerInvariant();

            model.SortBy = sortBy;

            var hasExplicitSortOrder = request.Query.ContainsKey("sortOrder");

            if (!hasExplicitSortOrder || string.IsNullOrWhiteSpace(model.SortOrder))
            {
                model.SortOrder = GetDefaultSortOrder(sortBy);
            }
            else
            {
                model.SortOrder = model.SortOrder.Trim().ToLowerInvariant();
            }

            if (!hasExplicitSortOrder && model.SortBy is "featured" or "bestseller")
            {
                model.SortOrder = "desc";
            }
        }
 private static void NormalizePagination(BookListViewModel model)
        {
            if (model.PageNumber < 1)
            {
                model.PageNumber = 1;
            }

            if (model.PageSize < 1)
            {
                model.PageSize = 12;
            }
        }
        private static string GetDefaultSortOrder(string sortBy) => sortBy switch
        {
            "newest" => "desc",
            "bestseller" => "desc",
            "discount" => "desc",
            "rating" => "desc",
            _ => "asc"
        };

        private static string BuildPageTitle(BookListViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                return $"Kết quả cho '{model.SearchTerm}'";
            }

            return model.SortBy switch
            {
                "featured" => "Sách nổi bật",
                "newest" => "Sách mới nhất",
                "bestseller" => "Sách bán chạy",
                "discount" => "Sách đang giảm giá",
                _ => "Tất cả sách"
            };
        }

        private static string BuildPageDescription(BookListViewModel model, int totalCount)
        {
            var formattedCount = totalCount.ToString("N0");

            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                return $"Có {formattedCount} kết quả phù hợp với từ khóa '{model.SearchTerm}'.";
            }

            return model.SortBy switch
            {
                "featured" => $"Khám phá {formattedCount} tựa sách được độc giả yêu thích nhất trong hệ thống của chúng tôi.",
                "newest" => $"Cập nhật những cuốn sách vừa lên kệ với {formattedCount} lựa chọn mới nhất.",
                "bestseller" => $"Top {formattedCount} đầu sách bán chạy đang được nhiều độc giả lựa chọn.",
                "discount" => $"Săn ưu đãi hấp dẫn với {formattedCount} đầu sách đang giảm giá.",
                _ => $"Khám phá {formattedCount} đầu sách thuộc nhiều thể loại khác nhau."
            };
        }
        public async Task<IActionResult> Details(int id, string? title)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var model = await _bookService.GetBookDetailsAsync(id, userId);

                if (model?.Book == null)
                {
                    return NotFound();
                }

                // ✅ Debug info for gallery images
                _logger.LogInformation(
                    "Book {BookId} - AdditionalImages: {AdditionalImages}, GalleryCount: {GalleryCount}, HasGallery: {HasGallery}",
                    id,
                    model.Book.AdditionalImages?.Substring(0, Math.Min(100, model.Book.AdditionalImages?.Length ?? 0)) ?? "null",
                    model.Book.GalleryImageCount,
                    model.Book.HasGalleryImages
                );

                ViewBag.PageTitle = model.Book.Title;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading book details for ID: {BookId}", id);
                return NotFound();
            }
        }

        public async Task<IActionResult> Category(int id, string? name)
        {
            try
            {
                var category = await _bookService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                var model = new BookListViewModel
                {
                    CategoryId = id,
                    PageSize = 12
                };

                var (books, totalCount) = await _bookService.GetBooksAsync(model);

                model.Books = books;
                model.TotalCount = totalCount;
                model.Categories = await _bookService.GetCategoriesAsync();

                ViewBag.Category = category;
                ViewBag.PageTitle = $"{category.Name} Books";

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category page for ID: {CategoryId}", id);
                return NotFound();
            }
        }

        public async Task<IActionResult> Search(string searchTerm, int page = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return RedirectToAction(nameof(Index));
                }

                var model = new BookListViewModel
                {
                    SearchTerm = searchTerm,
                    PageNumber = Math.Max(page, 1),
                    PageSize = 12
                };

                var (books, totalCount) = await _bookService.GetBooksAsync(model);

                model.Books = books;
                model.TotalCount = totalCount;
                model.Categories = await _bookService.GetCategoriesAsync();

                ViewBag.PageTitle = $"Search Results for '{searchTerm}'";

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching books with term: {SearchTerm}", searchTerm);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int bookId, ReviewCreateViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;

                if (!ModelState.IsValid)
                {
                    return RedirectToAction(nameof(Details), new { id = bookId });
                }

                await _reviewService.CreateReviewAsync(userId, model);
                TempData["SuccessMessage"] = "Your review has been submitted successfully!";

                return RedirectToAction(nameof(Details), new { id = bookId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = bookId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review for book {BookId}", bookId);
                TempData["ErrorMessage"] = "An error occurred while submitting your review.";
                return RedirectToAction(nameof(Details), new { id = bookId });
            }
        }

        // AJAX endpoints
        [HttpGet]
        public async Task<IActionResult> FilterBooks(BookListViewModel model)
        {
            try
            {
                var (books, totalCount) = await _bookService.GetBooksAsync(model);

                var result = new
                {
                    success = true,
                    books = books.Select(b => new
                    {
                        id = b.Id,
                        title = b.Title,
                        author = b.Author,
                        price = b.Price,
                        discountPrice = b.DiscountPrice,
                        displayPrice = b.DisplayPrice,
                        hasDiscount = b.HasDiscount,
                        discountPercentage = b.DiscountPercentage,
                        averageRating = b.AverageRating,
                        reviewCount = b.ReviewCount,
                        inStock = b.InStock,
                        category = b.Category?.Name
                    }),
                    totalCount,
                    totalPages = model.TotalPages
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering books");
                return Json(new { success = false, message = "An error occurred while filtering books." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> QuickSearch(string term)
        {
            try
            {
                if (string.IsNullOrEmpty(term) || term.Length < 2)
                {
                    return Json(new { success = false, message = "Search term too short" });
                }

                var books = await _bookService.SearchBooksAsync(term, 10);

                var result = books.Select(b => new
                {
                    id = b.Id,
                    title = b.Title,
                    author = b.Author,
                    displayPrice = b.DisplayPrice,
                    category = b.Category?.Name
                });

                return Json(new { success = true, books = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search");
                return Json(new { success = false, message = "Search failed" });
            }
        }
    }
}