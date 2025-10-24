// Services/IReviewService.cs & ReviewService.cs
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Models.ViewModels;

namespace BookStoreMVC.Services
{
    public interface IReviewService
    {
        Task<ReviewViewModel?> GetReviewByIdAsync(int reviewId);
        Task<(IEnumerable<ReviewViewModel> Reviews, int TotalCount)> GetReviewsAsync(ReviewListViewModel model);
        Task<IEnumerable<ReviewViewModel>> GetReviewsByBookAsync(int bookId, int count = 10);
        Task<IEnumerable<ReviewViewModel>> GetReviewsByUserAsync(string userId, int count = 10);
        Task<ReviewViewModel> CreateReviewAsync(string userId, ReviewCreateViewModel model);
        Task<bool> UpdateReviewAsync(int reviewId, string userId, ReviewCreateViewModel model);
        Task<bool> DeleteReviewAsync(int reviewId, string userId);
        Task<bool> ApproveReviewAsync(int reviewId);
        Task<bool> RejectReviewAsync(int reviewId);
        Task<bool> CanUserReviewBookAsync(string userId, int bookId);
        Task<(double AverageRating, int[] RatingDistribution)> GetBookRatingStatsAsync(int bookId);
    }

    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReviewViewModel?> GetReviewByIdAsync(int reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Book)
                    .ThenInclude(b => b.Category)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            return review != null ? MapToViewModel(review) : null;
        }

        public async Task<(IEnumerable<ReviewViewModel> Reviews, int TotalCount)> GetReviewsAsync(ReviewListViewModel model)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Book)
                    .ThenInclude(b => b.Category)
                .AsQueryable();

            // Apply filters
            if (model.BookId.HasValue)
                query = query.Where(r => r.BookId == model.BookId);

            if (model.Rating.HasValue)
                query = query.Where(r => r.Rating == model.Rating);

            if (!model.ShowUnapproved)
                query = query.Where(r => r.IsApproved);

            // Apply sorting
            query = model.SortBy.ToLower() switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "highest_rating" => query.OrderByDescending(r => r.Rating),
                "lowest_rating" => query.OrderBy(r => r.Rating),
                _ => query.OrderByDescending(r => r.CreatedAt) // newest first
            };

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            return (reviews.Select(MapToViewModel), totalCount);
        }

        public async Task<IEnumerable<ReviewViewModel>> GetReviewsByBookAsync(int bookId, int count = 10)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Book)
                .Where(r => r.BookId == bookId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();

            return reviews.Select(MapToViewModel);
        }

        public async Task<IEnumerable<ReviewViewModel>> GetReviewsByUserAsync(string userId, int count = 10)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Book)
                    .ThenInclude(b => b.Category)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();

            return reviews.Select(MapToViewModel);
        }

        public async Task<ReviewViewModel> CreateReviewAsync(string userId, ReviewCreateViewModel model)
        {
            // Kiểm tra xem người dùng có thể đánh giá hay không
            var canReview = await CanUserReviewBookAsync(userId, model.BookId);
            if (!canReview)
                throw new InvalidOperationException("You cannot review this book");

            // Kiểm tra xem người dùng đã đánh giá cuốn sách này chưa
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == model.BookId);

            if (existingReview != null)
                throw new InvalidOperationException("You have already reviewed this book");

            var review = new Review
            {
                UserId = userId,
                BookId = model.BookId,
                Rating = model.Rating,
                Comment = model.Comment,
                IsVerifiedPurchase = await HasUserPurchasedBookAsync(userId, model.BookId),
                IsApproved = true, // Tự động phê duyệt tạm thời
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return await GetReviewByIdAsync(review.Id) ?? throw new InvalidOperationException();
        }

        public async Task<bool> UpdateReviewAsync(int reviewId, string userId, ReviewCreateViewModel model)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null) return false;

            review.Rating = model.Rating;
            review.Comment = model.Comment;
            review.IsApproved = false; // Yêu cầu phê duyệt lại sau khi chỉnh sửa

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, string userId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveReviewAsync(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

            review.IsApproved = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectReviewAsync(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

            review.IsApproved = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUserReviewBookAsync(string userId, int bookId)
        {
           // Người dùng đã đăng nhập có thể đánh giá miễn là chưa có đánh giá trước đó cho cuốn sách này.
            // Nếu họ đã mua sách, đánh giá sẽ được gắn cờ là "Đã mua" ở bước tạo.
            return !await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.BookId == bookId);

           
        }

        public async Task<(double AverageRating, int[] RatingDistribution)> GetBookRatingStatsAsync(int bookId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.BookId == bookId && r.IsApproved)
                .ToListAsync();

            if (!reviews.Any())
                return (0, new int[5]);

            var averageRating = reviews.Average(r => r.Rating);
            var distribution = Enumerable.Range(1, 5)
                .Select(i => reviews.Count(r => r.Rating == i))
                .ToArray();

            return (averageRating, distribution);
        }

        private async Task<bool> HasUserPurchasedBookAsync(string userId, int bookId)
        {
            return await _context.OrderItems
                .AnyAsync(oi => oi.Order.UserId == userId &&
                               oi.BookId == bookId &&
                               oi.Order.Status == OrderStatus.Delivered);
        }

        private ReviewViewModel MapToViewModel(Review review)
        {
            return new ReviewViewModel
            {
                Id = review.Id,
                UserId = review.UserId,
                BookId = review.BookId,
                Rating = review.Rating,
                Comment = review.Comment,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                IsApproved = review.IsApproved,
                CreatedAt = review.CreatedAt,
                User = review.User,
                Book = review.Book
            };
        }


    }
}