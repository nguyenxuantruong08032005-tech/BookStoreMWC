// Controllers/OrdersController.cs - FIXED VERSION
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;

namespace BookStoreMVC.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            ICartService cartService,
            UserManager<User> userManager,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
        }

        // FIX: Better error handling for Index action
        [HttpGet("")]
        [HttpGet("Index")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var orders = await _orderService.GetUserOrdersAsync(userId);

                // Tạo OrderListViewModel thay vì trả về trực tiếp IEnumerable
                var model = new OrderListViewModel
                {
                    Orders = orders,
                    TotalCount = orders.Count()
                };

                ViewBag.PageTitle = "Đơn hàng của tôi";
                return View(model);  // Trả về OrderListViewModel
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                // Trả về model rỗng thay vì null
                return View(new OrderListViewModel());
            }
        }

        // FIX: Better error handling for Details action
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID is null when accessing order details");
                    return RedirectToAction("Login", "Account");
                }

                var order = await _orderService.GetOrderByIdAsync(id, userId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for user {UserId}", id, userId);
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                    return RedirectToAction(nameof(Index));
                }

                // Security check: ensure order belongs to user
                if (order.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to another user",
                        userId, id);
                    TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.PageTitle = $"Đơn hàng #{order.OrderNumber}";
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for order {OrderId}", id);
                TempData["ErrorMessage"] = "Không thể tải chi tiết đơn hàng. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("Checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var cart = await _cartService.GetCartAsync(userId);
                if (cart.IsEmpty)
                {
                    TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                var user = await _userManager.GetUserAsync(User);

                var model = new OrderCreateViewModel
                {
                    Cart = cart,
                    ShippingFirstName = user?.Name?.Split(' ').FirstOrDefault() ?? "",
                    ShippingLastName = user?.Name?.Split(' ').LastOrDefault() ?? "",
                    ShippingPhone = user?.PhoneNumber ?? "",
                    AvailablePaymentMethods = new[] { "Credit Card", "PayPal", "Bank Transfer", "Cash on Delivery" },
                    AvailableCountries = new[] { "Vietnam", "United States", "United Kingdom", "Canada", "Australia" }
                };

                ViewBag.PageTitle = "Checkout";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["ErrorMessage"] = "Không thể tải trang thanh toán.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost("Checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(OrderCreateViewModel model)
        {
            var currentUserId = _userManager.GetUserId(User)!;

            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateCheckoutViewModelAsync(model, currentUserId);

                    if (IsAjaxRequest())
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Vui lòng kiểm tra lại thông tin đã nhập.",
                            errors = ModelState
                                .Where(entry => entry.Value?.Errors?.Count > 0)
                                .ToDictionary(
                                    entry => entry.Key,
                                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage))
                        });
                    }

                    return View(model);
                }

                var order = await _orderService.CreateOrderAsync(currentUserId, model);

                if (order != null)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            orderId = order.Id,
                            redirectUrl = Url.Action(nameof(Details), new { id = order.Id })
                        });
                    }

                    TempData["SuccessMessage"] = $"Đơn hàng #{order.OrderNumber} đã được tạo thành công!";
                    return RedirectToAction(nameof(Details), new { id = order.Id });
                }

                ModelState.AddModelError(string.Empty, "Không thể tạo đơn hàng. Vui lòng thử lại.");
                await PopulateCheckoutViewModelAsync(model, currentUserId);

                if (IsAjaxRequest())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        success = false,
                        message = "Không thể tạo đơn hàng. Vui lòng thử lại."
                    });
                }

                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating order");
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateCheckoutViewModelAsync(model, currentUserId);

                if (IsAjaxRequest())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo đơn hàng. Vui lòng thử lại sau.");
                await PopulateCheckoutViewModelAsync(model, currentUserId);

                if (IsAjaxRequest())
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        success = false,
                        message = "Đã xảy ra lỗi khi tạo đơn hàng. Vui lòng thử lại sau."
                    });
                }

                return View(model);
            }
        }

        private async Task PopulateCheckoutViewModelAsync(OrderCreateViewModel model, string userId)
        {
            model.Cart = await _cartService.GetCartAsync(userId);
            model.AvailablePaymentMethods = new[] { "Credit Card", "PayPal", "Bank Transfer", "Cash on Delivery" };
            model.AvailableCountries = new[] { "Vietnam", "United States", "United Kingdom", "Canada", "Australia" };
        }

        private bool IsAjaxRequest()
        {
            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
                requestedWith.Any(value => string.Equals(value, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (Request.Headers.TryGetValue("Accept", out var acceptHeaders) &&
                acceptHeaders.Any(value => value.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        [HttpPost("Cancel/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });
                }

                var success = await _orderService.CancelOrderAsync(id, userId);

                if (success)
                {
                    _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId);
                    return Json(new { success = true, message = "Đơn hàng đã được hủy thành công." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng này. Đơn hàng có thể đã được xử lý." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi hủy đơn hàng." });
            }
        }

        // Trong file Controllers/OrdersController.cs
        // Tìm method Reorder và thay thế bằng code này:

        [HttpPost("Reorder/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });
                }

                var order = await _orderService.GetOrderByIdAsync(id, userId);
                if (order == null || order.UserId != userId)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                }

                // Add all items from the order to cart
                foreach (var item in order.OrderItems)
                {
                    var addToCartModel = new AddToCartViewModel
                    {
                        BookId = item.BookId,
                        Quantity = item.Quantity
                    };
                    await _cartService.AddToCartAsync(userId, addToCartModel);
                }

                var cart = await _cartService.GetCartAsync(userId);
                return Json(new { success = true, cartItemCount = cart.ItemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering items from order {OrderId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi thêm sản phẩm vào giỏ hàng." });
            }
        }
    }
}
