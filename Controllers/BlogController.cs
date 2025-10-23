using System;
using System.Collections.Generic;
using System.Linq;
using BookStoreMVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookStoreMVC.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult Index()
        {
            var posts = GetSamplePosts().ToList();

            var model = new BlogIndexViewModel
            {
                FeaturedPosts = posts.Take(2).ToList(),
                LatestPosts = posts.Skip(2).ToList(),
                PopularTags = new[]
                {
                    "Thói quen đọc", "Review sách", "Kỹ năng", "Sống chậm", "Văn học Việt"
                }
            };

            ViewBag.PageTitle = "Blog BookVerse - Cảm hứng đọc sách mỗi ngày";
            ViewBag.MetaDescription = "Khám phá những bài viết mới nhất về kinh nghiệm đọc sách, review tác phẩm và các xu hướng xuất bản.";

            return View(model);
        }

        private static IEnumerable<BlogPostViewModel> GetSamplePosts()
        {
            return new List<BlogPostViewModel>
            {
                new()
                {
                    Title = "7 thói quen đọc sách hiệu quả giúp bạn duy trì cảm hứng",
                    Slug = "7-thoi-quen-doc-sach-hieu-qua",
                    Summary = "Cùng BookVerse khám phá những bí quyết nhỏ để biến việc đọc sách trở thành một phần thú vị trong lịch trình bận rộn của bạn.",
                    Category = "Kỹ năng đọc",
                    Author = "Đội ngũ BookVerse",
                    PublishedAt = DateTime.UtcNow.AddDays(-2),
                    ReadTimeMinutes = 6,
                    HeroImageUrl = "/images/blog/reading-habits.jpg",
                    Tags = new[] { "Kỹ năng đọc", "Thói quen tốt", "BookVerse Tips" }
                },
                new()
                {
                    Title = "Top 5 tiểu thuyết Việt Nam nên đọc một lần trong đời",
                    Slug = "top-5-tieu-thuyet-viet-nam",
                    Summary = "Từ 'Tôi thấy hoa vàng trên cỏ xanh' đến 'Cánh đồng bất tận', đây là những tựa sách gợi nhiều suy ngẫm và cảm xúc cho độc giả Việt.",
                    Category = "Gợi ý sách hay",
                    Author = "Lan Chi",
                    PublishedAt = DateTime.UtcNow.AddDays(-5),
                    ReadTimeMinutes = 8,
                    HeroImageUrl = "/images/blog/vietnamese-novels.jpg",
                    Tags = new[] { "Văn học Việt", "Gợi ý sách hay" }
                },
                new()
                {
                    Title = "Bí kíp ghi chú khi đọc sách giúp ghi nhớ lâu hơn",
                    Slug = "bi-kip-ghi-chu-khi-doc-sach",
                    Summary = "Ghi chú thông minh giúp bạn kết nối kiến thức và áp dụng ngay vào thực tế. Cùng tìm hiểu các phương pháp ghi chú phổ biến.",
                    Category = "Phương pháp học",
                    Author = "Minh An",
                    PublishedAt = DateTime.UtcNow.AddDays(-9),
                    ReadTimeMinutes = 7,
                    HeroImageUrl = "/images/blog/note-taking.jpg",
                    Tags = new[] { "Học tập", "Ghi chú", "Productivity" }
                },
                new()
                {
                    Title = "5 cuốn sách self-help mang đến năng lượng tích cực cho buổi sáng",
                    Slug = "5-cuon-self-help-cho-buoi-sang",
                    Summary = "Bắt đầu ngày mới đầy hứng khởi với những trang sách truyền cảm hứng và thực tiễn mà bạn không nên bỏ lỡ.",
                    Category = "Self-help",
                    Author = "BookVerse Team",
                    PublishedAt = DateTime.UtcNow.AddDays(-12),
                    ReadTimeMinutes = 5,
                    HeroImageUrl = "/images/blog/morning-books.jpg",
                    Tags = new[] { "Self-help", "Năng lượng tích cực" }
                }
            };
        }
    }
}
