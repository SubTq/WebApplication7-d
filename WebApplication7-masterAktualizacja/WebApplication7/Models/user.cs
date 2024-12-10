using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BCrypt.Net;

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

        // Normalized email for case-insensitive comparison
        public string NormalizedEmail { get; set; }


        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public ICollection<Property> Properties { get; set; } = new List<Property>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // Metoda ustawiania hasła z użyciem BCrypt
        public void SetPassword(string password)
        {
            this.Password = BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Zmodyfikowana metoda weryfikacji hasła
        public bool VerifyPassword(string password)
        {
            try
            {
                // Próba weryfikacji hasła za pomocą BCrypt
                return BCrypt.Net.BCrypt.Verify(password, this.Password);
            }
            catch
            {
                // Jeśli weryfikacja nie powiedzie się (np. hasło w innym formacie), użyj starszego mechanizmu
                return HashPassword(password) == this.Password;
            }
        }


        // Starszy mechanizm hashowania haseł (np. SHA256)
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
