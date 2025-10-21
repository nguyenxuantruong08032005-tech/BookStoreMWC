using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models.Entities
{
    public class ShippingInfo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}