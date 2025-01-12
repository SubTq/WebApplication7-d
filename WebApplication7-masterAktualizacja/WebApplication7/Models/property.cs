using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication7.Models
{
    public class Property
    {
        public int PropertyId { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required.")]
        [MaxLength(100, ErrorMessage = "Type cannot exceed 100 characters.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [MaxLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = string.Empty;

        [Required(ErrorMessage = "Owner is required.")]
        public int OwnerUserId { get; set; }
        public User? OwnerUser { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string? ContactNumber { get; set; } 

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; } 

        [Required(ErrorMessage = "Main image URL is required.")]
        [RegularExpression(@"^(https?:\/\/)[\w\-]+(\.[\w\-]+)+(:\d+)?(\/[\w\-\.]*)*(\?.*)?$", ErrorMessage = "Please enter a valid URL starting with http:// or https://.")]
        public string ImageUrl { get; set; } = "https://example.com/default-image.jpg";

        [RegularExpression(@"^(https?:\/\/)[\w\-]+(\.[\w\-]+)+(:\d+)?(\/[\w\-\.]*)*(\?.*)?$", ErrorMessage = "Please enter a valid URL starting with http:// or https://.")]
        public string? AdditionalImageUrl1 { get; set; } = null;

        [RegularExpression(@"^(https?:\/\/)[\w\-]+(\.[\w\-]+)+(:\d+)?(\/[\w\-\.]*)*(\?.*)?$", ErrorMessage = "Please enter a valid URL starting with http:// or https://.")]
        public string? AdditionalImageUrl2 { get; set; } = null;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        
        [NotMapped]
        public double AverageRating =>
            Reservations != null && Reservations.Any(r => r.Rating.HasValue)
                ? Reservations.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating.Value) 
                : 0.0;

        [NotMapped]
        public int RatingsCount =>
            Reservations != null
                ? Reservations.Count(r => r.Rating.HasValue)
                : 0; 

    }
}
