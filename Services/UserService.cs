// Services/IUserService.cs & UserService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;

namespace BookStoreMVC.Services
{
    public interface IUserService
    {
        Task<UserProfileViewModel?> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, EditProfileViewModel model);
        Task<UserDashboardViewModel> GetUserDashboardAsync(string userId);
        Task<IEnumerable<User>> GetRecentUsersAsync(int count = 10);
        Task<int> GetTotalUsersCountAsync();
        Task<Dictionary<string, int>> GetUserRegistrationsAsync(int months = 12);
        Task<IEnumerable<UserSummaryViewModel>> GetTopCustomersAsync(int count = 5);


    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IBookService _bookService;
        private readonly IOrderService _orderService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IBookService bookService,
            IOrderService orderService,
            ILogger<UserService> logger)
        {
            _context = context;
            _userManager = userManager;
            _bookService = bookService;
            _orderService = orderService;
            _logger = logger;
        }

        public async Task<UserProfileViewModel?> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var totalOrders = await _context.Orders
                .CountAsync(o => o.UserId == userId);

            var totalSpent = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.Total);

            var reviewsCount = await _context.Reviews
                .CountAsync(r => r.UserId == userId);

            var wishlistCount = await _context.WishlistItems
                .CountAsync(w => w.UserId == userId);

            var recentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    Status = o.Status,
                    Total = o.Total,
                    CreatedAt = o.CreatedAt,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemViewModel
                    {
                        BookTitle = oi.Book.Title,
                        Quantity = oi.Quantity,
                        Total = oi.Total
                    }).ToList()
                })
                .ToListAsync();

            var recentReviews = await _context.Reviews
                .Include(r => r.Book)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new ReviewViewModel
                {
                    Id = r.Id,
                    BookId = r.BookId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Book = r.Book
                })
                .ToListAsync();

            return new UserProfileViewModel
            {
                Name = user.Name,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                TotalOrders = totalOrders,
                TotalSpent = totalSpent,
                ReviewsCount = reviewsCount,
                WishlistCount = wishlistCount,
                MemberSince = user.CreatedAt,
                RecentOrders = recentOrders,
                RecentReviews = recentReviews
            };
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, EditProfileViewModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.Name = model.Name;
            user.Email = model.Email;
            user.UserName = model.Email; // đồng bộ UserName với Email
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<UserDashboardViewModel> GetUserDashboardAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            var cartItemsCount = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            var wishlistItemsCount = await _context.WishlistItems
                .CountAsync(w => w.UserId == userId);

            var pendingOrdersCount = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.Status == OrderStatus.Pending);

            var totalSpent = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.Total);

            var recentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    Status = o.Status,
                    Total = o.Total,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            var recommendedBooks = await _bookService.GetRecommendedBooksAsync(userId, 6);

            return new UserDashboardViewModel
            {
                User = user,
                CartItemsCount = cartItemsCount,
                WishlistItemsCount = wishlistItemsCount,
                PendingOrdersCount = pendingOrdersCount,
                // TotalSpent = totalSpent,
                // RecentOrders = recentOrders,
                RecommendedBooks = recommendedBooks
            };
        }

        public async Task<IEnumerable<User>> GetRecentUsersAsync(int count = 10)
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<Dictionary<string, int>> GetUserRegistrationsAsync(int months = 12)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddMonths(-months);

                var registrationData = await _context.Users
                    .Where(u => u.CreatedAt >= startDate)
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return registrationData.ToDictionary(x => x.Date.ToString("MMM yyyy"), x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user registrations");
                return new Dictionary<string, int>();
            }
        }

        public async Task<IEnumerable<UserSummaryViewModel>> GetTopCustomersAsync(int count = 5)
        {
            try
            {
                var topCustomers = await _context.Orders
                    .Include(o => o.User)
                    .Where(o => o.Status == OrderStatus.Delivered && o.User != null)
                    .GroupBy(o => new { o.UserId, UserName = o.User.Name ?? o.User.Email ?? "Unknown" })
                    .Select(g => new UserSummaryViewModel
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.UserName,
                        OrdersCount = g.Count(),
                        TotalSpent = g.Sum(o => o.Total)
                    })
                    .OrderByDescending(u => u.TotalSpent)
                    .Take(count)
                    .ToListAsync();

                return topCustomers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers");
                return new List<UserSummaryViewModel>();
            }
        }
    }
}