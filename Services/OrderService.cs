// Services/IOrderService.cs & OrderService.cs
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;

namespace BookStoreMVC.Services
{
    public interface IOrderService
    {
        Task<OrderViewModel?> CreateOrderAsync(string userId, OrderCreateViewModel model);
        Task<OrderViewModel?> GetOrderByIdAsync(int orderId, string? userId = null);
        Task<(IEnumerable<OrderViewModel> Orders, int TotalCount)> GetOrdersAsync(OrderListViewModel model, string? userId = null);
        Task<IEnumerable<OrderViewModel>> GetUserOrdersAsync(string userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<bool> CancelOrderAsync(int orderId, string userId);
        Task<(decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue)> GetOrderStatisticsAsync();
        Task<Dictionary<string, decimal>> GetMonthlyRevenueAsync(int months = 12);
        Task<Dictionary<OrderStatus, int>> GetOrdersByStatusAsync();
        Task<IEnumerable<BookSummaryViewModel>> GetTopSellingBooksAsync(int count = 5);
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            ApplicationDbContext context,
            ICartService cartService,
            ILogger<OrderService> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<OrderViewModel?> CreateOrderAsync(string userId, OrderCreateViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get cart items
                var cart = await _cartService.GetCartAsync(userId);
                if (cart.IsEmpty)
                    throw new InvalidOperationException("Cart is empty");

                // Validate stock
                foreach (var item in cart.Items)
                {
                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book == null || book.StockQuantity < item.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for {item.Book.Title}");
                }

                // Create shipping info
                var shippingInfo = new ShippingInfo
                {
                    FirstName = model.ShippingFirstName,
                    LastName = model.ShippingLastName,
                    Address = model.ShippingAddress,
                    City = model.ShippingCity,
                    PostalCode = model.ShippingPostalCode,
                    Country = model.ShippingCountry,
                    Phone = model.ShippingPhone
                };

                _context.ShippingInfos.Add(shippingInfo);
                await _context.SaveChangesAsync();

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    ShippingInfoId = shippingInfo.Id,
                    Status = OrderStatus.Processing, // Auto-confirm order after successful payment
                    SubTotal = cart.SubTotal,
                    Tax = cart.Tax,
                    Discount = 0,
                    Total = cart.Total,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items and update stock
                foreach (var item in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Total = item.TotalPrice
                    };

                    _context.OrderItems.Add(orderItem);

                    // Update book stock
                    var book = await _context.Books.FindAsync(item.BookId);
                    book!.StockQuantity -= item.Quantity;
                }

                // Create payment record
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Method = model.PaymentMethod,
                    TransactionId = model.TransactionId,
                    Amount = order.Total,
                    Status = PaymentStatus.Completed, // Auto-complete payment for school project
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                order.PaymentId = payment.Id;

                await _context.SaveChangesAsync();

                // Clear cart
                await _cartService.ClearCartAsync(userId);

                await transaction.CommitAsync();

                return await GetOrderByIdAsync(order.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order for user {UserId}", userId);
                throw;
            }
        }

        public async Task<OrderViewModel?> GetOrderByIdAsync(int orderId, string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingInfo)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                        .ThenInclude(b => b.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(o => o.UserId == userId);

            var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);

            return order != null ? MapToViewModel(order) : null;
        }

        public async Task<(IEnumerable<OrderViewModel> Orders, int TotalCount)> GetOrdersAsync(OrderListViewModel model, string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingInfo)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(o => o.UserId == userId);

            // Apply filters
            if (model.StatusFilter.HasValue)
                query = query.Where(o => o.Status == model.StatusFilter);

            if (model.StartDate.HasValue)
                query = query.Where(o => o.CreatedAt >= model.StartDate);

            if (model.EndDate.HasValue)
                query = query.Where(o => o.CreatedAt <= model.EndDate);

            // Apply sorting
            query = model.SortBy.ToLower() switch
            {
                "created" => query.OrderBy(o => o.CreatedAt),
                "created_desc" => query.OrderByDescending(o => o.CreatedAt),
                "total" => query.OrderBy(o => o.Total),
                "total_desc" => query.OrderByDescending(o => o.Total),
                "status" => query.OrderBy(o => o.Status),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var orders = await query
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            return (orders.Select(MapToViewModel), totalCount);
        }

        public async Task<IEnumerable<OrderViewModel>> GetUserOrdersAsync(string userId)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.ShippingInfo)
                    .Include(o => o.Payment)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Book)
                            .ThenInclude(b => b.Category)  // THÊM DÒNG NÀY
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return orders.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user orders for {UserId}", userId);
                return Enumerable.Empty<OrderViewModel>();
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, string userId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Status != OrderStatus.Pending)
                return false;

            order.Status = OrderStatus.Cancelled;

            // Restore book stock
            foreach (var item in order.OrderItems)
            {
                item.Book.StockQuantity += item.Quantity;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private OrderViewModel MapToViewModel(Order order)
        {
            return new OrderViewModel
            {
                Id = order.Id,
                UserId = order.UserId,
                Status = order.Status,
                SubTotal = order.SubTotal,
                Tax = order.Tax,
                Discount = order.Discount,
                Total = order.Total,
                CreatedAt = order.CreatedAt,
                User = order.User,
                ShippingInfo = order.ShippingInfo,
                Payment = order.Payment,
                OrderItems = order.OrderItems
                    .Where(oi => oi.Book != null)  // ← THÊM NULL CHECK
                    .Select(oi => new OrderItemViewModel
                    {
                        Id = oi.Id,
                        BookId = oi.BookId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        Total = oi.Total,
                        BookTitle = oi.Book.Title,
                        BookAuthor = oi.Book.Author,
                        Book = oi.Book
                    }).ToList()
            };
        }

        public async Task<(decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue)> GetOrderStatisticsAsync()
        {
            var completedOrders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .ToListAsync();

            var totalRevenue = completedOrders.Sum(o => o.Total);
            var totalOrders = completedOrders.Count;
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            return (totalRevenue, totalOrders, averageOrderValue);
        }

        public async Task<Dictionary<string, decimal>> GetMonthlyRevenueAsync(int months = 12)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddMonths(-months);

                var monthlyData = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Delivered && o.CreatedAt >= startDate)
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Revenue = g.Sum(o => o.Total)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return monthlyData.ToDictionary(x => x.Date.ToString("MMM yyyy"), x => x.Revenue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly revenue");
                return new Dictionary<string, decimal>();
            }
        }

        public async Task<Dictionary<OrderStatus, int>> GetOrdersByStatusAsync()
        {
            return await _context.Orders
                .GroupBy(o => o.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<IEnumerable<BookSummaryViewModel>> GetTopSellingBooksAsync(int count = 5)
        {
            try
            {
                var topBooks = await _context.OrderItems
                    .Include(oi => oi.Book)
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Book != null && oi.Order != null && oi.Order.Status == OrderStatus.Delivered)
                    .GroupBy(oi => new { oi.BookId, oi.Book.Title })
                    .Select(g => new BookSummaryViewModel
                    {
                        BookId = g.Key.BookId,
                        Title = g.Key.Title,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Total)
                    })
                    .OrderByDescending(b => b.QuantitySold)
                    .Take(count)
                    .ToListAsync();

                return topBooks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling books");
                return new List<BookSummaryViewModel>();
            }
        }
    }
}