using System.Collections.Generic;
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
        public string? ContactNumber { get; set; } // Zmienione na nullable

        public string? Description { get; set; } // Zmienione na nullable

        [Required(ErrorMessage = "Main image URL is required.")]
        [RegularExpression(@"^(https?:\/\/)[\w\-]+(\.[\w\-]+)+(:\d+)?(\/[\w\-\.]*)*(\?.*)?$", ErrorMessage = "Please enter a valid URL starting with http:// or https://.")]
        public string ImageUrl { get; set; } = "https://example.com/default-image.jpg";

        [RegularExpression(@"^(https?:\/\/)[\w\-]+(\.[\w\-]+)+(:\d+)?(\/[\w\-\.]*)*(\?.*)?$", ErrorMessage = "Please enter a valid URL starting with http:// or https://.")]
        public string? AdditionalImageUrl1 { get; set; } = null;

        [RegularExpression(@"^(https?:\/\/)[\w\-]+(\.[\w\-]+)+(:\d+)?(\/[\w\-\.]*)*(\?.*)?$", ErrorMessage = "Please enter a valid URL starting with http:// or https://.")]
        public string? AdditionalImageUrl2 { get; set; } = null;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }

}
