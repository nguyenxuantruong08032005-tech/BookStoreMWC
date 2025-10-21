// Services/CartService.cs - FIXED VERSION
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;

namespace BookStoreMVC.Services
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync(string userId);
        Task<bool> AddToCartAsync(string userId, AddToCartViewModel model);
        Task<bool> UpdateCartItemAsync(string userId, int bookId, int quantity);
        Task<bool> RemoveFromCartAsync(string userId, int bookId);
        Task<bool> ClearCartAsync(string userId);
        Task<int> GetCartItemCountAsync(string userId);
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartService> _logger;
        private const int MaxQuantityPerItem = 10;

        public CartService(ApplicationDbContext context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CartViewModel> GetCartAsync(string userId)
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.Book)
                    .ThenInclude(b => b.Category)
                .Include(ci => ci.Book.Reviews)
                .Where(ci => ci.UserId == userId)
                .OrderBy(ci => ci.CreatedAt)
                .ToListAsync();

            return new CartViewModel
            {
                Items = cartItems.Select(MapToViewModel).ToList()
            };
        }

        public async Task<bool> AddToCartAsync(string userId, AddToCartViewModel model)
        {
            // Sử dụng transaction để đảm bảo tính toàn vẹn
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("=== ADD TO CART START ===");
                _logger.LogInformation("UserId: {UserId}, BookId: {BookId}, Quantity: {Quantity}",
                    userId, model.BookId, model.Quantity);

                // Validate quantity
                if (model.Quantity <= 0)
                {
                    _logger.LogWarning("INVALID_QUANTITY: {Quantity}", model.Quantity);
                    return false;
                }

                // Load book
                var book = await _context.Books
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == model.BookId);

                if (book == null)
                {
                    _logger.LogWarning("BOOK_NOT_FOUND: BookId {BookId}", model.BookId);
                    return false;
                }

                _logger.LogInformation("Book Found - Title: {Title}, Stock: {Stock}, IsActive: {IsActive}",
                    book.Title, book.StockQuantity, book.IsActive);

                // Check if active
                if (!book.IsActive)
                {
                    _logger.LogWarning("BOOK_INACTIVE: BookId {BookId}", model.BookId);
                    return false;
                }

                // Check stock
                if (book.StockQuantity <= 0)
                {
                    _logger.LogWarning("OUT_OF_STOCK: BookId {BookId}, Stock: {Stock}",
                        model.BookId, book.StockQuantity);
                    return false;
                }

                // Check existing cart item
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.BookId == model.BookId);

                int currentQuantity = existingItem?.Quantity ?? 0;
                int newTotalQuantity = currentQuantity + model.Quantity;

                _logger.LogInformation("Cart Status - Current: {Current}, Adding: {Adding}, New Total: {NewTotal}",
                    currentQuantity, model.Quantity, newTotalQuantity);

                // Validate stock for new total
                if (newTotalQuantity > book.StockQuantity)
                {
                    _logger.LogWarning("INSUFFICIENT_STOCK: {NewTotal} > {Stock}",
                        newTotalQuantity, book.StockQuantity);
                    return false;
                }

                // Validate max quantity limit
                if (newTotalQuantity > MaxQuantityPerItem)
                {
                    _logger.LogWarning("QUANTITY_LIMIT_EXCEEDED: {NewTotal} > {Max}",
                        newTotalQuantity, MaxQuantityPerItem);
                    return false;
                }

                // Update or create cart item
                if (existingItem != null)
                {
                    _logger.LogInformation("Updating existing item from {Old} to {New}",
                        existingItem.Quantity, newTotalQuantity);

                    existingItem.Quantity = newTotalQuantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                    _context.CartItems.Update(existingItem);
                }
                else
                {
                    _logger.LogInformation("Creating new cart item with quantity {Qty}", model.Quantity);

                    var cartItem = new CartItem
                    {
                        UserId = userId,
                        BookId = model.BookId,
                        Quantity = model.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("=== ADD TO CART SUCCESS ===");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "=== ADD TO CART ERROR ===");
                return false;
            }
        }

        public async Task<bool> UpdateCartItemAsync(string userId, int bookId, int quantity)
        {
            try
            {
                _logger.LogInformation("UpdateCartItem - UserId: {UserId}, BookId: {BookId}, Quantity: {Quantity}",
                    userId, bookId, quantity);

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Book)
                    .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.BookId == bookId);

                if (cartItem == null)
                {
                    _logger.LogWarning("Cart item not found");
                    return false;
                }

                if (quantity <= 0)
                {
                    _logger.LogInformation("Quantity <= 0, removing item");
                    return await RemoveFromCartAsync(userId, bookId);
                }

                // Validate stock
                if (quantity > cartItem.Book.StockQuantity)
                {
                    _logger.LogWarning("Insufficient stock: {Quantity} > {Stock}",
                        quantity, cartItem.Book.StockQuantity);
                    return false;
                }

                // Validate limit
                if (quantity > MaxQuantityPerItem)
                {
                    _logger.LogWarning("Quantity exceeds limit: {Quantity} > {Max}",
                        quantity, MaxQuantityPerItem);
                    return false;
                }

                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cart item updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item");
                return false;
            }
        }

        public async Task<bool> RemoveFromCartAsync(string userId, int bookId)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.BookId == bookId);

                if (cartItem == null) return false;

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Removed cart item: UserId {UserId}, BookId {BookId}", userId, bookId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item");
                return false;
            }
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            try
            {
                var cartItems = await _context.CartItems
                    .Where(ci => ci.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any()) return true;

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleared cart for UserId {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return false;
            }
        }

        public async Task<int> GetCartItemCountAsync(string userId)
        {
            return await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .SumAsync(ci => ci.Quantity);
        }

        private CartItemViewModel MapToViewModel(CartItem cartItem)
        {
            return new CartItemViewModel
            {
                Id = cartItem.Id,
                BookId = cartItem.BookId,
                Quantity = cartItem.Quantity,
                CreatedAt = cartItem.CreatedAt,
                BookTitle = cartItem.Book.Title,
                BookAuthor = cartItem.Book.Author,
                BookImage = cartItem.Book.ImageUrl,
                Book = new BookViewModel
                {
                    Id = cartItem.Book.Id,
                    Title = cartItem.Book.Title,
                    Author = cartItem.Book.Author,
                    ImageUrl = cartItem.Book.ImageUrl,
                    Price = cartItem.Book.Price,
                    DiscountPrice = cartItem.Book.DiscountPrice,
                    StockQuantity = cartItem.Book.StockQuantity,
                    IsActive = cartItem.Book.IsActive
                }
            };
        }
    }
}