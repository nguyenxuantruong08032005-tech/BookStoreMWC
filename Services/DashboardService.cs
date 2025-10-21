// Services/IDashboardService.cs & DashboardService.cs
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;

namespace BookStoreMVC.Services
{
    public interface IDashboardService
    {
        Task<AdminDashboardViewModel> GetAdminDashboardAsync();
        Task<Dictionary<string, object>> GetDashboardStatsAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBookService _bookService;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            IBookService bookService,
            IOrderService orderService,
            IUserService userService,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _bookService = bookService;
            _orderService = orderService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
        {
            var totalUsers = await _userService.GetTotalUsersCountAsync();
            var totalBooks = await _context.Books.CountAsync(b => b.IsActive);
            var totalOrders = await _context.Orders.CountAsync();
            var totalCategories = await _context.Categories.CountAsync(c => c.IsActive);

            var (totalRevenue, _, _) = await _orderService.GetOrderStatisticsAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending);

            var lowStockBooks = await _context.Books
                .CountAsync(b => b.IsActive && b.StockQuantity <= 10);

            var unapprovedReviews = await _context.Reviews
                .CountAsync(r => !r.IsApproved);

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    Status = o.Status,
                    Total = o.Total,
                    CreatedAt = o.CreatedAt,
                    User = o.User
                })
                .ToListAsync();

            var recentReviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Book)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new ReviewViewModel
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    IsApproved = r.IsApproved,
                    User = r.User,
                    Book = r.Book
                })
                .ToListAsync();

            var newUsers = await _userService.GetRecentUsersAsync(10);

            var lowStockItems = await _context.Books
                .Include(b => b.Category)
                .Where(b => b.IsActive && b.StockQuantity <= 10)
                .Take(10)
                .Select(b => new BookViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    StockQuantity = b.StockQuantity,
                    Category = b.Category
                })
                .ToListAsync();

            var monthlyRevenue = await _orderService.GetMonthlyRevenueAsync(12);
            var ordersByStatus = await _orderService.GetOrdersByStatusAsync();

            var booksByCategory = await _context.Categories
                .Include(c => c.Books)
                .Where(c => c.IsActive)
                .Select(c => new { c.Name, Count = c.Books.Count(b => b.IsActive) })
                .ToListAsync();

            var booksByCategoryDict = booksByCategory
                .GroupBy(x => x.Name)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            var userRegistrations = await _userService.GetUserRegistrationsAsync(12);

            return new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalBooks = totalBooks,
                TotalOrders = totalOrders,
                TotalCategories = totalCategories,
                TotalRevenue = totalRevenue,
                PendingOrders = pendingOrders,
                LowStockBooks = lowStockBooks,
                UnapprovedReviews = unapprovedReviews,
                RecentOrders = recentOrders,
                RecentReviews = recentReviews,
                NewUsers = newUsers,
                LowStockItems = lowStockItems,
                MonthlyRevenue = monthlyRevenue,
                OrdersByStatus = ordersByStatus.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                BooksByCategory = booksByCategoryDict,
                UserRegistrations = userRegistrations
            };
        }

        public async Task<Dictionary<string, object>> GetDashboardStatsAsync()
        {
            var stats = new Dictionary<string, object>();

            // Basic counts
            stats["totalUsers"] = await _context.Users.CountAsync();
            stats["totalBooks"] = await _context.Books.CountAsync(b => b.IsActive);
            stats["totalOrders"] = await _context.Orders.CountAsync();
            stats["totalRevenue"] = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.Total);

            // Order statistics
            var orderStats = await _orderService.GetOrdersByStatusAsync();
            stats["pendingOrders"] = orderStats.GetValueOrDefault(OrderStatus.Pending, 0);
            stats["processingOrders"] = orderStats.GetValueOrDefault(OrderStatus.Processing, 0);
            stats["shippedOrders"] = orderStats.GetValueOrDefault(OrderStatus.Shipped, 0);
            stats["deliveredOrders"] = orderStats.GetValueOrDefault(OrderStatus.Delivered, 0);

            // Low stock alerts
            stats["lowStockBooks"] = await _context.Books
                .CountAsync(b => b.IsActive && b.StockQuantity <= 10);

            // Review statistics
            stats["totalReviews"] = await _context.Reviews.CountAsync();
            stats["unapprovedReviews"] = await _context.Reviews.CountAsync(r => !r.IsApproved);
            stats["averageRating"] = await _context.Reviews
                .Where(r => r.IsApproved)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            return stats;
        }
    }
}