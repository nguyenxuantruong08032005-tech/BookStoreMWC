using System.ComponentModel.DataAnnotations;
using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        // Shipping Information
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        [Display(Name = "First Name")]
        public string ShippingFirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        [Display(Name = "Last Name")]
        public string ShippingLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        [Display(Name = "Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City cannot be longer than 100 characters")]
        [Display(Name = "City")]
        public string ShippingCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required")]
        [StringLength(20, ErrorMessage = "Postal code cannot be longer than 20 characters")]
        [Display(Name = "Postal Code")]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country cannot be longer than 100 characters")]
        [Display(Name = "Country")]
        public string ShippingCountry { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string? ShippingPhone { get; set; }

        // Payment Information
        [Required(ErrorMessage = "Payment method is required")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Transaction ID cannot be longer than 100 characters")]
        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }

        // Order Information
        [StringLength(500, ErrorMessage = "Notes cannot be longer than 500 characters")]
        [Display(Name = "Order Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Use profile address")]
        public bool UseProfileAddress { get; set; }

        // Readonly properties for display
        public CartViewModel Cart { get; set; } = new();
        public IEnumerable<string> AvailablePaymentMethods { get; set; } = new List<string>();
        public IEnumerable<string> AvailableCountries { get; set; } = new List<string>();
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = new();
        public ShippingInfo ShippingInfo { get; set; } = new();
        public Payment? Payment { get; set; }
        public ICollection<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        // Calculated properties
        public string OrderNumber => $"ORD-{Id:D6}";
        public string StatusDisplayName => Status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.Processing => "Processing",
            OrderStatus.Shipped => "Shipped",
            OrderStatus.Delivered => "Delivered",
            OrderStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
        public string StatusCssClass => Status switch
        {
            OrderStatus.Pending => "bg-yellow-100 text-yellow-800",
            OrderStatus.Processing => "bg-blue-100 text-blue-800",
            OrderStatus.Shipped => "bg-purple-100 text-purple-800",
            OrderStatus.Delivered => "bg-green-100 text-green-800",
            OrderStatus.Cancelled => "bg-red-100 text-red-800",
            _ => "bg-gray-100 text-gray-800"
        };
        public bool CanCancel => Status == OrderStatus.Pending;
        public int ItemCount => OrderItems.Sum(i => i.Quantity);
    }

    public class OrderItemViewModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }

        // Book information
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string? BookImageUrl { get; set; }

        // Navigation properties
        public Book Book { get; set; } = new();
    }

    public class OrderListViewModel
    {
        // --- Danh sách đơn hàng ---
        public IEnumerable<OrderViewModel> Orders { get; set; } = new List<OrderViewModel>();

        // --- Bộ lọc ---
        public OrderStatus? StatusFilter { get; set; }
        public string SearchTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // --- Sắp xếp ---
        public string SortBy { get; set; } = "created_desc";
        public Dictionary<string, string> SortOptions => new()
        {
            {"created_desc", "Newest First"},
            {"created", "Oldest First"},
            {"total_desc", "Highest Amount"},
            {"total", "Lowest Amount"},
            {"status", "Status"}
        };

        // --- Phân trang ---
        public int PageNumber { get; set; } = 1;             // Trang hiện tại
        public int PageSize { get; set; } = 10;              // Số bản ghi mỗi trang
        public int TotalCount { get; set; }                  // Tổng số bản ghi
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // Chỉ số hiển thị (ví dụ: "Hiển thị 11–20 / 52 đơn hàng")
        public int StartItem => (PageNumber - 1) * PageSize + 1;
        public int EndItem => Math.Min(PageNumber * PageSize, TotalCount);

        // --- Thống kê trạng thái ---
        public decimal TotalAmount => Orders.Sum(o => o.Total);
        public int PendingCount => Orders.Count(o => o.Status == OrderStatus.Pending);
        public int ProcessingCount => Orders.Count(o => o.Status == OrderStatus.Processing);
        public int ShippedCount => Orders.Count(o => o.Status == OrderStatus.Shipped);
        public int DeliveredCount => Orders.Count(o => o.Status == OrderStatus.Delivered);
        public int CancelledCount => Orders.Count(o => o.Status == OrderStatus.Cancelled);
    }
}
