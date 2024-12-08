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
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(ApplicationDbContext context, ILogger<ReservationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Index action called");
            var userFirstName = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);

            if (user == null)
            {
                _logger.LogWarning("User not found for first name: {UserFirstName}", userFirstName);
                return NotFound("User not found");
            }

            var reservations = await _context.Reservations
            .Where(r => r.UserId == user.UserId)
            .Include(r => r.Property)
            .Include(r => r.User)
            .ToListAsync();

            _logger.LogInformation("Retrieved {Count} reservations", reservations.Count);
            return View(reservations);
        }

        // GET: Reservations/MyReservations
        public async Task<IActionResult> MyReservations()
        {
            var userFirstName = User.Identity.Name;
            _logger.LogInformation("Current user first name: {UserFirstName}", userFirstName);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);
            if (user == null)
            {
                _logger.LogWarning("User not found for first name: {UserFirstName}", userFirstName);
                return NotFound("User not found");
            }

            var reservations = await _context.Reservations
            .Where(r => r.UserId == user.UserId)
            .Include(r => r.Property)
            .Include(r => r.User)
            .ToListAsync();

            _logger.LogInformation("Retrieved {Count} reservations for user {UserId}", reservations.Count, user.UserId);
            return View(reservations);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create()
        {
            var userFirstName = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);

            if (user == null)
            {
                _logger.LogWarning($"User with name {userFirstName} not found.");
                return NotFound("User not found");
            }

            // Pobierz tylko nieruchomości, które nie należą do użytkownika
            var availableProperties = await _context.Properties
                .Where(p => p.OwnerUserId != user.UserId)
                .ToListAsync();

            ViewData["Properties"] = new SelectList(availableProperties, "PropertyId", "Address");
            return View();
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationId,PropertyId,StartDate,EndDate,Status")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                var userFirstName = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.FirstName == userFirstName);

                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(reservation);
                }

                var property = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyId == reservation.PropertyId);
                if (property == null)
                {
                    ModelState.AddModelError("", "Property not found.");
                    return View(reservation);
                }

                if (property.OwnerUserId == user.UserId)
                {
                    ModelState.AddModelError("", "You cannot reserve your own property.");
                    return View(reservation);
                }

                // Sprawdź, czy są konflikty terminów
                var conflictingReservations = await _context.Reservations
                    .Where(r => r.PropertyId == reservation.PropertyId &&
                                r.StartDate < reservation.EndDate &&
                                r.EndDate > reservation.StartDate)
                    .ToListAsync();

                if (conflictingReservations.Any())
                {
                    ModelState.AddModelError("", "The property is already reserved during the selected dates.");
                    return View(reservation);
                }

                reservation.UserId = user.UserId;
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyReservations));
            }

            // Jeśli walidacja się nie powiodła
            var availableProperties = await _context.Properties
                .Where(p => p.OwnerUserId != reservation.UserId)
                .ToListAsync();

            ViewData["Properties"] = new SelectList(availableProperties, "PropertyId", "Address", reservation.PropertyId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit GET action called with null id");
                return NotFound();
            }

            var reservation = await _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .FirstOrDefaultAsync(m => m.ReservationId == id);

            if (reservation == null)
            {
                _logger.LogWarning("Edit GET action could not find reservation with id {Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Edit GET action found reservation with id {Id}", id);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,PropertyId,StartDate,EndDate,Status")] Reservation reservation)
        {
            if (id != reservation.ReservationId)
            {
                _logger.LogWarning("Edit POST action called with mismatched id {Id} and reservation id {ReservationId}", id, reservation.ReservationId);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingReservation = await _context.Reservations
                    .Include(r => r.Property)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(m => m.ReservationId == id);

                    if (existingReservation != null)
                    {
                        // Check for conflicting reservations
                        var conflictingReservations = await _context.Reservations
                        .Where(r => r.PropertyId == reservation.PropertyId &&
                        r.StartDate < reservation.EndDate &&
                        r.EndDate > reservation.StartDate &&
                        r.ReservationId != reservation.ReservationId) // Exclude current reservation
                        .ToListAsync();

                        if (conflictingReservations.Any())
                        {
                            _logger.LogWarning("Attempt to update reservation for property {PropertyId} during already reserved dates", reservation.PropertyId);
                            ModelState.AddModelError("", "The property is already reserved during the selected dates.");
                        }
                        else
                        {
                            existingReservation.StartDate = reservation.StartDate;
                            existingReservation.EndDate = reservation.EndDate;
                            existingReservation.Status = reservation.Status;

                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Reservation with id {Id} updated successfully", id);
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Edit POST action could not find reservation with id {ReservationId}", reservation.ReservationId);
                        return NotFound();
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ReservationExists(reservation.ReservationId))
                    {
                        _logger.LogWarning("Edit POST action could not find reservation with id {ReservationId}", reservation.ReservationId);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "An error occurred while updating the reservation with id {Id}", id);
                        throw;
                    }
                }
            }

            _logger.LogWarning("Edit POST action called with invalid model state");
            var availableProperties = await _context.Properties
            .Where(p => p.OwnerUserId != reservation.UserId)
            .ToListAsync();

            ViewData["Properties"] = new SelectList(availableProperties, "PropertyId", "Address", reservation.PropertyId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete GET action called with null id");
                return NotFound();
            }

            var reservation = await _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .FirstOrDefaultAsync(m => m.ReservationId == id);

            if (reservation == null)
            {
                _logger.LogWarning("Delete GET action could not find reservation with id {Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Delete GET action found reservation with id {Id}", id);
            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                _logger.LogWarning("Delete POST action could not find reservation with id {Id}", id);
                return NotFound();
            }

            try
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Reservation with id {Id} deleted successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the reservation with id {Id}", id);
                ModelState.AddModelError("", "Unable to delete reservation. Try again, and if the problem persists, see your system administrator.");
                return View(reservation);
            }
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details GET action called with null id");
                return NotFound();
            }

            var reservation = await _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .FirstOrDefaultAsync(m => m.ReservationId == id);

            if (reservation == null)
            {
                _logger.LogWarning("Details GET action could not find reservation with id {Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Details GET action found reservation with id {Id}", id);
            return View(reservation);
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationId == id);
        }

        // New method to get property details
        [HttpGet]
        public async Task<IActionResult> GetPropertyDetails(int propertyId)
        {
            var property = await _context.Properties
            .Where(p => p.PropertyId == propertyId)
            .Select(p => new
            {
                p.PropertyId,
                p.Address,
                p.ImageUrl
            })
            .FirstOrDefaultAsync();

            if (property == null)
            {
                return NotFound();
            }

            return Json(property);
        }
    }
}