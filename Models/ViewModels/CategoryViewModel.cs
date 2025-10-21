using System.ComponentModel.DataAnnotations;
using BookStoreMVC.Models.Entities;

namespace BookStoreMVC.Models.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Parent Category")]
        public int? ParentCategoryId { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Image URL")]
        [StringLength(255, ErrorMessage = "Image URL cannot be longer than 255 characters")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Category? ParentCategory { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public ICollection<Book> Books { get; set; } = new List<Book>();
        public IEnumerable<Category> AvailableParentCategories { get; set; } = new List<Category>();

        // Calculated properties
        public int BookCount => Books.Count(b => b.IsActive);
        public bool HasSubCategories => SubCategories.Any();
        public string FullPath => ParentCategory != null ? $"{ParentCategory.Name} > {Name}" : Name;
    }

    public class CategoryListViewModel
    {
        public IEnumerable<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();

        // --- Filters / Search ---
        public string? SearchTerm { get; set; }
        public int? ParentCategoryId { get; set; }              // filter by parent
        public IEnumerable<CategoryViewModel> ParentCategories { get; set; } = new List<CategoryViewModel>(); // để render dropdown
        public bool? IsActive { get; set; }                     // null = tất cả, true = chỉ active, false = chỉ inactive

        // --- Paging ---
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 1;
        public IEnumerable<int> PageSizeOptions { get; } = new[] { 10, 25, 50, 100 };

        // --- Sorting ---
        public string SortBy { get; set; } = "name";
        public string SortOrder { get; set; } = "asc";
        public IReadOnlyDictionary<string, string> SortOptions { get; } = new Dictionary<string, string>
        {
            { "name", "Tên A-Z" },
            { "name_desc", "Tên Z-A" },
            { "books_count", "Số sách (Tăng dần)" },
            { "books_count_desc", "Số sách (Giảm dần)" },
            { "created", "Ngày tạo (Cũ → Mới)" },
            { "created_desc", "Ngày tạo (Mới → Cũ)" },
            { "order", "Thứ tự hiển thị" }
        };
    }
}