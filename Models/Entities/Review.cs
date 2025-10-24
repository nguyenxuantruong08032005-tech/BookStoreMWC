using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models.Entities
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int BookId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string? Comment { get; set; }

        public bool IsVerifiedPurchase { get; set; } = false;

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Book Book { get; set; } = null!;
    }
}