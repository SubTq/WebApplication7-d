using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication7.Data;
using WebApplication7.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Mail;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApplication7.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var normalizedEmail = model.Email.Trim().ToUpperInvariant();
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
                if (existingUser != null)
                {
                    ViewData["ErrorMessage"] = "An account with this email already exists.";
                    return View(model);
                }

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    ContactNumber = model.ContactNumber, // Contact number added
                    NormalizedEmail = model.Email.Trim().ToUpperInvariant()
                };
                user.SetPassword(model.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email)
        };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }


        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["ErrorMessage"] = "Email and password are required.";
                return View();
            }

            var normalizedEmail = email.Trim().ToUpperInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user == null)
            {
                ViewData["ErrorMessage"] = "No account found with this email.";
                return View();
            }
            else if (!user.VerifyPassword(password))
            {
                ViewData["ErrorMessage"] = "Incorrect password.";
                return View();
            }

            // Tworzenie listy roszczeń (claims)
            var claims = new List<Claim>
{
    new(ClaimTypes.Name, user.Email),
    new(ClaimTypes.Email, user.Email),
    new(ClaimTypes.NameIdentifier, user.UserId.ToString())
};

            // Jeśli użytkownik jest administratorem, dodaj rolę "Admin"
            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            // Utwórz tożsamość i zaloguj użytkownika
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Przekierowanie na stronę główną po zalogowaniu
            return RedirectToAction("Index", "Home");
        }


        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewData["ErrorMessage"] = "Email is required.";
                return View();
            }

            try
            {
                var normalizedEmail = email.Trim().ToUpperInvariant();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

                if (user == null)
                {
                    ViewData["SuccessMessage"] = "If the email exists, a reset link has been sent. Please check your email.";
                    return View();
                }

                user.PasswordResetToken = GenerateResetToken();
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                SendResetEmail(user.Email, user.PasswordResetToken);
                ViewData["SuccessMessage"] = "If the email exists, a reset link has been sent. Please check your email.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during password reset.");
                ViewData["ErrorMessage"] = "An error occurred. Please try again later.";
            }

            return View();
        }

        // GET: Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid token.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                return BadRequest("Invalid or expired token.");
            }

            return View(new ResetPasswordModel { Token = token });
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ErrorMessage"] = "Invalid data.";
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == model.Token && u.PasswordResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                ViewData["ErrorMessage"] = "Invalid or expired token.";
                return View(model);
            }

            user.SetPassword(model.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            ViewData["SuccessMessage"] = "Your password has been reset successfully.";
            return RedirectToAction("Login");
        }

        private static string GenerateResetToken()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private void SendResetEmail(string toEmail, string token)
        {
            string resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme)
                ?? throw new InvalidOperationException("Unable to generate reset link.");

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email cannot be null or empty.", nameof(toEmail));
            }

            var mailMessage = new MailMessage()
            {
                From = new("houserentproject@gmail.com", "RentHouse Support"),
                Subject = "Reset Password",
                Body = $"Click the link to reset your password: <a href='{resetLink}'>Reset Password</a>",
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            try
            {
                using var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("houserentproject@gmail.com", "nzse ndli tcnm lljo"),
                    EnableSsl = true
                };
                smtpClient.Send(mailMessage);
                _logger.LogInformation("Password reset email successfully sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send reset email to {Email}: {ErrorMessage}", toEmail, ex.Message);
                throw;
            }
        }


        // GET: Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return NotFound("User not found.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return View(user);
        }

        // GET: Account/Edit
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return NotFound("User not found.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var editModel = new EditUserModel
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ContactNumber = user.ContactNumber
            };

            return View(editModel);
        }


        // POST: Account/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(EditUserModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Nieprawidłowy stan modelu podczas edycji profilu.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Błąd walidacji: {ErrorMessage}", error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                var currentUserEmail = User.Identity?.Name;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    TempData["ErrorMessage"] = "Unable to determine the current user email.";
                    return RedirectToAction("Index", "Home");
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);

                if (existingUser == null || existingUser.UserId != model.UserId)
                {
                    _logger.LogWarning("Nieautoryzowana próba edycji profilu przez {Email}.", currentUserEmail);
                    return Unauthorized("Nieautoryzowany dostęp.");
                }

                // Sprawdź unikalność e-maila, jeśli został zmieniony
                if (!string.Equals(existingUser.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "Ten adres e-mail jest już zarejestrowany.");
                        return View(model);
                    }
                }

                // Zaktualizuj dane użytkownika
                existingUser.FirstName = model.FirstName;
                existingUser.LastName = model.LastName;
                existingUser.ContactNumber = model.ContactNumber;
                existingUser.Email = model.Email;
                existingUser.NormalizedEmail = model.Email.Trim().ToUpperInvariant();

                _context.Update(existingUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profil został pomyślnie zaktualizowany dla {Email}.", existingUser.Email);
                TempData["SuccessMessage"] = "Twój profil został pomyślnie zaktualizowany.";
                return RedirectToAction(nameof(Profile));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Wystąpił błąd współbieżności podczas aktualizacji profilu użytkownika.");
                ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji Twojego profilu. Spróbuj ponownie.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił nieoczekiwany błąd podczas aktualizacji profilu użytkownika.");
                ModelState.AddModelError("", "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.");
                return View(model);
            }
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (User.Identity != null && User.Identity.IsAuthenticated && !string.IsNullOrWhiteSpace(User.Identity.Name))
            {
                var currentUserEmail = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(u => u.Email == currentUserEmail);

                ViewData["IsAdmin"] = user?.IsAdmin ?? false;
            }
            else
            {
                ViewData["IsAdmin"] = false;
            }
        }




    }

}





