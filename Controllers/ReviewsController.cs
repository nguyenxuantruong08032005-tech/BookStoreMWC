using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;

namespace BookStoreMVC.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IBookService _bookService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            IReviewService reviewService,
            IBookService bookService,
            UserManager<User> userManager,
            ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService;
            _bookService = bookService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(ReviewListViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // If no specific book filter, show user's reviews
                if (!model.BookId.HasValue && !string.IsNullOrEmpty(userId))
                {
                    var userReviews = await _reviewService.GetReviewsByUserAsync(userId, 50);
                    model.Reviews = userReviews;
                    ViewBag.PageTitle = "My Reviews";
                }
                else
                {
                    var (reviews, totalCount) = await _reviewService.GetReviewsAsync(model);
                    model.Reviews = reviews;
                    model.TotalCount = totalCount;
                    ViewBag.PageTitle = "Reviews";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews");
                return View(new ReviewListViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(int bookId)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var canReview = await _reviewService.CanUserReviewBookAsync(userId, bookId);

                if (!canReview)
                {
                    TempData["ErrorMessage"] = "You can only review books you have purchased and haven't reviewed yet.";
                    return RedirectToAction("Details", "Books", new { id = bookId });
                }

                var book = await _bookService.GetBookByIdAsync(bookId);
                if (book == null)
                {
                    return NotFound();
                }

                var model = new ReviewCreateViewModel
                {
                    BookId = bookId,
                   Book = BookSummaryViewModel.FromBook(book)
                };

                ViewBag.PageTitle = $"Review: {book.Title}";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review creation page");
                return RedirectToAction("Details", "Books", new { id = bookId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var book = await _bookService.GetBookByIdAsync(model.BookId);
                    model.Book = book != null
                        ? BookSummaryViewModel.FromBook(book)
                        : new BookSummaryViewModel { Id = model.BookId };
                    return View(model);
                }

                var userId = _userManager.GetUserId(User)!;
                await _reviewService.CreateReviewAsync(userId, model);

                TempData["SuccessMessage"] = "Your review has been submitted successfully!";
                return RedirectToAction("Details", "Books", new { id = model.BookId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var book = await _bookService.GetBookByIdAsync(model.BookId);
                model.Book = book != null
                    ? BookSummaryViewModel.FromBook(book)
                    : new BookSummaryViewModel { Id = model.BookId };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                ModelState.AddModelError(string.Empty, "An error occurred while submitting your review.");
               var book = await _bookService.GetBookByIdAsync(model.BookId);
                model.Book = book != null
                    ? BookSummaryViewModel.FromBook(book)
                    : new BookSummaryViewModel { Id = model.BookId };
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var review = await _reviewService.GetReviewByIdAsync(id);

                if (review == null || review.UserId != userId)
                {
                    return NotFound();
                }
                  var book = await _bookService.GetBookByIdAsync(review.BookId);
                var model = new ReviewCreateViewModel
                {
                    BookId = review.BookId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                     Book = book != null
                        ? BookSummaryViewModel.FromBook(book)
                        : new BookSummaryViewModel
                        {
                            Id = review.Book.Id,
                            Title = review.Book.Title,
                            Author = review.Book.Author,
                            ImageUrl = review.Book.ImageUrl
                        }
                };

                ViewBag.PageTitle = $"Edit Review: {review.Book.Title}";
                ViewBag.ReviewId = id;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review edit page");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReviewCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                   var book = await _bookService.GetBookByIdAsync(model.BookId);
                    model.Book = book != null
                        ? BookSummaryViewModel.FromBook(book)
                        : new BookSummaryViewModel { Id = model.BookId };
                    ViewBag.ReviewId = id;
                    return View(model);
                }

                var userId = _userManager.GetUserId(User)!;
                var success = await _reviewService.UpdateReviewAsync(id, userId, model);

                if (success)
                {
                    TempData["SuccessMessage"] = "Your review has been updated successfully!";
                    return RedirectToAction("Details", "Books", new { id = model.BookId });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Unable to update review.");
                     var book = await _bookService.GetBookByIdAsync(model.BookId);
                    model.Book = book != null
                        ? BookSummaryViewModel.FromBook(book)
                        : new BookSummaryViewModel { Id = model.BookId };
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review");
                ModelState.AddModelError(string.Empty, "An error occurred while updating your review.");
              var book = await _bookService.GetBookByIdAsync(model.BookId);
                model.Book = book != null
                    ? BookSummaryViewModel.FromBook(book)
                    : new BookSummaryViewModel { Id = model.BookId };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var success = await _reviewService.DeleteReviewAsync(id, userId);

                if (success)
                {
                    return Json(new { success = true, message = "Review deleted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Unable to delete review." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review");
                return Json(new { success = false, message = "An error occurred while deleting the review." });
            }
        }
    }
}