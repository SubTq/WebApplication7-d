using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PagedList.Core;
using System.Linq;
using System.Security.Claims;
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


        
        public async Task<IActionResult> MyReservations(int? pageNumber)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Unauthorized access to MyReservations.");
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                _logger.LogWarning("User not found for email: {UserEmail}", userEmail);
                return NotFound("User not found.");
            }

            
            var reservationsToUpdate = await _context.Reservations
                .Where(r => r.UserId == user.UserId && r.EndDate < DateTime.Now && r.Status != "Zakończony")
                .ToListAsync();

            foreach (var reservation in reservationsToUpdate)
            {
                reservation.Status = "Zakończony";
                _context.Update(reservation);
            }
            await _context.SaveChangesAsync();

            
            int pageSize = 10;
            int currentPage = pageNumber ?? 1;

            var reservations = await _context.Reservations
                .Where(r => r.UserId == user.UserId)
                .Include(r => r.Property)
                .OrderBy(r => r.StartDate)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalReservations = await _context.Reservations
                .Where(r => r.UserId == user.UserId)
                .CountAsync();

            ViewData["TotalPages"] = (int)Math.Ceiling(totalReservations / (double)pageSize);
            ViewData["CurrentPage"] = currentPage;

            return View(reservations);
        }



        
        public async Task<IActionResult> Create(int? propertyId)
        {
            if (propertyId == null)
            {
                return NotFound();
            }

            var property = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId);

            if (property == null)
            {
                return NotFound();
            }

            ViewData["PropertyId"] = propertyId;
            ViewData["PropertyAddress"] = property.Address;
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationId,PropertyId,StartDate,EndDate,Status")] Reservation reservation)
        {
            _logger.LogInformation("POST Create method called.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid.");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogError("Validation error in field {Field}: {ErrorMessage}", state.Key, error.ErrorMessage);
                    }
                }
                return View(reservation);
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                _logger.LogError("User not found.");
                ModelState.AddModelError("", "User not found.");
                return View(reservation);
            }

            var property = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyId == reservation.PropertyId);
            if (property == null)
            {
                _logger.LogError("Property not found.");
                ModelState.AddModelError("", "Property not found.");
                return View(reservation);
            }

            if (property.OwnerUserId == user.UserId)
            {
                _logger.LogError("User tried to reserve their own property.");
                ModelState.AddModelError("", "You cannot reserve your own property.");
                return View(reservation);
            }

            var conflictingReservations = await _context.Reservations
                .Where(r => r.PropertyId == reservation.PropertyId &&
                            r.StartDate < reservation.EndDate &&
                            r.EndDate > reservation.StartDate)
                .ToListAsync();

            if (conflictingReservations.Any())
            {
                _logger.LogError("Conflicting reservations found.");
                ModelState.AddModelError("", "The property is already reserved during the selected dates.");
                return View(reservation);
            }

            reservation.UserId = user.UserId;
            _context.Add(reservation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reservation created successfully.");
            return RedirectToAction(nameof(MyReservations));
        }

        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReservationId == id);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.Status == "Zakończony")
            {
                TempData["ErrorMessage"] = "You cannot edit a reservation that has ended.";
                return RedirectToAction(nameof(MyReservations));
            }

            return View(reservation);
        }


        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,PropertyId,StartDate,EndDate,Status,UserId")] Reservation reservation)
        {
            if (id != reservation.ReservationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    
                    _logger.LogInformation("Rozpoczęcie aktualizacji rezerwacji. Reservation ID: {ReservationId}, User ID: {UserId}, Property ID: {PropertyId}, StartDate: {StartDate}, EndDate: {EndDate}",
                        reservation.ReservationId, reservation.UserId, reservation.PropertyId, reservation.StartDate, reservation.EndDate);

                    
                    if (!_context.Users.Any(u => u.UserId == reservation.UserId))
                    {
                        _logger.LogWarning("Nieprawidłowy UserId: {UserId} dla rezerwacji ID: {ReservationId}", reservation.UserId, reservation.ReservationId);
                        ModelState.AddModelError("UserId", "Nieprawidłowy identyfikator użytkownika.");
                        return View(reservation);
                    }

                    
                    var conflictingReservations = await _context.Reservations
                        .Where(r => r.PropertyId == reservation.PropertyId &&
                                    r.StartDate < reservation.EndDate &&
                                    r.EndDate > reservation.StartDate &&
                                    r.ReservationId != reservation.ReservationId)
                        .ToListAsync();

                    if (conflictingReservations.Any())
                    {
                        _logger.LogWarning("Konflikt rezerwacji. Reservation ID: {ReservationId}, Property ID: {PropertyId}, StartDate: {StartDate}, EndDate: {EndDate}",
                            reservation.ReservationId, reservation.PropertyId, reservation.StartDate, reservation.EndDate);

                        ModelState.AddModelError("", "The property is already reserved during the selected dates.");
                        return View(reservation);
                    }

                   
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pomyślnie zaktualizowano rezerwację. Reservation ID: {ReservationId}", reservation.ReservationId);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.ReservationId))
                    {
                        _logger.LogError("Nie znaleziono rezerwacji ID: {ReservationId} podczas aktualizacji.", reservation.ReservationId);
                        return NotFound();
                    }
                    _logger.LogError("Błąd współbieżności podczas aktualizacji rezerwacji ID: {ReservationId}.", reservation.ReservationId);
                    throw;
                }

                return RedirectToAction(nameof(MyReservations));
            }

            
            _logger.LogWarning("ModelState nie jest prawidłowy dla rezerwacji ID: {ReservationId}.", reservation.ReservationId);
            return View(reservation);
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationId == id);
        }


        
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

        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.Status == "Zakończony")
            {
                TempData["ErrorMessage"] = "You cannot delete a reservation that has ended.";
                return RedirectToAction(nameof(MyReservations));
            }

            try
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Reservation ID {ReservationId} deleted successfully.", id);
                return RedirectToAction(nameof(MyReservations));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while deleting reservation ID {ReservationId}.", id);
                TempData["ErrorMessage"] = "Unable to delete reservation.";
                return RedirectToAction(nameof(MyReservations));
            }
        }


        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .ThenInclude(p => p.OwnerUser) 
                .FirstOrDefaultAsync(m => m.ReservationId == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }



        
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

        [Authorize]
        public async Task<IActionResult> ManageReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null)
            {
                return NotFound();
            }

            var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);

            if (currentUser == null || reservation.Property.OwnerUserId != currentUser.UserId)
            {
                return Unauthorized();
            }

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageReservation(int id, string status)
        {
            
            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null)
            {
                return NotFound("Reservation not found.");
            }

            
            reservation.Status = status;

            try
            {
                _context.Update(reservation);
                await _context.SaveChangesAsync(); 
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error while updating reservation status.");
                ModelState.AddModelError("", "Unable to update reservation status. Try again later.");
                return View(reservation); 
            }

            
            return RedirectToAction("MyProperties", "Properties");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateReservation(int reservationId, int rating)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);

            if (reservation == null || reservation.Status != "Zakończony")
            {
                return BadRequest("Nie można ocenić tej rezerwacji.");
            }

            if (reservation.Rating != null)
            {
                return BadRequest("Rezerwacja została już oceniona.");
            }

            reservation.Rating = rating;
            _context.Update(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyReservations));
        }



    }
}