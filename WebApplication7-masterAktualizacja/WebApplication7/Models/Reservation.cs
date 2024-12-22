using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication7.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public Property? Property { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        [CurrentOrFutureDate(ErrorMessage = "Start Date cannot be in the past.")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        [CurrentOrFutureDate(ErrorMessage = "End Date cannot be in the past.")]
        public DateTime EndDate { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public string? OwnerNote { get; set; } = "";

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int? Rating { get; set; }
    }

    public class CurrentOrFutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            DateTime date = Convert.ToDateTime(value);
            return date >= DateTime.Now.Date;
        }
    }
}
