// Services/IWishlistService.cs & WishlistService.cs
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;

namespace BookStoreMVC.Services
{
    public interface IWishlistService
    {
        Task<WishlistViewModel> GetWishlistAsync(string userId);
        Task<bool> AddToWishlistAsync(string userId, AddToWishlistViewModel model);
        Task<bool> RemoveFromWishlistAsync(string userId, int bookId);
        Task<bool> ClearWishlistAsync(string userId);
        Task<bool> IsBookInWishlistAsync(string userId, int bookId);
        Task<int> GetWishlistItemCountAsync(string userId);
        Task<bool> MoveToCartAsync(string userId, int bookId, int quantity = 1);
        Task<(bool Success, bool IsInWishlist)> ToggleWishlistItemAsync(string userId, int bookId);
    }

    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<WishlistService> _logger;

        public WishlistService(
            ApplicationDbContext context,
            ICartService cartService,
            ILogger<WishlistService> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<WishlistViewModel> GetWishlistAsync(string userId)
        {
            var wishlistItems = await _context.WishlistItems
                .Include(wi => wi.Book)
                .ThenInclude(b => b.Category)
                .Include(wi => wi.Book.Reviews)
                .Where(wi => wi.UserId == userId)
                .OrderByDescending(wi => wi.CreatedAt)
                .ToListAsync();

            return new WishlistViewModel
            {
                Items = wishlistItems.Select(MapToViewModel).ToList()
            };
        }

        public async Task<bool> AddToWishlistAsync(string userId, AddToWishlistViewModel model)
        {
            var book = await _context.Books.FindAsync(model.BookId);
            if (book == null || !book.IsActive)
                return false;

            var existingItem = await _context.WishlistItems
                .FirstOrDefaultAsync(wi => wi.UserId == userId && wi.BookId == model.BookId);

            if (existingItem != null)
                return true; // Already in wishlist

            var wishlistItem = new WishListItem
            {
                UserId = userId,
                BookId = model.BookId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromWishlistAsync(string userId, int bookId)
        {
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(wi => wi.UserId == userId && wi.BookId == bookId);

            if (wishlistItem == null) return false;

            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearWishlistAsync(string userId)
        {
            var wishlistItems = await _context.WishlistItems
                .Where(wi => wi.UserId == userId)
                .ToListAsync();

            if (!wishlistItems.Any()) return true;

            _context.WishlistItems.RemoveRange(wishlistItems);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsBookInWishlistAsync(string userId, int bookId)
        {
            return await _context.WishlistItems
                .AnyAsync(wi => wi.UserId == userId && wi.BookId == bookId);
        }

        public async Task<int> GetWishlistItemCountAsync(string userId)
        {
            return await _context.WishlistItems
                .CountAsync(wi => wi.UserId == userId);
        }

        public async Task<bool> MoveToCartAsync(string userId, int bookId, int quantity = 1)
        {
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(wi => wi.UserId == userId && wi.BookId == bookId);

            if (wishlistItem == null) return false;

            // Add to cart
            var addToCartModel = new AddToCartViewModel
            {
                BookId = bookId,
                Quantity = quantity
            };

            var addedToCart = await _cartService.AddToCartAsync(userId, addToCartModel);

            if (addedToCart)
            {
                // Remove from wishlist
                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();
            }

            return addedToCart;
        }
 public async Task<(bool Success, bool IsInWishlist)> ToggleWishlistItemAsync(string userId, int bookId)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Where(b => b.Id == bookId)
                .Select(b => new { b.Id, b.IsActive })
                .FirstOrDefaultAsync();

            if (book == null || !book.IsActive)
            {
                return (false, false);
            }

            var existingItem = await _context.WishlistItems
                .FirstOrDefaultAsync(wi => wi.UserId == userId && wi.BookId == bookId);

            if (existingItem != null)
            {
                _context.WishlistItems.Remove(existingItem);
                await _context.SaveChangesAsync();
                return (true, false);
            }

            var wishlistItem = new WishListItem
            {
                UserId = userId,
                BookId = bookId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync();
            return (true, true);
        }
        private WishlistItemViewModel MapToViewModel(WishListItem wishlistItem)
        {
            return new WishlistItemViewModel
            {
                Id = wishlistItem.Id,
                BookId = wishlistItem.BookId,
                CreatedAt = wishlistItem.CreatedAt,
                Book = new BookViewModel
                {
                    Id = wishlistItem.Book.Id,
                    Title = wishlistItem.Book.Title,
                    Author = wishlistItem.Book.Author,
                    Description = wishlistItem.Book.Description,
                    Price = wishlistItem.Book.Price,
                    DiscountPrice = wishlistItem.Book.DiscountPrice,
                    StockQuantity = wishlistItem.Book.StockQuantity,
                    CategoryId = wishlistItem.Book.CategoryId,
                    Category = wishlistItem.Book.Category,
                    Publisher = wishlistItem.Book.Publisher,
                    PublishDate = wishlistItem.Book.PublishDate,
                    Language = wishlistItem.Book.Language,
                    PageCount = wishlistItem.Book.PageCount,
                    ImageUrl = NormalizeImageUrl(wishlistItem.Book.ImageUrl),
                    AdditionalImages = wishlistItem.Book.AdditionalImages,
                    CreatedDate = wishlistItem.Book.CreatedAt,
                    LastUpdatedDate = wishlistItem.Book.UpdatedAt,
                    IsActive = wishlistItem.Book.IsActive,
                    Reviews = wishlistItem.Book.Reviews
                }
            };
        }
         private static string? NormalizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return imageUrl;
            }

            var trimmed = imageUrl.Trim();

            // Ensure we only have a single leading slash
            trimmed = trimmed.TrimStart('~');
            trimmed = trimmed.StartsWith("/") ? trimmed : $"/{trimmed}";

            return trimmed;
        }
    }
}