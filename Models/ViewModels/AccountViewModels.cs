// Models/ViewModels/AccountViewModels.cs
using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models.ViewModels
{
    // ===================================================================
    // LOGIN VIEW MODEL
    // ===================================================================
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    // ===================================================================
    // REGISTER VIEW MODEL
    // ===================================================================
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} và tối đa {1} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Tôi đồng ý với điều khoản và điều kiện")]
        public bool AgreeToTerms { get; set; }
        [Display(Name = "Tôi muốn nhận email về các chương trình ưu đãi")]
        public bool ReceivePromotions { get; set; }
    }

    // ===================================================================
    // FORGOT PASSWORD VIEW MODEL
    // ===================================================================
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    // ===================================================================
    // RESET PASSWORD VIEW MODEL
    // ===================================================================
    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} và tối đa {1} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ❌ ĐÃ XÓA: LoginWith2faViewModel - Không cần 2FA nữa

    // ===================================================================
    // CHANGE PASSWORD VIEW MODEL
    // ===================================================================
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} và tối đa {1} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ===================================================================
    // USER PROFILE VIEW MODEL (ENHANCED)
    // ===================================================================
    public class UserProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Thành phố")]
        public string? City { get; set; }

        [StringLength(100)]
        [Display(Name = "Quốc gia")]
        public string? Country { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Ngày tham gia")]
        public DateTime CreatedAt { get; set; }

        // THỐNG KÊ
        [Display(Name = "Tổng đơn hàng")]
        public int TotalOrders { get; set; }

        [Display(Name = "Tổng chi tiêu")]
        public decimal TotalSpent { get; set; }

        [Display(Name = "Số đánh giá")]
        public int ReviewsCount { get; set; }

        [Display(Name = "Số sản phẩm yêu thích")]
        public int WishlistCount { get; set; }

        [Display(Name = "Thành viên từ")]
        public DateTime MemberSince { get; set; }

        // HOẠT ĐỘNG GẦN ĐÂY
        public List<OrderViewModel> RecentOrders { get; set; } = new();
        public List<ReviewViewModel> RecentReviews { get; set; } = new();
    }

    // ===================================================================
    // EDIT PROFILE VIEW MODEL
    // ===================================================================
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Thành phố")]
        public string? City { get; set; }

        [StringLength(100)]
        [Display(Name = "Quốc gia")]
        public string? Country { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }

        public string? CurrentProfilePictureUrl { get; set; }
    }

    // ===================================================================
    // USER DASHBOARD VIEW MODEL
    // ===================================================================
    // public class UserDashboardViewModel
    // {
    //     public string UserId { get; set; } = string.Empty;
    //     public string UserName { get; set; } = string.Empty;
    //     public string Email { get; set; } = string.Empty;
    //     public string? ProfilePictureUrl { get; set; }

    //     // Statistics
    //     public int TotalOrders { get; set; }
    //     public int PendingOrders { get; set; }
    //     public int CompletedOrders { get; set; }
    //     public decimal TotalSpent { get; set; }

    //     public int WishlistCount { get; set; }
    //     public int CartItemCount { get; set; }
    //     public int ReviewCount { get; set; }

    //     // Recent activities
    //     public List<OrderViewModel> RecentOrders { get; set; } = new();
    //     public List<BookViewModel> RecentlyViewedBooks { get; set; } = new();
    // }
}