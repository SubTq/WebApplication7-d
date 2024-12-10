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
                    NormalizedEmail = model.Email.Trim().ToUpperInvariant()
                };
                user.SetPassword(model.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email)
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
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
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        private void SendResetEmail(string toEmail, string token)
        {
            string resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);

            var mailMessage = new MailMessage
            {
                From = new MailAddress("no-reply@RentHouse.com"),
                Subject = "Reset Password",
                Body = $"Click the link to reset your password: <a href='{resetLink}'>Reset Password</a>",
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            try
            {
                using (var smtpClient = new SmtpClient("smtp.your-email-provider.com"))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential("your-email", "your-password");
                    smtpClient.EnableSsl = true;
                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send reset email to {toEmail}: {ex.Message}");
                throw;
            }
        }
    }
}
