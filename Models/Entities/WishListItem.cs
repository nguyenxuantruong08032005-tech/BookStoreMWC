using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models.Entities
{
    public class WishListItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int BookId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Book Book { get; set; } = null!;
    }
}