using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication7.Data;
using WebApplication7.Models;

namespace WebApplication7.Controllers
{
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult AdminPanel()
        {
            return View();
        }


     
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();

            if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value is string userIdString &&
                int.TryParse(userIdString, out int currentUserId))
            {
                ViewBag.CurrentUserId = currentUserId;
            }
            else
            {
              
                TempData["ErrorMessage"] = "Unable to determine the current user ID.";
                return RedirectToAction("Index", "Home");
            }

            return View(users);
        }




      
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Users));
        }

      
        public async Task<IActionResult> Properties()
        {
            var properties = await _context.Properties.ToListAsync();
            return View(properties);
        }

      
        public async Task<IActionResult> DeleteProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToAction(nameof(Properties));
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Property deleted successfully.";
            return RedirectToAction(nameof(Properties));
        }

       
        public async Task<IActionResult> Reservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Property)
                .ToListAsync();
            return View(reservations);
        }

        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction(nameof(Reservations));
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation deleted successfully.";
            return RedirectToAction(nameof(Reservations));
        }
    }
}
