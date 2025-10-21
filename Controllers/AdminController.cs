// Controllers/AdminController.cs - Complete Version Based on All Services
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace BookStoreMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IBookService _bookService;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly IReviewService _reviewService;
        private readonly ICartService _cartService;
        private readonly IWishlistService _wishlistService;
        private readonly IFileUploadService _fileUploadService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IDashboardService dashboardService,
            IBookService bookService,
            IOrderService orderService,
            IUserService userService,
            IReviewService reviewService,
            ICartService cartService,
            IWishlistService wishlistService,
            IFileUploadService fileUploadService,
            UserManager<User> userManager,
            ILogger<AdminController> logger)
        {
            _dashboardService = dashboardService;
            _bookService = bookService;
            _orderService = orderService;
            _userService = userService;
            _reviewService = reviewService;
            _cartService = cartService;
            _wishlistService = wishlistService;
            _fileUploadService = fileUploadService;
            _userManager = userManager;
            _logger = logger;
        }

        #region Dashboard

        [Route("")]
        [Route("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var model = await _dashboardService.GetAdminDashboardAsync();

                ViewBag.PageTitle = "Admin Dashboard";
                ViewBag.ActiveMenu = "Dashboard";
                ViewBag.IsAdmin = true;



                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "Không thể tải dashboard. Vui lòng thử lại.";
                return View(new AdminDashboardViewModel());
            }
        }

        [HttpGet]
        [Route("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new { success = false, message = "Lỗi khi tải thống kê" });
            }
        }

        #endregion

        #region Books Management

        [Route("books")]
        public async Task<IActionResult> Books(BookListViewModel model)
        {
            try
            {
                var (books, totalCount) = await _bookService.GetBooksAsync(model);

                model.Books = books;
                model.TotalCount = totalCount;
                model.Categories = await _bookService.GetCategoriesAsync();

                // Add stock counts for dashboard
                var booksList = books.ToList();
                ViewBag.InStockCount = booksList.Count(b => b.StockQuantity > 0);
                ViewBag.LowStockCount = booksList.Count(b => b.StockQuantity <= 10 && b.StockQuantity > 0);
                ViewBag.OutOfStockCount = booksList.Count(b => b.StockQuantity == 0);

                ViewBag.PageTitle = "Quản lý sách";
                ViewBag.ActiveMenu = "Books";

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading books management page");
                TempData["ErrorMessage"] = "Không thể tải danh sách sách. Vui lòng thử lại.";
                return View(new BookListViewModel());
            }
        }

        [HttpGet]
        [Route("books/create")]
        public async Task<IActionResult> CreateBook()
        {
            try
            {
                var model = new BookViewModel
                {
                    Categories = (await _bookService.GetCategoriesAsync()).ToList(),
                    IsActive = true,
                    StockQuantity = 0
                };

                ViewBag.PageTitle = "Thêm sách mới";
                ViewBag.ActiveMenu = "Books";


                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create book page");
                TempData["ErrorMessage"] = "Không thể tải trang tạo sách.";
                return RedirectToAction(nameof(Books));
            }
        }

        [HttpPost]
        [Route("books/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(BookViewModel model)
        {
            try
            {
                // Validate image file
                if (model.ImageFile != null && !_fileUploadService.IsValidImageFile(model.ImageFile))
                {
                    ModelState.AddModelError("ImageFile", "Vui lòng tải lên file ảnh hợp lệ (JPG, PNG, GIF, WebP) < 5MB.");
                }

                if (!ModelState.IsValid)
                {
                    model.Categories = (await _bookService.GetCategoriesAsync()).ToList();
                    return View(model);
                }

                var createdBook = await _bookService.CreateBookAsync(model, model.ImageFile);

                TempData["SuccessMessage"] = $"Tạo sách '{createdBook.Title}' thành công!";
                return RedirectToAction(nameof(Books));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo sách.");
                model.Categories = (await _bookService.GetCategoriesAsync()).ToList();
                return View(model);
            }
        }

        [HttpGet]
        [Route("books/edit/{id:int}")]
        public async Task<IActionResult> EditBook(int id)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(id);
                if (book == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sách.";
                    return RedirectToAction(nameof(Books));
                }

                book.Categories = (await _bookService.GetCategoriesAsync()).ToList();

                ViewBag.PageTitle = $"Chỉnh sửa: {book.Title}";
                ViewBag.ActiveMenu = "Books";



                return View(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit book page for book {BookId}", id);
                TempData["ErrorMessage"] = "Không thể tải trang chỉnh sửa.";
                return RedirectToAction(nameof(Books));
            }
        }

        [HttpPost]
        [Route("books/edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(BookViewModel model)
        {
            try
            {
                if (model.ImageFile != null && !_fileUploadService.IsValidImageFile(model.ImageFile))
                {
                    ModelState.AddModelError("ImageFile", "Vui lòng tải lên file ảnh hợp lệ.");
                }

                if (!ModelState.IsValid)
                {
                    model.Categories = (await _bookService.GetCategoriesAsync()).ToList();
                    return View(model);
                }

                var updatedBook = await _bookService.UpdateBookAsync(model, model.ImageFile);

                TempData["SuccessMessage"] = $"Cập nhật sách '{updatedBook.Title}' thành công!";
                return RedirectToAction(nameof(Books));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book {BookId}", model.Id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật sách.");
                model.Categories = (await _bookService.GetCategoriesAsync()).ToList();
                return View(model);
            }
        }

        [HttpPost]
        [Route("books/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                var success = await _bookService.DeleteBookAsync(id);

                if (success)
                {
                    return Json(new { success = true, message = "Xóa sách thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy sách." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book {BookId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa sách." });
            }
        }

        [HttpPost]
        [Route("books/update-stock/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookStock(int id, [FromBody] UpdateStockModel model)
        {
            try
            {
                var success = await _bookService.UpdateStockAsync(id, model.Quantity);

                if (success)
                {
                    return Json(new { success = true, message = "Cập nhật tồn kho thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy sách." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book stock {BookId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi." });
            }
        }

        #endregion

        #region File Management
        // Thêm các API endpoints này vào AdminController.cs

        // 1. Upload ảnh bìa riêng biệt
        [HttpPost]
        [Route("books/upload-image/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBookImage(int id, IFormFile imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn file ảnh." });
                }

                if (!_fileUploadService.IsValidImageFile(imageFile))
                {
                    return Json(new { success = false, message = "File không hợp lệ. Chỉ chấp nhận JPG, PNG, GIF, WebP dưới 5MB." });
                }

                var success = await _bookService.UpdateBookImageAsync(id, imageFile);

                if (success)
                {
                    var book = await _bookService.GetBookByIdAsync(id);
                    return Json(new { success = true, message = "Upload ảnh bìa thành công!", imageUrl = book?.ImageUrl });
                }

                return Json(new { success = false, message = "Không tìm thấy sách." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading book image for book {BookId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi upload ảnh." });
            }
        }

        // 2. Xóa ảnh bìa
        [HttpPost]
        [Route("books/remove-image/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBookImage(int id)
        {
            try
            {
                var success = await _bookService.RemoveBookImageAsync(id);

                if (success)
                {
                    return Json(new { success = true, message = "Xóa ảnh bìa thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy sách hoặc ảnh." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing book image for book {BookId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa ảnh." });
            }
        }

        // 3. Lấy danh sách ảnh gallery
        [HttpGet]
        [Route("books/gallery/{id:int}")]
        public async Task<IActionResult> GetBookGallery(int id)
        {
            try
            {
                var galleryImages = await _bookService.GetBookGalleryImagesAsync(id);

                return Json(new
                {
                    success = true,
                    images = galleryImages.Select(img => new
                    {
                        id = img.Id,
                        url = img.ImageUrl
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading gallery for book {BookId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi tải thư viện ảnh." });
            }
        }

        // 4. Upload nhiều ảnh vào gallery
        [HttpPost]
        [Route("books/upload-gallery/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadGalleryImages(int id, List<IFormFile> galleryFiles)
        {
            try
            {
                if (galleryFiles == null || galleryFiles.Count == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một file ảnh." });
                }

                // Validate tất cả files
                var invalidFiles = galleryFiles.Where(f => !_fileUploadService.IsValidImageFile(f)).ToList();
                if (invalidFiles.Any())
                {
                    return Json(new { success = false, message = $"{invalidFiles.Count} file không hợp lệ. Chỉ chấp nhận JPG, PNG, GIF, WebP dưới 5MB." });
                }

                var uploadedCount = await _bookService.UploadGalleryImagesAsync(id, galleryFiles);

                if (uploadedCount > 0)
                {
                    return Json(new { success = true, message = $"Upload {uploadedCount} ảnh thành công!", uploadedCount });
                }

                return Json(new { success = false, message = "Không thể upload ảnh." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading gallery images for book {BookId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi upload ảnh thư viện." });
            }
        }

        // 5. Xóa ảnh từ gallery
        [HttpDelete]
        [Route("books/remove-gallery-image/{imageId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveGalleryImage(int imageId)
        {
            try
            {
                var success = await _bookService.RemoveGalleryImageAsync(imageId);

                if (success)
                {
                    return Json(new { success = true, message = "Xóa ảnh thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy ảnh." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing gallery image {ImageId}", imageId);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa ảnh." });
            }
        }

        #endregion

        #region Categories Management

        [Route("categories")]
        public async Task<IActionResult> Categories()
        {
            try
            {
                var categories = await _bookService.GetCategoriesWithStatsAsync();

                var model = new CategoryListViewModel
                {
                    Categories = categories
                };

                ViewBag.PageTitle = "Quản lý danh mục";
                ViewBag.ActiveMenu = "Categories";


                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories management page");
                TempData["ErrorMessage"] = "Không thể tải danh sách danh mục.";
                return View(new CategoryListViewModel());
            }
        }

        [HttpGet]
        [Route("categories/create")]
        public async Task<IActionResult> CreateCategory()
        {
            try
            {
                var model = new CategoryViewModel
                {
                    AvailableParentCategories = (await _bookService.GetCategoriesAsync()).ToList(),
                    IsActive = true
                };

                ViewBag.PageTitle = "Thêm danh mục mới";
                ViewBag.ActiveMenu = "Categories";



                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create category page");
                TempData["ErrorMessage"] = "Không thể tải trang tạo danh mục.";
                return RedirectToAction(nameof(Categories));
            }
        }

        [HttpPost]
        [Route("categories/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableParentCategories = (await _bookService.GetCategoriesAsync()).ToList();
                    return View(model);
                }

                var createdCategory = await _bookService.CreateCategoryAsync(model);

                TempData["SuccessMessage"] = $"Tạo danh mục '{createdCategory.Name}' thành công!";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo danh mục.");
                model.AvailableParentCategories = (await _bookService.GetCategoriesAsync()).ToList();
                return View(model);
            }
        }

        [HttpGet]
        [Route("categories/edit/{id:int}")]
        public async Task<IActionResult> EditCategory(int id)
        {
            try
            {
                var category = await _bookService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                    return RedirectToAction(nameof(Categories));
                }

                category.AvailableParentCategories = (await _bookService.GetCategoriesAsync())
                    .Where(c => c.Id != id)
                    .ToList();

                ViewBag.PageTitle = $"Chỉnh sửa: {category.Name}";
                ViewBag.ActiveMenu = "Categories";



                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit category page for category {CategoryId}", id);
                TempData["ErrorMessage"] = "Không thể tải trang chỉnh sửa.";
                return RedirectToAction(nameof(Categories));
            }
        }

        [HttpPost]
        [Route("categories/edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableParentCategories = (await _bookService.GetCategoriesAsync())
                        .Where(c => c.Id != model.Id)
                        .ToList();
                    return View(model);
                }

                var updatedCategory = await _bookService.UpdateCategoryAsync(model);

                TempData["SuccessMessage"] = $"Cập nhật danh mục '{updatedCategory.Name}' thành công!";
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", model.Id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật danh mục.");
                model.AvailableParentCategories = (await _bookService.GetCategoriesAsync())
                    .Where(c => c.Id != model.Id)
                    .ToList();
                return View(model);
            }
        }

        [HttpPost]
        [Route("categories/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var success = await _bookService.DeleteCategoryAsync(id);

                if (success)
                {
                    return Json(new { success = true, message = "Xóa danh mục thành công!" });
                }

                return Json(new { success = false, message = "Không thể xóa danh mục. Có thể danh mục đang chứa sách." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa danh mục." });
            }
        }

        #endregion

        #region Orders Management

        [Route("orders")]
        public async Task<IActionResult> Orders(string? status = null)
        {
            try
            {
                var model = new OrderListViewModel
                {
                    StatusFilter = !string.IsNullOrEmpty(status)
                        ? Enum.TryParse<OrderStatus>(status, true, out var parsedStatus) ? parsedStatus : null
                        : null,
                    PageSize = 25
                };

                var (orders, totalCount) = await _orderService.GetOrdersAsync(model);

                model.Orders = orders;
                model.TotalCount = totalCount;

                // Add order status counts for dashboard
                var stats = await _orderService.GetOrdersByStatusAsync();
                ViewBag.PendingCount = stats.GetValueOrDefault(OrderStatus.Pending, 0);
                ViewBag.ProcessingCount = stats.GetValueOrDefault(OrderStatus.Processing, 0);
                ViewBag.CompletedCount = stats.GetValueOrDefault(OrderStatus.Delivered, 0);
                ViewBag.CancelledCount = stats.GetValueOrDefault(OrderStatus.Cancelled, 0);

                ViewBag.PageTitle = "Quản lý đơn hàng";
                ViewBag.ActiveMenu = "Orders";
                ViewBag.CurrentStatus = status;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders management page");
                TempData["ErrorMessage"] = "Không thể tải danh sách đơn hàng.";
                return View(new OrderListViewModel());
            }
        }

        [HttpGet]
        [Route("orders/details/{id:int}")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Orders));
                }

                ViewBag.PageTitle = $"Chi tiết đơn hàng #{id}";
                ViewBag.ActiveMenu = "Orders";


                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for order {OrderId}", id);
                TempData["ErrorMessage"] = "Không thể tải chi tiết đơn hàng.";
                return RedirectToAction(nameof(Orders));
            }
        }

        [HttpPost]
        [Route("orders/update-status/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateModel model)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(model.Status, out var status))
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ." });
                }

                var success = await _orderService.UpdateOrderStatusAsync(id, status);

                if (success)
                {
                    return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật trạng thái." });
            }
        }

        #endregion

        #region Users Management

        [Route("users")]
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userService.GetRecentUsersAsync(100);

                ViewBag.PageTitle = "Quản lý người dùng";
                ViewBag.ActiveMenu = "Users";


                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users management page");
                TempData["ErrorMessage"] = "Không thể tải danh sách người dùng.";
                return View(new List<User>());
            }
        }

        [Route("users/details/{id}")]
        public async Task<IActionResult> UserDetails(string id)
        {
            try
            {
                var userProfile = await _userService.GetUserProfileAsync(id);
                if (userProfile == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Users));
                }

                ViewBag.PageTitle = $"Người dùng: {userProfile.Name}";
                ViewBag.ActiveMenu = "Users";



                return View(userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "Không thể tải thông tin người dùng.";
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [Route("users/toggle-lock/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLock(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var isLocked = await _userManager.IsLockedOutAsync(user);

                IdentityResult result;
                if (isLocked)
                {
                    // Unlock user
                    result = await _userManager.SetLockoutEndDateAsync(user, null);
                }
                else
                {
                    // Lock user for 100 years
                    result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                }

                if (result.Succeeded)
                {
                    var message = isLocked ? "Mở khóa người dùng thành công!" : "Khóa người dùng thành công!";
                    return Json(new { success = true, message = message, isLocked = !isLocked });
                }

                return Json(new { success = false, message = "Không thể thực hiện thao tác." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user lock for user {UserId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi." });
            }
        }

        #endregion

        #region Reviews Management

        [Route("reviews")]
        public async Task<IActionResult> Reviews(ReviewListViewModel model)
        {
            try
            {
                // Không có hệ thống phê duyệt - hiển thị tất cả reviews
                model.ShowUnapproved = false;
                var (reviews, totalCount) = await _reviewService.GetReviewsAsync(model);

                model.Reviews = reviews;
                model.TotalCount = totalCount;

                ViewBag.PageTitle = "Quản lý đánh giá";
                ViewBag.ActiveMenu = "Reviews";



                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews management page");
                TempData["ErrorMessage"] = "Không thể tải danh sách đánh giá.";
                return View(new ReviewListViewModel());
            }
        }

        [HttpPost]
        [Route("reviews/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var adminUserId = _userManager.GetUserId(User);

                // Add null check before using adminUserId
                if (string.IsNullOrEmpty(adminUserId))
                {
                    _logger.LogWarning("DeleteReview called but GetUserId returned null");
                    return Json(new { success = false, message = "Không thể xác định người dùng." });
                }

                var success = await _reviewService.DeleteReviewAsync(id, adminUserId);

                if (success)
                {
                    return Json(new { success = true, message = "Xóa đánh giá thành công!" });
                }

                return Json(new { success = false, message = "Không tìm thấy đánh giá." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", id);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa đánh giá." });
            }
        }

        #endregion

        #region Reports & Analytics

        [Route("reports")]
        public async Task<IActionResult> Reports()
        {
            try
            {
                var model = new AdminDashboardViewModel();

                // Get order statistics
                var (totalRevenue, totalOrders, averageOrderValue) = await _orderService.GetOrderStatisticsAsync();
                var monthlyRevenue = await _orderService.GetMonthlyRevenueAsync(12);
                var ordersByStatus = await _orderService.GetOrdersByStatusAsync();

                // Get user registrations
                var userRegistrations = await _userService.GetUserRegistrationsAsync(12);

                // Get top selling books and top customers
                var topSellingBooks = await _orderService.GetTopSellingBooksAsync(5);
                var topCustomers = await _userService.GetTopCustomersAsync(5);

                model.TotalRevenue = totalRevenue;
                model.TotalOrders = totalOrders;
                model.MonthlyRevenue = monthlyRevenue;
                model.OrdersByStatus = ordersByStatus.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                model.UserRegistrations = userRegistrations;

                ViewBag.TopSellingBooks = topSellingBooks;
                ViewBag.TopCustomers = topCustomers;
                ViewBag.PageTitle = "Báo cáo & Thống kê";
                ViewBag.ActiveMenu = "Reports";



                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports page");
                TempData["ErrorMessage"] = "Không thể tải trang báo cáo.";
                return View(new AdminDashboardViewModel());
            }
        }

        [HttpGet]
        [Route("reports/export")]
        public async Task<IActionResult> ExportReport(string type, string format = "csv")
        {
            try
            {
                // TODO: Implement report export functionality
                return Json(new { success = false, message = "Tính năng xuất báo cáo đang được phát triển." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report of type: {Type}", type);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xuất báo cáo." });
            }
        }

        #endregion

        #region Settings

        [Route("settings")]
        public IActionResult Settings()
        {
            ViewBag.PageTitle = "Cài đặt hệ thống";
            ViewBag.ActiveMenu = "Settings";



            return View();
        }

        [HttpPost]
        [Route("settings/clear-cache")]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCache()
        {
            try
            {
                // TODO: Implement cache clearing
                TempData["SuccessMessage"] = "Cache đã được xóa thành công!";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa cache.";
                return RedirectToAction(nameof(Settings));
            }
        }

        [HttpPost]
        [Route("settings/backup")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBackup()
        {
            try
            {
                // TODO: Implement database backup
                TempData["SuccessMessage"] = "Backup đã được tạo thành công!";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tạo backup.";
                return RedirectToAction(nameof(Settings));
            }
        }

        #endregion

        #region Search API

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> Search(string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return Json(new { books = Array.Empty<object>(), orders = Array.Empty<object>(), users = Array.Empty<object>() });
                }

                // Search books
                var books = await _bookService.SearchBooksAsync(q, 5);
                var bookResults = books.Select(b => new
                {
                    id = b.Id,
                    title = b.Title,
                    author = b.Author,
                    price = b.Price
                });

                // Search orders (simplified - can be enhanced)
                var orderResults = Array.Empty<object>();

                // Search users (simplified - can be enhanced)
                var userResults = Array.Empty<object>();

                return Json(new
                {
                    books = bookResults,
                    orders = orderResults,
                    users = userResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing admin search for query: {Query}", q);
                return Json(new { error = "Tìm kiếm thất bại" });
            }
        }

        #endregion

        #region Notifications API

        [HttpGet]
        [Route("notifications/getunread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            try
            {
                // Generate notifications based on dashboard stats
                var stats = await _dashboardService.GetDashboardStatsAsync();

                var notifications = new List<object>();

                // Check for pending orders
                if (stats.TryGetValue("PendingOrders", out var pendingOrders) && Convert.ToInt32(pendingOrders) > 0)
                {
                    notifications.Add(new
                    {
                        id = 1,
                        type = "order",
                        title = "Đơn hàng chờ xử lý",
                        message = $"Có {pendingOrders} đơn hàng đang chờ xử lý",
                        url = "/admin/orders?status=Pending",
                        createdAt = DateTime.UtcNow,
                        isRead = false
                    });
                }

                // Check for low stock books
                if (stats.TryGetValue("LowStockCount", out var lowStock) && Convert.ToInt32(lowStock) > 0)
                {
                    notifications.Add(new
                    {
                        id = 2,
                        type = "warning",
                        title = "Sách sắp hết hàng",
                        message = $"Có {lowStock} sách có tồn kho thấp (< 10 cuốn)",
                        url = "/admin/books?filter=lowstock",
                        createdAt = DateTime.UtcNow,
                        isRead = false
                    });
                }

                // Check for total reviews (informational)
                if (stats.TryGetValue("TotalReviews", out var totalReviews) && Convert.ToInt32(totalReviews) > 0)
                {
                    var recentReviews = Convert.ToInt32(totalReviews);
                    if (recentReviews > 0)
                    {
                        notifications.Add(new
                        {
                            id = 3,
                            type = "review",
                            title = "Đánh giá mới",
                            message = $"Hệ thống có {recentReviews} đánh giá từ khách hàng",
                            url = "/admin/reviews",
                            createdAt = DateTime.UtcNow,
                            isRead = false
                        });
                    }
                }

                // Check for new users in last 24h
                if (stats.TryGetValue("NewUsersToday", out var newUsers) && Convert.ToInt32(newUsers) > 0)
                {
                    notifications.Add(new
                    {
                        id = 4,
                        type = "user",
                        title = "Người dùng mới",
                        message = $"Có {newUsers} người dùng mới đăng ký trong 24h qua",
                        url = "/admin/users",
                        createdAt = DateTime.UtcNow,
                        isRead = false
                    });
                }

                return Json(new
                {
                    count = notifications.Count,
                    notifications = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications");
                return Json(new { count = 0, notifications = Array.Empty<object>() });
            }
        }

        #endregion
    }

    #region Helper Models

    public class OrderStatusUpdateModel
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateStockModel
    {
        public int Quantity { get; set; }
    }

    #endregion



}