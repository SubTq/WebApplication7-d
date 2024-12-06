using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using WebApplication7.Data;
using WebApplication7.Models;

namespace WebApplication7.Controllers
{
    [Authorize]
    public class PropertiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PropertiesController> _logger;

        public PropertiesController(ApplicationDbContext context, ILogger<PropertiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Properties
        public async Task<IActionResult> Index(string searchString)
        {
            var properties = from p in _context.Properties
                             select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                properties = properties.Where(p => p.Address.Contains(searchString));
            }

            return View(await properties.ToListAsync());
        }

        [HttpGet]
        [AllowAnonymous] // Dodaj ten atrybut, aby metoda była dostępna bez logowania
        public async Task<IActionResult> SearchProperties(string query)
        {
            // Gdy query jest puste lub null, zwróć wszystkie nieruchomości
            if (string.IsNullOrWhiteSpace(query))
            {
                var allProperties = await _context.Properties
                    .Select(p => new
                    {
                        p.PropertyId,
                        p.Address,
                        p.ImageUrl,
                        p.Description,
                        Price = p.Price.ToString("C")
                    })
                    .ToListAsync();

                return Json(new { properties = allProperties });
            }

            // Gdy query jest niepuste, wykonaj filtrowanie po adresie
            var filteredProperties = await _context.Properties
                .Where(p => p.Address.Contains(query))
                .Select(p => new
                {
                    p.PropertyId,
                    p.Address,
                    p.ImageUrl,
                    p.Description,
                    Price = p.Price.ToString("C")
                })
                .ToListAsync();

            return Json(new { properties = filteredProperties });
        }

        // GET: Properties/Details/5
        [AllowAnonymous] // Umożliwia dostęp do metody dla niezalogowanych użytkowników
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.OwnerUser)
                .FirstOrDefaultAsync(m => m.PropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            // Obsługa wartości null dla zdjęć
            ViewData["MainImageUrl"] = !string.IsNullOrEmpty(property.ImageUrl) ? property.ImageUrl : null;
            ViewData["AdditionalImageUrl1"] = !string.IsNullOrEmpty(property.AdditionalImageUrl1) ? property.AdditionalImageUrl1 : null;
            ViewData["AdditionalImageUrl2"] = !string.IsNullOrEmpty(property.AdditionalImageUrl2) ? property.AdditionalImageUrl2 : null;

            return View(property);
        }

        // GET: Properties/MyProperties
        public async Task<IActionResult> MyProperties()
        {
            var userFirstName = User.Identity.Name;
            _logger.LogInformation("Current user first name: {UserFirstName}", userFirstName);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);
            if (user == null)
            {
                _logger.LogWarning("User not found for first name: {UserFirstName}", userFirstName);
                return NotFound("User not found");
            }

            var properties = await _context.Properties
                .Where(p => p.OwnerUserId == user.UserId)
                .Include(p => p.OwnerUser)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} properties for user {UserId}", properties.Count, user.UserId);
            return View(properties);
        }

        // GET: Properties/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Create action called");
            var userFirstName = User.Identity.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);
            if (user == null)
            {
                _logger.LogWarning($"User with name {userFirstName} not found.");
            }
            ViewData["OwnerUserId"] = user?.UserId;
            return View();
        }

        // POST: Properties/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("PropertyId,Address,Type,Price,Status,ContactNumber,Description,ImageUrl,AdditionalImageUrl1,AdditionalImageUrl2")] Property property)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userFirstName = User.Identity.Name;

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);
                    if (user == null)
                    {
                        ModelState.AddModelError("", $"Owner user with name {userFirstName} not found.");
                        _logger.LogWarning($"Owner user with name {userFirstName} not found.");
                    }
                    else
                    {
                        property.OwnerUserId = user.UserId;
                        property.OwnerUser = user;

                        // Zapisz linki do obrazów
                        if (!Uri.IsWellFormedUriString(property.ImageUrl, UriKind.Absolute))
                        {
                            ModelState.AddModelError("ImageUrl", "The ImageUrl field must be a valid URL.");
                            return View(property);
                        }

                        if (!string.IsNullOrEmpty(property.AdditionalImageUrl1) &&
                            !Uri.IsWellFormedUriString(property.AdditionalImageUrl1, UriKind.Absolute))
                        {
                            ModelState.AddModelError("AdditionalImageUrl1", "Additional Image 1 URL must be a valid URL.");
                            return View(property);
                        }

                        if (!string.IsNullOrEmpty(property.AdditionalImageUrl2) &&
                            !Uri.IsWellFormedUriString(property.AdditionalImageUrl2, UriKind.Absolute))
                        {
                            ModelState.AddModelError("AdditionalImageUrl2", "Additional Image 2 URL must be a valid URL.");
                            return View(property);
                        }

                        _context.Add(property);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Property added successfully");
                        return RedirectToAction(nameof(MyProperties));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding property");
                    ModelState.AddModelError("", "Unable to add property. Please try again.");
                }
            }
            return View(property);
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }
    }
}
