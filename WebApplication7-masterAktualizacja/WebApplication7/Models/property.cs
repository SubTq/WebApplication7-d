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

        // Using RegularExpression for more flexible validation
        [RegularExpression(@"^(https?:\/\/)?([\w\-])+\.{1}([a-zA-Z]{2,63})([\/\w \.-]*)*\/?$", ErrorMessage = "Please enter a valid URL.")]
        public string ImageUrl { get; set; } = string.Empty;

        [RegularExpression(@"^(https?:\/\/)?([\w\-])+\.{1}([a-zA-Z]{2,63})([\/\w \.-]*)*\/?$", ErrorMessage = "Please enter a valid URL.")]
        public string AdditionalImageUrl1 { get; set; } = string.Empty;

        [RegularExpression(@"^(https?:\/\/)?([\w\-])+\.{1}([a-zA-Z]{2,63})([\/\w \.-]*)*\/?$", ErrorMessage = "Please enter a valid URL.")]
        public string AdditionalImageUrl2 { get; set; } = string.Empty;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
