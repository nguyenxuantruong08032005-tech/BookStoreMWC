// Services/SessionCartService.cs - FIXED VERSION
using BookStoreMVC.Models.ViewModels;
using System.Text.Json;

namespace BookStoreMVC.Services
{
    public interface ISessionCartService
    {
        CartViewModel GetCart();
        void AddToCart(int bookId, int quantity);
        void UpdateCartItem(int bookId, int quantity);
        void RemoveFromCart(int bookId);
        void ClearCart();
        int GetCartItemCount();
    }

    public class SessionCartService : ISessionCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBookService _bookService;
        private readonly ILogger<SessionCartService> _logger;
        private const string CartSessionKey = "GuestCart";
        private const int MaxQuantityPerItem = 10;

        public SessionCartService(
            IHttpContextAccessor httpContextAccessor,
            IBookService bookService,
            ILogger<SessionCartService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _bookService = bookService;
            _logger = logger;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public CartViewModel GetCart()
        {
            var cartJson = Session.GetString(CartSessionKey);

            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartViewModel();
            }

            try
            {
                var cart = JsonSerializer.Deserialize<CartViewModel>(cartJson);
                return cart ?? new CartViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing cart from session");
                return new CartViewModel();
            }
        }

        public void AddToCart(int bookId, int quantity)
        {
            try
            {
                _logger.LogInformation("SessionCart - AddToCart: BookId {BookId}, Quantity {Quantity}",
                    bookId, quantity);

                // Validate quantity
                if (quantity <= 0)
                {
                    _logger.LogWarning("Invalid quantity: {Quantity}", quantity);
                    throw new ArgumentException("Số lượng phải lớn hơn 0");
                }

                // Load book với validation
                var book = _bookService.GetBookByIdAsync(bookId).Result;

                if (book == null)
                {
                    _logger.LogWarning("Book not found: {BookId}", bookId);
                    throw new InvalidOperationException("Sản phẩm không tồn tại");
                }

                if (!book.IsActive)
                {
                    _logger.LogWarning("Book is not active: {BookId}", bookId);
                    throw new InvalidOperationException("Sản phẩm hiện không còn bán");
                }

                if (book.StockQuantity <= 0)
                {
                    _logger.LogWarning("Book out of stock: {BookId}, Stock: {Stock}",
                        bookId, book.StockQuantity);
                    throw new InvalidOperationException("Sản phẩm đã hết hàng");
                }

                var cart = GetCart();
                var existingItem = cart.Items.FirstOrDefault(i => i.BookId == bookId);

                int currentQuantity = existingItem?.Quantity ?? 0;
                int newTotalQuantity = currentQuantity + quantity;

                // Kiểm tra tồn kho
                if (newTotalQuantity > book.StockQuantity)
                {
                    _logger.LogWarning("Insufficient stock: {NewTotal} > {Stock}",
                        newTotalQuantity, book.StockQuantity);

                    string message = currentQuantity > 0
                        ? $"Kho chỉ còn {book.StockQuantity} sản phẩm. Bạn đã có {currentQuantity} trong giỏ hàng."
                        : $"Kho chỉ còn {book.StockQuantity} sản phẩm.";

                    throw new InvalidOperationException(message);
                }

                // Kiểm tra giới hạn
                if (newTotalQuantity > MaxQuantityPerItem)
                {
                    _logger.LogWarning("Quantity limit exceeded: {NewTotal} > {Max}",
                        newTotalQuantity, MaxQuantityPerItem);

                    string message = currentQuantity > 0
                        ? $"Bạn chỉ có thể mua tối đa {MaxQuantityPerItem} sản phẩm này. Bạn đã có {currentQuantity} trong giỏ hàng."
                        : $"Bạn chỉ có thể mua tối đa {MaxQuantityPerItem} sản phẩm này.";

                    throw new InvalidOperationException(message);
                }

                if (existingItem != null)
                {
                    existingItem.Quantity = newTotalQuantity;
                    _logger.LogInformation("Updated existing item to quantity: {Quantity}", newTotalQuantity);
                }
                else
                {
                    cart.Items.Add(new CartItemViewModel
                    {
                        Id = null, // Session cart không có ID
                        BookId = book.Id,
                        BookTitle = book.Title,
                        BookAuthor = book.Author,
                        BookImage = book.ImageUrl,
                        Price = book.DisplayPrice,
                        Quantity = quantity,
                        CreatedAt = DateTime.UtcNow,
                        Book = book
                    });
                    _logger.LogInformation("Added new item with quantity: {Quantity}", quantity);
                }

                SaveCart(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to session cart");
                throw; // Re-throw để Controller xử lý
            }
        }

        public void UpdateCartItem(int bookId, int quantity)
        {
            try
            {
                _logger.LogInformation("SessionCart - UpdateCartItem: BookId {BookId}, Quantity {Quantity}",
                    bookId, quantity);

                var cart = GetCart();
                var item = cart.Items.FirstOrDefault(i => i.BookId == bookId);

                if (item == null)
                {
                    _logger.LogWarning("Cart item not found");
                    return;
                }

                if (quantity <= 0)
                {
                    RemoveFromCart(bookId);
                    return;
                }

                // Validate với database
                var book = _bookService.GetBookByIdAsync(bookId).Result;

                if (book == null || !book.IsActive)
                {
                    _logger.LogWarning("Book not available");
                    throw new InvalidOperationException("Sản phẩm không còn khả dụng");
                }

                if (quantity > book.StockQuantity)
                {
                    _logger.LogWarning("Quantity exceeds stock: {Quantity} > {Stock}",
                        quantity, book.StockQuantity);
                    throw new InvalidOperationException($"Kho chỉ còn {book.StockQuantity} sản phẩm");
                }

                if (quantity > MaxQuantityPerItem)
                {
                    _logger.LogWarning("Quantity exceeds limit: {Quantity} > {Max}",
                        quantity, MaxQuantityPerItem);
                    throw new InvalidOperationException($"Bạn chỉ có thể mua tối đa {MaxQuantityPerItem} sản phẩm này");
                }

                item.Quantity = quantity;
                SaveCart(cart);

                _logger.LogInformation("Cart item updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session cart item");
                throw;
            }
        }

        public void RemoveFromCart(int bookId)
        {
            try
            {
                var cart = GetCart();
                var item = cart.Items.FirstOrDefault(i => i.BookId == bookId);

                if (item != null)
                {
                    cart.Items.Remove(item);
                    SaveCart(cart);
                    _logger.LogInformation("Removed item from cart: BookId {BookId}", bookId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from session cart");
            }
        }

        public void ClearCart()
        {
            try
            {
                Session.Remove(CartSessionKey);
                _logger.LogInformation("Session cart cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing session cart");
            }
        }

        public int GetCartItemCount()
        {
            var cart = GetCart();
            return cart.ItemCount;
        }

        private void SaveCart(CartViewModel cart)
        {
            try
            {
                var cartJson = JsonSerializer.Serialize(cart);
                Session.SetString(CartSessionKey, cartJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cart to session");
                throw;
            }
        }

        // Phương thức để chuyển giỏ hàng session sang database khi user đăng nhập
        public List<CartItemViewModel> GetCartItemsForMigration()
        {
            var cart = GetCart();
            return cart.Items.ToList();
        }
    }
}