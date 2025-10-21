using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class CartViewModel
    {
        public ICollection<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

        // Calculated properties
        public decimal SubTotal => Items.Sum(i => i.TotalPrice);
        public decimal Tax => SubTotal * 0.1m; // 10% VAT
        public decimal ShippingCost => SubTotal >= 299000 ? 0 : 30000;
        public decimal Total => SubTotal + Tax + ShippingCost;
        public int ItemCount => Items.Sum(i => i.Quantity);
        public bool IsEmpty => !Items.Any();
        public bool QualifiesForFreeShipping => SubTotal >= 299000;
        public decimal AmountForFreeShipping => Math.Max(0, 299000 - SubTotal);
    }

    public class CartItemViewModel
    {
        // Id CÓ THỂ NULL cho session cart
        public int? Id { get; set; }

        public int BookId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Book information
        public BookViewModel? Book { get; set; }

        // Thuộc tính riêng cho session cart
        public string BookTitle { get; set; } = string.Empty;
        public string? BookImage { get; set; }
        public string BookAuthor { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // Calculated properties
        public decimal UnitPrice => Book?.DisplayPrice ?? Price;
        public decimal TotalPrice => UnitPrice * Quantity;
        public bool InStock => Book?.InStock ?? true;
        public int MaxQuantity => Book != null ? Math.Min(Book.StockQuantity, 10) : 10;
    }

    public class AddToCartViewModel
    {
        public int BookId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemViewModel
    {
        public int BookId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveFromCartViewModel
    {
        public int BookId { get; set; }
    }
}