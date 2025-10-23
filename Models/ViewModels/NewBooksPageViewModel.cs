using System;
using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class NewBooksPageViewModel
    {
        public string ActiveFilter { get; set; } = "all";
        public BookListViewModel Listing { get; set; } = new();
        public List<BookViewModel> SpotlightBooks { get; set; } = new();
        public List<BookViewModel> TrendingBooks { get; set; } = new();
        public List<CategoryViewModel> HighlightCategories { get; set; } = new();
        public int TotalNewBooks { get; set; }
        public int BooksAddedThisMonth { get; set; }
        public DateTime? LatestAddedDate { get; set; }
        public int ActiveCategories { get; set; }
    }
}
