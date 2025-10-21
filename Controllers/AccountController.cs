// Controllers/AccountController.cs - HOÀN CHỈNH KHÔNG CÓ 2FA
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;
using BookStoreMVC.Services;

namespace BookStoreMVC.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ICartService _cartService;
        private readonly ISessionCartService _sessionCartService;
        private readonly ILogger<AccountController> _logger;

        // ===================================================================
        // CONSTRUCTOR
        // ===================================================================
        public AccountController(
            IUserService userService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ICartService cartService,
            ISessionCartService sessionCartService,
            ILogger<AccountController> logger)
        {
            _userService = userService;
            _userManager = userManager;
            _signInManager = signInManager;
            _cartService = cartService;
            _sessionCartService = sessionCartService;
            _logger = logger;
        }

        // ===================================================================
        // DASHBOARD
        // ===================================================================
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var dashboard = await _userService.GetUserDashboardAsync(userId);

                ViewBag.PageTitle = "My Account";
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user dashboard");
                return View(new UserDashboardViewModel());
            }
        }

        // ===================================================================
        // PROFILE - GET
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var profile = await _userService.GetUserProfileAsync(userId);

                if (profile == null)
                {
                    return NotFound();
                }

                ViewBag.PageTitle = "My Profile";
                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                return RedirectToAction(nameof(Index));
            }
        }

        // ===================================================================
        // PROFILE - POST (Update Profile)
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Nếu validation fail, load lại profile
                    var userId = _userManager.GetUserId(User)!;
                    var fullProfile = await _userService.GetUserProfileAsync(userId);
                    return View(fullProfile);
                }

                var currentUserId = _userManager.GetUserId(User)!;
                var success = await _userService.UpdateUserProfileAsync(currentUserId, model);

                if (!success)
                {
                    ModelState.AddModelError("", "Không thể cập nhật hồ sơ.");
                    var fullProfile = await _userService.GetUserProfileAsync(currentUserId);
                    return View(fullProfile);
                }

                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật hồ sơ.");

                var userId = _userManager.GetUserId(User)!;
                var profile = await _userService.GetUserProfileAsync(userId);
                return View(profile ?? new UserProfileViewModel());
            }
        }

        // ===================================================================
        // CHANGE PASSWORD
        // ===================================================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewBag.PageTitle = "Đổi mật khẩu";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction(nameof(Login));
                }

                var result = await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword);

                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    _logger.LogInformation("User changed their password successfully.");
                    TempData["SuccessMessage"] = "Mật khẩu đã được thay đổi thành công.";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đổi mật khẩu.");
                return View(model);
            }
        }

        // ===================================================================
        // LOGIN - GET
        // ===================================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.PageTitle = "Đăng nhập";
            return View();
        }

        // ===================================================================
        // LOGIN - POST (KHÔNG CÓ 2FA)
        // ===================================================================
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Lưu thông tin giỏ hàng session TRƯỚC KHI đăng nhập
                var sessionCartItems = _sessionCartService.GetCart().Items.ToList();

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true
                );

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // MIGRATE GIỎ HÀNG TỪ SESSION SANG DATABASE
                    if (sessionCartItems.Any())
                    {
                        var userId = _userManager.GetUserId(User)!;

                        foreach (var item in sessionCartItems)
                        {
                            try
                            {
                                await _cartService.AddToCartAsync(userId, new AddToCartViewModel
                                {
                                    BookId = item.BookId,
                                    Quantity = item.Quantity
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error migrating cart item {item.BookId}");
                            }
                        }

                        // Xóa session cart sau khi migrate thành công
                        _sessionCartService.ClearCart();

                        TempData["SuccessMessage"] = "Đăng nhập thành công! Giỏ hàng của bạn đã được cập nhật.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Đăng nhập thành công!";
                    }

                    // Redirect
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                // BỎ HOÀN TOÀN 2FA - Không check RequiresTwoFactor nữa
                // if (result.RequiresTwoFactor) { ... } // ← ĐÃ XÓA

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    ModelState.AddModelError(string.Empty,
                        "Tài khoản của bạn đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau.");
                    return View(model);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại.");
                return View(model);
            }
        }

        // ===================================================================
        // REGISTER - GET
        // ===================================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.PageTitle = "Đăng ký";
            return View();
        }

        // ===================================================================
        // REGISTER - POST
        // ===================================================================
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Lưu giỏ hàng session trước khi đăng ký
                var sessionCartItems = _sessionCartService.GetCart().Items.ToList();

                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Thêm role Customer cho tài khoản mới
                    var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        await _userManager.DeleteAsync(user);
                        return View(model);
                    }

                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        var confirmResult = await _userManager.UpdateAsync(user);
                        if (!confirmResult.Succeeded)
                        {
                            foreach (var error in confirmResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }

                            await _userManager.DeleteAsync(user);
                            return View(model);
                        }
                    }

                    // Tự động đăng nhập sau khi đăng ký
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // MIGRATE GIỎ HÀNG TỪ SESSION SANG DATABASE
                    if (sessionCartItems.Any())
                    {
                        foreach (var item in sessionCartItems)
                        {
                            try
                            {
                                await _cartService.AddToCartAsync(user.Id, new AddToCartViewModel
                                {
                                    BookId = item.BookId,
                                    Quantity = item.Quantity
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error migrating cart item {item.BookId}");
                            }
                        }

                        _sessionCartService.ClearCart();
                        TempData["SuccessMessage"] = "Đăng ký thành công! Giỏ hàng của bạn đã được lưu.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Đăng ký thành công! Chào mừng bạn đến với BookVerse.";
                    }

                    // Redirect
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại.");
                return View(model);
            }
        }

        // ===================================================================
        // LOGOUT
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            TempData["InfoMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        // ===================================================================
        // FORGOT PASSWORD - GET
        // ===================================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewBag.PageTitle = "Quên mật khẩu";
            return View();
        }

        // ===================================================================
        // FORGOT PASSWORD - POST
        // ===================================================================
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Không tiết lộ là email này chưa đăng ký (bảo mật)
                TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, một liên kết đặt lại mật khẩu đã được gửi.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account",
                new { userId = user.Id, token = token }, protocol: Request.Scheme);

            // TODO: Gửi email callbackUrl cho user
            // await _emailService.SendPasswordResetEmailAsync(model.Email, callbackUrl);

            _logger.LogInformation($"Password reset link generated for {model.Email}: {callbackUrl}");

            TempData["SuccessMessage"] = "Liên kết đặt lại mật khẩu đã được gửi đến email của bạn.";
            TempData["DebugResetUrl"] = callbackUrl; // CHỈ CHO DEV - XÓA KHI PRODUCTION

            return RedirectToAction(nameof(Login));
        }

        // ===================================================================
        // RESET PASSWORD - GET
        // ===================================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string? userId = null, string? token = null)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                UserId = userId
            };

            ViewBag.PageTitle = "Đặt lại mật khẩu";
            return View(model);
        }

        // ===================================================================
        // RESET PASSWORD - POST
        // ===================================================================
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ===================================================================
        // ACCESS DENIED
        // ===================================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            ViewBag.PageTitle = "Truy cập bị từ chối";
            return View();
        }
    }
}