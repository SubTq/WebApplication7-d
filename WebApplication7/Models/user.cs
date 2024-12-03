using System.ComponentModel.DataAnnotations;

namespace WebApplication7.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public ICollection<Property> Properties { get; set; } = new List<Property>();

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
