using System.ComponentModel.DataAnnotations;

namespace WebApplication7.Models
{
    public class RegisterModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;
    }
}
