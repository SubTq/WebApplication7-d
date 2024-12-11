using System.ComponentModel.DataAnnotations;

namespace WebApplication7.Models
{
    public class EditUserModel
    {
        public int UserId { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string? ContactNumber { get; set; }
    }

}
