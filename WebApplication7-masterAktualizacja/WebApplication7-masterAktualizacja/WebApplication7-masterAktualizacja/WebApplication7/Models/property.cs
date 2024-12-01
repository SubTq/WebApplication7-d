using System.ComponentModel.DataAnnotations;

namespace WebApplication7.Models
{
    public class Property
    {
        public int PropertyId { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        [Required]
        public int OwnerUserId { get; set; }
        public User? OwnerUser { get; set; }

        [Phone]
        public string ContactNumber { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Url]
        public string ImageUrl { get; set; } = string.Empty;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
