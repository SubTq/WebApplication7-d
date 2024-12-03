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

            // Filtracja wyników na podstawie wpisanego adresu
            if (!string.IsNullOrEmpty(searchString))
            {
                properties = properties.Where(p => p.Address.Contains(searchString));
            }

            return View(await properties.ToListAsync());
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
            _logger.LogInformation($"User.Identity.Name: {userFirstName}");

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
        public async Task<IActionResult> Create([Bind("PropertyId,Address,Type,Price,Status,ContactNumber,Description,ImageUrl")] Property property)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Model state is valid");
                try
                {
                    var userFirstName = User.Identity.Name;
                    _logger.LogInformation($"User.Identity.Name: {userFirstName}");

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
            else
            {
                _logger.LogWarning("Model state is invalid");
                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        _logger.LogWarning($"Property: {error.Key}, Error: {subError.ErrorMessage}");
                    }
                }
            }
            var userNamePost = User.Identity.Name;
            var userPost = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userNamePost);
            ViewData["OwnerUserId"] = userPost?.UserId;
            return View(property);
        }

        // GET: Properties/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }
            var userFirstName = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);
            if (user == null)
            {
                _logger.LogWarning($"User with name {userFirstName} not found.");
            }
            ViewData["OwnerUserId"] = user?.UserId;
            return View(property);
        }

        // POST: Properties/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("PropertyId,Address,Type,Price,Status,ContactNumber,Description,ImageUrl")] Property property)
        {
            if (id != property.PropertyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userFirstName = User.Identity.Name;
                    _logger.LogInformation($"User.Identity.Name: {userFirstName}");

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);
                    if (user == null)
                    {
                        ModelState.AddModelError("", $"Owner user with name {userFirstName} not found.");
                        _logger.LogWarning($"Owner user with name {userFirstName} not found.");
                    }
                    else
                    {
                        property.OwnerUserId = user.UserId;
                        _context.Update(property);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Property updated successfully");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(property.PropertyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(MyProperties));
            }
            var userNamePost = User.Identity.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userNamePost);
            ViewData["OwnerUserId"] = currentUser?.UserId;
            return View(property);
        }

        // GET: Properties/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            return View(property);
        }

        // POST: Properties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyProperties));
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.PropertyId == id);
        }

        // GET: Properties/Details/5
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

            var userFirstName = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);
            ViewData["IsOwner"] = (user != null && property.OwnerUserId == user.UserId);

            return View(property);
        }
    }
}
