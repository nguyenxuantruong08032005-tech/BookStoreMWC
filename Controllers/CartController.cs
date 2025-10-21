// Controllers/CartController.cs - FIXED VERSION
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;

namespace BookStoreMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ISessionCartService _sessionCartService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            ISessionCartService sessionCartService,
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _sessionCartService = sessionCartService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: Cart/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                CartViewModel cart;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User)!;
                    cart = await _cartService.GetCartAsync(userId);
                }
                else
                {
                    cart = _sessionCartService.GetCart();
                }

                ViewBag.PageTitle = "Shopping Cart";
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải giỏ hàng";
                return View(new CartViewModel());
            }
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartViewModel model)
        {
            try
            {
                _logger.LogInformation("AddToCart request - BookId: {BookId}, Quantity: {Quantity}",
                    model.BookId, model.Quantity);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state");
                    return Json(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ"
                    });
                }

                // Validate book trước (cho cả User và Guest)
                var book = await _context.Books
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == model.BookId);

                if (book == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm không tồn tại",
                        errorCode = "BOOK_NOT_FOUND"
                    });
                }

                if (!book.IsActive)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm hiện không còn bán",
                        errorCode = "BOOK_INACTIVE"
                    });
                }

                if (book.StockQuantity <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm đã hết hàng",
                        errorCode = "OUT_OF_STOCK",
                        availableStock = 0
                    });
                }

                if (model.Quantity > book.StockQuantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Kho chỉ còn {book.StockQuantity} sản phẩm",
                        errorCode = "INSUFFICIENT_STOCK",
                        availableStock = book.StockQuantity
                    });
                }

                bool success;
                int itemCount;

                if (User.Identity?.IsAuthenticated == true)
                {
                    // ==== USER ĐÃ ĐĂNG NHẬP ====
                    var userId = _userManager.GetUserId(User)!;
                    success = await _cartService.AddToCartAsync(userId, model);
                    itemCount = await _cartService.GetCartItemCountAsync(userId);

                    if (success)
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Đã thêm vào giỏ hàng!",
                            cartItemCount = itemCount
                        });
                    }
                    else
                    {
                        // Kiểm tra xem có phải lỗi vượt quá giới hạn không
                        var existingItem = await _context.CartItems
                            .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.BookId == model.BookId);

                        if (existingItem != null)
                        {
                            int newTotal = existingItem.Quantity + model.Quantity;
                            if (newTotal > book.StockQuantity)
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Kho chỉ còn {book.StockQuantity} sản phẩm. Bạn đã có {existingItem.Quantity} trong giỏ hàng.",
                                    errorCode = "INSUFFICIENT_STOCK",
                                    availableStock = book.StockQuantity,
                                    currentCartQuantity = existingItem.Quantity
                                });
                            }
                            else if (newTotal > 10)
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Bạn chỉ có thể mua tối đa 10 sản phẩm này. Bạn đã có {existingItem.Quantity} trong giỏ hàng.",
                                    errorCode = "QUANTITY_LIMIT_EXCEEDED",
                                    currentCartQuantity = existingItem.Quantity
                                });
                            }
                        }

                        return Json(new
                        {
                            success = false,
                            message = "Không thể thêm sản phẩm. Vui lòng kiểm tra tồn kho."
                        });
                    }
                }
                else
                {
                    // ==== GUEST (CHƯA ĐĂNG NHẬP) ====
                    try
                    {
                        _sessionCartService.AddToCart(model.BookId, model.Quantity);
                        itemCount = _sessionCartService.GetCartItemCount();

                        return Json(new
                        {
                            success = true,
                            message = "Đã thêm vào giỏ hàng!",
                            cartItemCount = itemCount
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        // SessionCartService throw InvalidOperationException với message chi tiết
                        return Json(new
                        {
                            success = false,
                            message = ex.Message
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddToCart");
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau."
                });
            }
        }

        // POST: Cart/UpdateCartItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemViewModel model)
        {
            try
            {
                bool success;
                CartViewModel cart;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User)!;
                    success = await _cartService.UpdateCartItemAsync(userId, model.BookId, model.Quantity);
                    cart = await _cartService.GetCartAsync(userId);
                }
                else
                {
                    try
                    {
                        _sessionCartService.UpdateCartItem(model.BookId, model.Quantity);
                        cart = _sessionCartService.GetCart();
                        success = true;
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Json(new
                        {
                            success = false,
                            message = ex.Message
                        });
                    }
                }

                if (success)
                {
                    var item = cart.Items.FirstOrDefault(i => i.BookId == model.BookId);
                    var itemTotal = item != null ? (item.Price * item.Quantity).ToString("C0") : "0 ₫";

                    return Json(new
                    {
                        success = true,
                        message = "Đã cập nhật giỏ hàng!",
                        itemTotal = itemTotal,
                        cart = new
                        {
                            subTotal = cart.SubTotal.ToString("C0"),
                            tax = cart.Tax.ToString("C0"),
                            shippingCost = cart.ShippingCost.ToString("C0"),
                            total = cart.Total.ToString("C0"),
                            itemCount = cart.ItemCount
                        }
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể cập nhật số lượng. Vui lòng kiểm tra tồn kho."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi cập nhật giỏ hàng."
                });
            }
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartViewModel model)
        {
            try
            {
                bool success;
                CartViewModel cart;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User)!;
                    success = await _cartService.RemoveFromCartAsync(userId, model.BookId);
                    cart = await _cartService.GetCartAsync(userId);
                }
                else
                {
                    _sessionCartService.RemoveFromCart(model.BookId);
                    cart = _sessionCartService.GetCart();
                    success = true;
                }

                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Đã xóa khỏi giỏ hàng!",
                        cart = new
                        {
                            subTotal = cart.SubTotal.ToString("C0"),
                            tax = cart.Tax.ToString("C0"),
                            shippingCost = cart.ShippingCost.ToString("C0"),
                            total = cart.Total.ToString("C0"),
                            itemCount = cart.ItemCount
                        }
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa sản phẩm khỏi giỏ hàng."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xóa sản phẩm."
                });
            }
        }

        // POST: Cart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                bool success;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User)!;
                    success = await _cartService.ClearCartAsync(userId);
                }
                else
                {
                    _sessionCartService.ClearCart();
                    success = true;
                }

                if (success)
                {
                    return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa giỏ hàng." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xóa giỏ hàng."
                });
            }
        }

        // GET: Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                int count;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    count = await _cartService.GetCartItemCountAsync(userId!);
                }
                else
                {
                    count = _sessionCartService.GetCartItemCount();
                }

                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }

        // POST: Cart/MigrateGuestCart
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> MigrateGuestCart()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var sessionCart = _sessionCartService.GetCart();

                if (sessionCart.Items.Any())
                {
                    foreach (var item in sessionCart.Items)
                    {
                        await _cartService.AddToCartAsync(userId, new AddToCartViewModel
                        {
                            BookId = item.BookId,
                            Quantity = item.Quantity
                        });
                    }

                    _sessionCartService.ClearCart();
                    TempData["SuccessMessage"] = "Giỏ hàng của bạn đã được cập nhật!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating guest cart");
                TempData["ErrorMessage"] = "Có lỗi khi chuyển giỏ hàng";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}