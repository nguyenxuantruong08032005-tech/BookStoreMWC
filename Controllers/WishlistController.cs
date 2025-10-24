// Controllers/WishlistController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;

namespace BookStoreMVC.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(
            IWishlistService wishlistService,
            UserManager<User> userManager,
            ILogger<WishlistController> logger)
        {
            _wishlistService = wishlistService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var wishlist = await _wishlistService.GetWishlistAsync(userId);

                ViewBag.PageTitle = "My Wishlist";
                return View(wishlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wishlist");
                return View(new WishlistViewModel());
            }
        }
[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle([FromBody] AddToWishlistViewModel? model)
        {
            try
            {
                if (model == null || model.BookId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var (success, isInWishlist) = await _wishlistService.ToggleWishlistItemAsync(userId, model.BookId);

                if (!success)
                {
                    return Json(new { success = false, message = "Unable to update wishlist." });
                }

                var wishlistCount = await _wishlistService.GetWishlistItemCountAsync(userId);

                return Json(new
                {
                    success = true,
                    isInWishlist,
                    wishlistCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling wishlist item");
                return Json(new { success = false, message = "An error occurred while updating wishlist." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(AddToWishlistViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var success = await _wishlistService.AddToWishlistAsync(userId, model);

                if (success)
                {
                    var itemCount = await _wishlistService.GetWishlistItemCountAsync(userId);
                    return Json(new { success = true, message = "Item added to wishlist!", wishlistItemCount = itemCount });
                }
                else
                {
                    return Json(new { success = false, message = "Unable to add item to wishlist." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to wishlist");
                return Json(new { success = false, message = "An error occurred while adding the item to wishlist." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(int bookId)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var success = await _wishlistService.RemoveFromWishlistAsync(userId, bookId);

                if (success)
                {
                    var itemCount = await _wishlistService.GetWishlistItemCountAsync(userId);
                    return Json(new
                    {
                        success = true,
                        message = "Item removed from wishlist!",
                        wishlistItemCount = itemCount
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Unable to remove item from wishlist." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from wishlist");
                return Json(new { success = false, message = "An error occurred while removing the item." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int bookId, int quantity = 1)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var success = await _wishlistService.MoveToCartAsync(userId, bookId, quantity);

                if (success)
                {
                    var itemCount = await _wishlistService.GetWishlistItemCountAsync(userId);
                    return Json(new
                    {
                        success = true,
                        message = "Item moved to cart!",
                        wishlistItemCount = itemCount
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Unable to move item to cart. Please check stock availability." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving item to cart");
                return Json(new { success = false, message = "An error occurred while moving the item to cart." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWishlist()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var success = await _wishlistService.ClearWishlistAsync(userId);

                if (success)
                {
                    return Json(new { success = true, message = "Wishlist cleared successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Unable to clear wishlist." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing wishlist");
                return Json(new { success = false, message = "An error occurred while clearing the wishlist." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlistCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { count = 0 });
                }

                var count = await _wishlistService.GetWishlistItemCountAsync(userId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist count");
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckWishlistStatus(int bookId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { inWishlist = false });
                }

                var inWishlist = await _wishlistService.IsBookInWishlistAsync(userId, bookId);
                return Json(new { inWishlist });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking wishlist status");
                return Json(new { inWishlist = false });
            }
        }
    }
}