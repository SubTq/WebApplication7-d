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
        public IActionResult Login()
        {
            return View();
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
