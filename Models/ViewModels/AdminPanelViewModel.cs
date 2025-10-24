using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Statistics
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCategories { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockBooks { get; set; }
        public int UnapprovedReviews { get; set; }

        // Recent activity
        public IEnumerable<OrderViewModel> RecentOrders { get; set; } = new List<OrderViewModel>();
        public IEnumerable<ReviewViewModel> RecentReviews { get; set; } = new List<ReviewViewModel>();
        public IEnumerable<User> NewUsers { get; set; } = new List<User>();
        public IEnumerable<BookViewModel> LowStockItems { get; set; } = new List<BookViewModel>();

        // Charts data
        public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new();
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();
        public Dictionary<string, int> BooksByCategory { get; set; } = new();
        public Dictionary<string, int> UserRegistrations { get; set; } = new();
    }

    public class UserDashboardViewModel
    {
        public User User { get; set; } = new();
        public int CartItemsCount { get; set; }
        public int WishlistItemsCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public decimal TotalSpent { get; set; }

        // Recent activity
        public IEnumerable<OrderViewModel> RecentOrders { get; set; } = new List<OrderViewModel>();
        public IEnumerable<BookViewModel> RecommendedBooks { get; set; } = new List<BookViewModel>();
        public IEnumerable<BookViewModel> ContinueReading { get; set; } = new List<BookViewModel>();
        public IEnumerable<ReviewViewModel> MyRecentReviews { get; set; } = new List<ReviewViewModel>();
    }

    public class AdminOrdersViewModel
    {
        public OrderListViewModel OrderList { get; set; } = new();

        // Thống kê toàn hệ thống
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TodayOrders { get; set; }
        public decimal TodayRevenue { get; set; }

        // Có thể bổ sung Top Users, Top Books
        public IEnumerable<UserSummaryViewModel> TopCustomers { get; set; } = new List<UserSummaryViewModel>();
         public IEnumerable<TopSellingBookViewModel> TopSellingBooks { get; set; } = new List<TopSellingBookViewModel>();
    }

    public class UserSummaryViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class TopSellingBookViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsLocked { get; set; } // true = tài khoản bị khóa
    }
    public class AdminUsersViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new List<UserItemViewModel>();

        // Thêm các trường hỗ trợ tìm kiếm, phân trang
        public string? SearchTerm { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;
    }
}