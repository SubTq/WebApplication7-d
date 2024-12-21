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
            var properties = from p in _context.Properties select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                properties = properties.Where(p => p.Address.Contains(searchString));
            }

            return View(await properties.ToListAsync());
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SearchProperties(string query)
        {
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
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToAction("Index");
            }

            var property = await _context.Properties
                .Include(p => p.OwnerUser)
                .FirstOrDefaultAsync(m => m.PropertyId == id);

            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToAction("Index");
            }

            var currentUserEmail = User.Identity?.Name;
            ViewData["IsOwner"] = property.OwnerUser?.Email == currentUserEmail;
            ViewData["CurrentUserEmail"] = currentUserEmail;
            ViewData["OwnerEmail"] = property.OwnerUser?.Email ?? "Unknown";

            ViewData["MainImageUrl"] = !string.IsNullOrEmpty(property.ImageUrl) ? property.ImageUrl : "https://via.placeholder.com/150";
            ViewData["AdditionalImageUrl1"] = !string.IsNullOrEmpty(property.AdditionalImageUrl1) ? property.AdditionalImageUrl1 : null;
            ViewData["AdditionalImageUrl2"] = !string.IsNullOrEmpty(property.AdditionalImageUrl2) ? property.AdditionalImageUrl2 : null;

            return View(property);
        }


        // GET: Properties/MyProperties
        public async Task<IActionResult> MyProperties()
        {
            var currentUserEmail = User.Identity.Name;

            if (string.IsNullOrEmpty(currentUserEmail))
            {
                _logger.LogWarning("Current user email is null or empty.");
                return NotFound("User not found");
            }

            var properties = await _context.Properties
                .Where(p => p.OwnerUser.Email == currentUserEmail)
                .Include(p => p.Reservations) // Ważne: Dodanie Include
                .ThenInclude(r => r.User)    // Opcjonalnie: Dodanie użytkownika
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} properties for user {Email}", properties.Count, currentUserEmail);
            return View(properties);
        }



        // GET: Properties/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var currentUserEmail = User.Identity.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            if (user == null)
            {
                _logger.LogWarning($"User with email {currentUserEmail} not found.");
                return NotFound("User not found.");
            }

            ViewData["OwnerUserId"] = user.UserId;
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
                    var currentUserEmail = User.Identity.Name;

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                    if (user == null)
                    {
                        ModelState.AddModelError("", $"Owner user with email {currentUserEmail} not found.");
                        _logger.LogWarning($"Owner user with email {currentUserEmail} not found.");
                    }
                    else
                    {
                        property.OwnerUserId = user.UserId;
                        property.OwnerUser = user;

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

        private void PopulateStatusList(string selectedStatus = null)
        {
            ViewBag.StatusList = new SelectList(new[] { "Available", "Rented", "Under Maintenance" }, selectedStatus);
        }

        // GET: Properties/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToAction("Index", "Home");
            }

            var property = await _context.Properties
                .Include(p => p.OwnerUser)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (property == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToAction("Index", "Home");
            }

            var currentUserEmail = User.Identity?.Name;

            if (property.OwnerUser?.Email != currentUserEmail)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this property.";
                return RedirectToAction("Index", "Home");
            }

            return View(property);
        }

        // POST: Properties/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("PropertyId,Address,Type,Price,Status,ContactNumber,Description,ImageUrl,AdditionalImageUrl1,AdditionalImageUrl2,OwnerUserId")] Property property)
        {
            if (id != property.PropertyId)
            {
                TempData["ErrorMessage"] = "Invalid property ID.";
                return RedirectToAction("Index", "Home");
            }

            var currentUserEmail = User.Identity?.Name;
            var existingProperty = await _context.Properties
                .Include(p => p.OwnerUser)
                .FirstOrDefaultAsync(p => p.PropertyId == id);

            if (existingProperty == null)
            {
                TempData["ErrorMessage"] = "Property not found.";
                return RedirectToAction("Index", "Home");
            }

            if (existingProperty.OwnerUser?.Email != currentUserEmail)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this property.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return View(property);
            }

            try
            {
                existingProperty.Address = property.Address;
                existingProperty.Type = property.Type;
                existingProperty.Price = property.Price;
                existingProperty.Status = property.Status;
                existingProperty.ContactNumber = property.ContactNumber;
                existingProperty.Description = property.Description;
                existingProperty.ImageUrl = property.ImageUrl;
                existingProperty.AdditionalImageUrl1 = property.AdditionalImageUrl1;
                existingProperty.AdditionalImageUrl2 = property.AdditionalImageUrl2;

                _context.Update(existingProperty);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Property updated successfully.";
                return RedirectToAction(nameof(MyProperties));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropertyExists(property.PropertyId))
                {
                    TempData["ErrorMessage"] = "Property not found.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    throw;
                }
            }


        }

        [Authorize]
        public async Task<IActionResult> ManageReservations(int propertyId)
        {
            var currentUserEmail = User.Identity.Name;
            var property = await _context.Properties
                .Include(p => p.Reservations)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerUser.Email == currentUserEmail);

            if (property == null)
            {
                return NotFound("Property not found or unauthorized access.");
            }

            return View(property);
        }





        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }
    }
}
