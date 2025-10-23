using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BookStoreMVC.Models.ViewModels
{
    public class BlogPostViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        [Required]
        public string Summary { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public int ReadTimeMinutes { get; set; }

        public string HeroImageUrl { get; set; } = string.Empty;

        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();

        public string FormattedPublishedDate => PublishedAt.ToLocalTime().ToString("dd MMMM yyyy");
    }

    public class BlogIndexViewModel
    {
        public IEnumerable<BlogPostViewModel> FeaturedPosts { get; set; } = Enumerable.Empty<BlogPostViewModel>();

        public IEnumerable<BlogPostViewModel> LatestPosts { get; set; } = Enumerable.Empty<BlogPostViewModel>();

        public IEnumerable<string> PopularTags { get; set; } = Enumerable.Empty<string>();
    }
}