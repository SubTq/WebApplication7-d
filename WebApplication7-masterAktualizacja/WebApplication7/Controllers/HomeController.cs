using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication7.Data;
using WebApplication7.Models;

namespace WebApplication7.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var properties = await _context.Properties.Include(p => p.OwnerUser).ToListAsync();
            return View(properties);
        }
        [HttpGet]
        public async Task<IActionResult> SearchProperties(string searchString)
        {
            var properties = from p in _context.Properties
                             select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                properties = properties.Where(p => p.Address.Contains(searchString));
            }

            var result = await properties.Select(p => new
            {
                p.PropertyId,
                p.Address,
                p.Description,
                p.Price,
                p.ImageUrl
            }).ToListAsync();

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == user.Email && u.Password == user.Password);

            if (admin != null)
            {
                // Przekierowanie do panelu administracyjnego
                return RedirectToAction("Index", "Admin");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(user);
        }
    }
}
