// Services/IBookService.cs & BookService.cs
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;

namespace BookStoreMVC.Services
{
    public interface IBookService
    {
        // Book CRUD operations: lấy, thêm, sửa, xóa, cập nhật tồn kho.
        Task<BookViewModel?> GetBookByIdAsync(int id);
        Task<BookDetailsViewModel> GetBookDetailsAsync(int id, string? userId = null);
        Task<(IEnumerable<BookViewModel> Books, int TotalCount)> GetBooksAsync(BookListViewModel model);
        Task<IEnumerable<BookViewModel>> SearchBooksAsync(string searchTerm, int maxResults = 20);
        Task<bool> DeleteBookAsync(int id);
        Task<bool> UpdateStockAsync(int bookId, int quantity);
        Task<BookViewModel> CreateBookAsync(BookViewModel model, IFormFile? imageFile);
        Task<BookViewModel> UpdateBookAsync(BookViewModel model, IFormFile? imageFile);
        Task<bool> UpdateBookImageAsync(int bookId, IFormFile imageFile);
        Task<bool> RemoveBookImageAsync(int bookId);


        // Truy vấn sách nổi bật
        Task<IEnumerable<BookViewModel>> GetFeaturedBooksAsync(int count = 8);
        Task<IEnumerable<BookViewModel>> GetNewBooksAsync(int count = 8);
        Task<IEnumerable<BookViewModel>> GetBestSellersAsync(int count = 8);
        Task<IEnumerable<BookViewModel>> GetRelatedBooksAsync(int bookId, int count = 4);
        Task<IEnumerable<BookViewModel>> GetRecommendedBooksAsync(string userId, int count = 8);

        // Truy vấn theo danh mục
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<IEnumerable<CategoryViewModel>> GetCategoriesWithStatsAsync();
        Task<CategoryViewModel?> GetCategoryByIdAsync(int id);
        Task<CategoryViewModel> CreateCategoryAsync(CategoryViewModel model);
        Task<CategoryViewModel> UpdateCategoryAsync(CategoryViewModel model);
        Task<bool> DeleteCategoryAsync(int id);

        //
        Task<List<BookGalleryImage>> GetBookGalleryImagesAsync(int bookId);
        Task<int> UploadGalleryImagesAsync(int bookId, List<IFormFile> imageFiles);
        Task<bool> RemoveGalleryImageAsync(int imageId);
    }

    public partial class BookService : IBookService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookService> _logger;
        private readonly IFileUploadService _fileUploadService;


        public BookService(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            ILogger<BookService> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        public async Task<BookViewModel?> GetBookByIdAsync(int id)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.User)
                .Include(b => b.GalleryImages) // ✅ Load gallery images từ bảng
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

            if (book == null) return null;

            // ✅ Sync: Nếu AdditionalImages rỗng nhưng có GalleryImages, convert sang JSON
            if (string.IsNullOrEmpty(book.AdditionalImages) && book.GalleryImages.Any())
            {
                var galleryUrls = book.GalleryImages
                    .Where(img => img.IsActive)
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => img.ImageUrl)
                    .Where(url => !string.IsNullOrEmpty(url))
                    .ToList();

                if (galleryUrls.Any())
                {
                    book.AdditionalImages = System.Text.Json.JsonSerializer.Serialize(galleryUrls);
                }
            }

            return MapToViewModel(book);
        }

        public async Task<BookDetailsViewModel> GetBookDetailsAsync(int id, string? userId = null)
        {
            var book = await GetBookByIdAsync(id);
            if (book == null)
                throw new ArgumentException("Book not found", nameof(id));

            var relatedBooks = await GetRelatedBooksAsync(id, 4);

            var result = new BookDetailsViewModel
            {
                Book = book,
                RelatedBooks = relatedBooks
            };

            if (!string.IsNullOrEmpty(userId))
            {
                result.CanReview = await CanUserReviewBookAsync(userId, id);
                result.HasPurchased = await HasUserPurchasedBookAsync(userId, id);
                result.AlreadyReviewed = await HasUserReviewedBookAsync(userId, id);
                result.IsInWishlist = await IsBookInUserWishlistAsync(userId, id);
                result.CartQuantity = await GetBookQuantityInCartAsync(userId, id);
            }

            return result;
        }

        public async Task<(IEnumerable<BookViewModel> Books, int TotalCount)> GetBooksAsync(BookListViewModel model)
        {
            var query = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Where(b => b.IsActive);

            // Áp dụng bộ lọc
            if (model.CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == model.CategoryId);

            if (!string.IsNullOrEmpty(model.SearchTerm))
                query = query.Where(b =>
                    b.Title.Contains(model.SearchTerm) ||
                    b.Author.Contains(model.SearchTerm) ||
                    b.Description!.Contains(model.SearchTerm));

            if (model.MinPrice.HasValue)
                query = query.Where(b => (b.DiscountPrice ?? b.Price) >= model.MinPrice);

            if (model.MaxPrice.HasValue)
                query = query.Where(b => (b.DiscountPrice ?? b.Price) <= model.MaxPrice);

            if (!string.IsNullOrEmpty(model.Author))
                query = query.Where(b => b.Author.Contains(model.Author));

            if (!string.IsNullOrEmpty(model.Publisher))
                query = query.Where(b => b.Publisher!.Contains(model.Publisher));

            if (!string.IsNullOrEmpty(model.Language))
                query = query.Where(b => b.Language == model.Language);

            if (model.InStock)
                query = query.Where(b => b.StockQuantity > 0);

            if (model.MinRating.HasValue)
                query = query.Where(b => b.Reviews.Any() && b.Reviews.Average(r => r.Rating) >= model.MinRating);

            // Áp dụng bộ sắp xếp
            query = model.SortBy.ToLower() switch
            {
                "title" => model.SortOrder == "desc" ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
                "author" => model.SortOrder == "desc" ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
                "price" => model.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.DiscountPrice ?? b.Price)
                    : query.OrderBy(b => b.DiscountPrice ?? b.Price),
                "rating" => model.SortOrder == "desc"
                    ? query.OrderByDescending(b => b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0)
                    : query.OrderBy(b => b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0),
                "newest" => query.OrderByDescending(b => b.CreatedAt),
                "oldest" => query.OrderBy(b => b.CreatedAt),
                "popularity" => query.OrderByDescending(b => b.Reviews.Count),
                _ => query.OrderBy(b => b.Title)
            };

            var totalCount = await query.CountAsync();

            var books = await query
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            return (books.Select(MapToViewModel), totalCount);
        }

        public async Task<IEnumerable<BookViewModel>> GetFeaturedBooksAsync(int count = 8)
        {
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Where(b => b.IsActive && b.StockQuantity > 0)
                .OrderByDescending(b => b.Reviews.Count)
                .ThenByDescending(b => b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0)
                .Take(count)
                .ToListAsync();

            return books.Select(MapToViewModel);
        }

        public async Task<IEnumerable<BookViewModel>> GetNewBooksAsync(int count = 8)
        {
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Where(b => b.IsActive && b.StockQuantity > 0)
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();

            return books.Select(MapToViewModel);
        }

        public async Task<IEnumerable<BookViewModel>> GetBestSellersAsync(int count = 8)
        {
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Include(b => b.OrderItems)
                .Where(b => b.IsActive && b.StockQuantity > 0)
                .OrderByDescending(b => b.OrderItems.Sum(oi => oi.Quantity))
                .Take(count)
                .ToListAsync();

            return books.Select(MapToViewModel);
        }

        public async Task<IEnumerable<BookViewModel>> GetRelatedBooksAsync(int bookId, int count = 4)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return Enumerable.Empty<BookViewModel>();

            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Where(b => b.IsActive && b.Id != bookId && b.CategoryId == book.CategoryId)
                .OrderByDescending(b => b.Reviews.Count)
                .Take(count)
                .ToListAsync();

            return books.Select(MapToViewModel);
        }

        public async Task<BookViewModel> CreateBookAsync(BookViewModel model, IFormFile? imageFile)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var book = new Book
                {
                    Title = model.Title,
                    Author = model.Author,
                    Description = model.Description,
                    Price = model.Price,
                    DiscountPrice = model.DiscountPrice,
                    StockQuantity = model.StockQuantity,
                    CategoryId = model.CategoryId,
                    Publisher = model.Publisher,
                    PublishDate = model.PublishDate,
                    PageCount = model.PageCount,
                    Language = model.Language,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Handle image upload
                if (imageFile != null)
                {
                    var uploadResult = await _fileUploadService.UploadImageAsync(imageFile, "books");
                    if (uploadResult.Success)
                    {
                        book.ImageUrl = uploadResult.ImageUrl;
                        book.ImageFileName = uploadResult.FileName;
                        book.ImageContentType = uploadResult.ContentType;
                        book.ImageFileSize = uploadResult.FileSize;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to upload image for book {Title}: {Error}",
                            model.Title, uploadResult.ErrorMessage);
                    }
                }

                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToViewModel(book);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BookViewModel> UpdateBookAsync(BookViewModel model, IFormFile? imageFile)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var book = await _context.Books.FindAsync(model.Id);
                if (book == null)
                    throw new ArgumentException("Book not found", nameof(model));

                // Update book properties
                book.Title = model.Title;
                book.Author = model.Author;
                book.Description = model.Description;
                book.Price = model.Price;
                book.DiscountPrice = model.DiscountPrice;
                book.StockQuantity = model.StockQuantity;
                book.CategoryId = model.CategoryId;
                book.Publisher = model.Publisher;
                book.PublishDate = model.PublishDate;
                book.PageCount = model.PageCount;
                book.Language = model.Language;
                book.IsActive = model.IsActive;
                book.UpdatedAt = DateTime.UtcNow;

                // Handle image upload/removal
                if (model.RemoveImage && !string.IsNullOrEmpty(book.ImageUrl))
                {
                    await _fileUploadService.DeleteImageAsync(book.ImageUrl);
                    book.ImageUrl = null;
                    book.ImageFileName = null;
                    book.ImageContentType = null;
                    book.ImageFileSize = null;
                }
                else if (imageFile != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(book.ImageUrl))
                    {
                        await _fileUploadService.DeleteImageAsync(book.ImageUrl);
                    }

                    // Upload new image
                    var uploadResult = await _fileUploadService.UploadImageAsync(imageFile, "books");
                    if (uploadResult.Success)
                    {
                        book.ImageUrl = uploadResult.ImageUrl;
                        book.ImageFileName = uploadResult.FileName;
                        book.ImageContentType = uploadResult.ContentType;
                        book.ImageFileSize = uploadResult.FileSize;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to upload image for book {Title}: {Error}",
                            model.Title, uploadResult.ErrorMessage);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToViewModel(book);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateBookImageAsync(int bookId, IFormFile imageFile)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null) return false;

                // Delete old image if exists
                if (!string.IsNullOrEmpty(book.ImageUrl))
                {
                    await _fileUploadService.DeleteImageAsync(book.ImageUrl);
                }

                // Upload new image
                var uploadResult = await _fileUploadService.UploadImageAsync(imageFile, "books");
                if (uploadResult.Success)
                {
                    book.ImageUrl = uploadResult.ImageUrl;
                    book.ImageFileName = uploadResult.FileName;
                    book.ImageContentType = uploadResult.ContentType;
                    book.ImageFileSize = uploadResult.FileSize;
                    book.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book image for book {BookId}", bookId);
                return false;
            }
        }

        public async Task<bool> RemoveBookImageAsync(int bookId)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null || string.IsNullOrEmpty(book.ImageUrl))
                    return false;

                await _fileUploadService.DeleteImageAsync(book.ImageUrl);

                book.ImageUrl = null;
                book.ImageFileName = null;
                book.ImageContentType = null;
                book.ImageFileSize = null;
                book.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing book image for book {BookId}", bookId);
                return false;
            }
        }

        public async Task<bool> DeleteBookAsync(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return false;

            book.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // Phương thức trợ giúp
        private BookViewModel MapToViewModel(Book book)
        {
            return new BookViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Description = book.Description,
                Price = book.Price,
                DiscountPrice = book.DiscountPrice,
                StockQuantity = book.StockQuantity,
                CategoryId = book.CategoryId,
                Publisher = book.Publisher,
                PublishDate = book.PublishDate,
                PageCount = book.PageCount,
                Language = book.Language,
                IsActive = book.IsActive,
                ImageUrl = book.ImageUrl,
                ImageFileName = book.ImageFileName,
                ImageContentType = book.ImageContentType,
                ImageFileSize = book.ImageFileSize,
                AdditionalImages = book.AdditionalImages, // ✅ FIX: Map gallery images JSON
                Category = book.Category,
                Reviews = book.Reviews
            };
        }

        private async Task<bool> CanUserReviewBookAsync(string userId, int bookId)
        {
            return await HasUserPurchasedBookAsync(userId, bookId) &&
                   !await HasUserReviewedBookAsync(userId, bookId);
        }

        private async Task<bool> HasUserPurchasedBookAsync(string userId, int bookId)
        {
            return await _context.OrderItems
                .AnyAsync(oi => oi.Order.UserId == userId &&
                               oi.BookId == bookId &&
                               oi.Order.Status == OrderStatus.Delivered);
        }

        private async Task<bool> HasUserReviewedBookAsync(string userId, int bookId)
        {
            return await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.BookId == bookId);
        }

        private async Task<bool> IsBookInUserWishlistAsync(string userId, int bookId)
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.BookId == bookId);
        }

        private async Task<int> GetBookQuantityInCartAsync(string userId, int bookId)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId);
            return cartItem?.Quantity ?? 0;
        }

        // Phương pháp phân loại
        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<CategoryViewModel>> GetCategoriesWithStatsAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.Books)
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return categories.Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                ParentCategoryId = c.ParentCategoryId,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                ParentCategory = c.ParentCategory,
                SubCategories = c.SubCategories,
                Books = c.Books
            });
        }

        public async Task<CategoryViewModel?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return null;

            return new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                ParentCategoryId = category.ParentCategoryId,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                ParentCategory = category.ParentCategory,
                SubCategories = category.SubCategories,
                Books = category.Books
            };
        }

        public async Task<CategoryViewModel> CreateCategoryAsync(CategoryViewModel model)
        {
            var category = new Category
            {
                Name = model.Name,
                ParentCategoryId = model.ParentCategoryId,
                Description = model.Description,
                ImageUrl = model.ImageUrl,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return await GetCategoryByIdAsync(category.Id) ?? throw new InvalidOperationException();
        }

        public async Task<CategoryViewModel> UpdateCategoryAsync(CategoryViewModel model)
        {
            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null)
                throw new ArgumentException("Category not found", nameof(model));

            category.Name = model.Name;
            category.ParentCategoryId = model.ParentCategoryId;
            category.Description = model.Description;
            category.ImageUrl = model.ImageUrl;
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return await GetCategoryByIdAsync(category.Id) ?? throw new InvalidOperationException();
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Books)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return false;

            // Check if category has books or subcategories
            if (category.Books.Any() || category.SubCategories.Any())
                throw new InvalidOperationException("Cannot delete category with books or subcategories");

            category.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // Phương pháp bổ sung
        public async Task<IEnumerable<BookViewModel>> SearchBooksAsync(string searchTerm, int maxResults = 20)
        {
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Where(b => b.IsActive &&
                           (b.Title.Contains(searchTerm) ||
                            b.Author.Contains(searchTerm) ||
                            b.Description!.Contains(searchTerm)))
                .Take(maxResults)
                .ToListAsync();

            return books.Select(MapToViewModel);
        }

        public async Task<IEnumerable<BookViewModel>> GetRecommendedBooksAsync(string userId, int count = 8)
        {
            // Khuyến nghị đơn giản dựa trên lịch sử mua hàng và các danh mục của người dùng
            var userCategories = await _context.OrderItems
                .Where(oi => oi.Order.UserId == userId)
                .Select(oi => oi.Book.CategoryId)
                .Distinct()
                .ToListAsync();

            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Where(b => b.IsActive &&
                           b.StockQuantity > 0 &&
                           userCategories.Contains(b.CategoryId))
                .OrderByDescending(b => b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0)
                .Take(count)
                .ToListAsync();

            return books.Select(MapToViewModel);
        }

        public async Task<bool> UpdateStockAsync(int bookId, int quantity)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return false;

            book.StockQuantity = Math.Max(0, book.StockQuantity + quantity);
            book.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<BookGalleryImage>> GetBookGalleryImagesAsync(int bookId)
        {
            try
            {
                return await _context.BookGalleryImages
                    .Where(img => img.BookId == bookId && img.IsActive)
                    .OrderBy(img => img.DisplayOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gallery images for book {BookId}", bookId);
                return new List<BookGalleryImage>();
            }
        }

        // 2. Upload nhiều ảnh vào gallery
        public async Task<int> UploadGalleryImagesAsync(int bookId, List<IFormFile> imageFiles)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                    return 0;

                var uploadedCount = 0;
                var maxDisplayOrder = await _context.BookGalleryImages
                    .Where(img => img.BookId == bookId)
                    .MaxAsync(img => (int?)img.DisplayOrder) ?? 0;

                foreach (var imageFile in imageFiles)
                {
                    var uploadResult = await _fileUploadService.UploadImageAsync(imageFile, "books/gallery");

                    if (uploadResult.Success)
                    {
                        var galleryImage = new BookGalleryImage
                        {
                            BookId = bookId,
                            ImageUrl = uploadResult.ImageUrl,
                            ImageFileName = uploadResult.FileName,
                            ImageContentType = uploadResult.ContentType,
                            ImageFileSize = uploadResult.FileSize,
                            DisplayOrder = ++maxDisplayOrder,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.BookGalleryImages.Add(galleryImage);
                        uploadedCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to upload gallery image: {Error}", uploadResult.ErrorMessage);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return uploadedCount;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error uploading gallery images for book {BookId}", bookId);
                return 0;
            }
        }

        // 3. Xóa ảnh từ gallery
        public async Task<bool> RemoveGalleryImageAsync(int imageId)
        {
            try
            {
                var galleryImage = await _context.BookGalleryImages.FindAsync(imageId);
                if (galleryImage == null)
                    return false;

                // Xóa file vật lý
                if (!string.IsNullOrEmpty(galleryImage.ImageUrl))
                {
                    await _fileUploadService.DeleteImageAsync(galleryImage.ImageUrl);
                }

                // Xóa record khỏi database
                _context.BookGalleryImages.Remove(galleryImage);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing gallery image {ImageId}", imageId);
                return false;
            }
        }
    }
}