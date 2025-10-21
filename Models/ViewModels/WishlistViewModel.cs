// Models/ViewModels/WishlistViewModel.cs
using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class WishlistViewModel
    {
        public ICollection<WishlistItemViewModel> Items { get; set; } = new List<WishlistItemViewModel>();

        // Calculated properties
        public int ItemCount => Items.Count;
        public bool IsEmpty => !Items.Any();
        public decimal TotalValue => Items.Sum(i => i.Book.DisplayPrice);
        public int InStockCount => Items.Count(i => i.Book.InStock);
        public int OutOfStockCount => Items.Count(i => !i.Book.InStock);
    }

    public class WishlistItemViewModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Book information
        public BookViewModel Book { get; set; } = new();

        // Calculated properties
        public bool InStock => Book.InStock;
        public bool HasDiscount => Book.HasDiscount;
        public string TimeAdded
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                if (timeSpan.TotalDays >= 30)
                    return $"{(int)(timeSpan.TotalDays / 30)} month(s) ago";
                if (timeSpan.TotalDays >= 1)
                    return $"{(int)timeSpan.TotalDays} day(s) ago";
                return "Recently";
            }
        }
    }

    public class AddToWishlistViewModel
    {
        public int BookId { get; set; }
    }
}