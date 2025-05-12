// File: Controllers/ExpertAvailabilityController.cs
using Doceity.Data;
using Doceity.Models; // Per Availability e ApplicationUser
using Doceity.Constants; // Per Roles
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Per DateTime
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Per List

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Expert)] // Solo Esperti possono gestire le loro disponibilità
    public class ExpertAvailabilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        // private readonly ILogger<ExpertAvailabilityController> _logger; // Opzionale per il logging

        public ExpertAvailabilityController(ApplicationDbContext context,
                                            UserManager<ApplicationUser> userManager
                                            /*, ILogger<ExpertAvailabilityController> logger */) // Aggiungi logger se lo usi
        {
            _context = context;
            _userManager = userManager;
            // _logger = logger;
        }

        // Helper per verificare se l'esperto è approvato
        private async Task<(bool isApproved, string userId)> CheckExpertStatusAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (false, null);
            return (user.IsApprovedExpert, user.Id);
        }

        // GET: ExpertAvailability
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono gestire le disponibilità.";
                return RedirectToAction("Index", "Home");
            }
            var today = DateTime.UtcNow.Date;
            var availabilities = await _context.Availabilities
                .Where(a => a.ExpertUserId == userId && a.AvailableDate >= today)
                .OrderBy(a => a.AvailableDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();
            return View(availabilities);
        }

        // GET: ExpertAvailability/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var (isApproved, _) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono aggiungere disponibilità.";
                return RedirectToAction(nameof(Index));
            }
            var model = new Availability
            {
                AvailableDate = DateTime.UtcNow.Date.AddDays(1)
            };
            return View(model);
        }

        // POST: ExpertAvailability/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AvailableDate,StartTime,EndTime")] Availability availability)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            availability.ExpertUserId = userId;
            ModelState.Remove("ExpertUserId");
            ModelState.Remove("Expert");

            if (availability.EndTime <= availability.StartTime)
            {
                ModelState.AddModelError("EndTime", "L'ora di fine deve essere successiva all'ora di inizio.");
            }
            if (availability.AvailableDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("AvailableDate", "La data di disponibilità non può essere nel passato.");
            }

            bool overlaps = await _context.Availabilities
                .AnyAsync(a => a.ExpertUserId == userId &&
                               a.AvailableDate == availability.AvailableDate.Date &&
                               ((availability.StartTime >= a.StartTime && availability.StartTime < a.EndTime) ||
                                (availability.EndTime > a.StartTime && availability.EndTime <= a.EndTime) ||
                                (availability.StartTime <= a.StartTime && availability.EndTime >= a.EndTime)));
            if (overlaps)
            {
                ModelState.AddModelError("", "Questa fascia oraria si sovrappone con una disponibilità esistente.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(availability);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Nuova disponibilità aggiunta con successo!";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Errore nell'aggiunta della disponibilità. Controlla i campi.";
            return View(availability);
        }

        // GET: ExpertAvailability/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono modificare le disponibilità.";
                return RedirectToAction(nameof(Index));
            }
            var availability = await _context.Availabilities.FindAsync(id.Value);
            if (availability == null) return NotFound();
            if (availability.ExpertUserId != userId)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a modificare questa disponibilità.";
                return Forbid();
            }
            return View(availability);
        }

        // POST: ExpertAvailability/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AvailabilityId,AvailableDate,StartTime,EndTime,ExpertUserId")] Availability availability)
        {
            if (id != availability.AvailabilityId) return NotFound();

            var (isApproved, currentUserId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var availabilityToUpdate = await _context.Availabilities.FirstOrDefaultAsync(a => a.AvailabilityId == id);
            if (availabilityToUpdate == null) return NotFound();
            if (availabilityToUpdate.ExpertUserId != currentUserId)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a modificare questa disponibilità.";
                return Forbid();
            }

            ModelState.Remove("Expert");

            if (availability.EndTime <= availability.StartTime)
            {
                ModelState.AddModelError("EndTime", "L'ora di fine deve essere successiva all'ora di inizio.");
            }
            if (availability.AvailableDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("AvailableDate", "La data di disponibilità non può essere nel passato.");
            }
            bool overlaps = await _context.Availabilities
                .AnyAsync(a => a.ExpertUserId == currentUserId &&
                               a.AvailabilityId != availability.AvailabilityId &&
                               a.AvailableDate == availability.AvailableDate.Date &&
                               ((availability.StartTime >= a.StartTime && availability.StartTime < a.EndTime) ||
                                (availability.EndTime > a.StartTime && availability.EndTime <= a.EndTime) ||
                                (availability.StartTime <= a.StartTime && availability.EndTime >= a.EndTime)));
            if (overlaps)
            {
                ModelState.AddModelError("", "Questa fascia oraria si sovrappone con un'altra tua disponibilità esistente.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    availabilityToUpdate.AvailableDate = availability.AvailableDate;
                    availabilityToUpdate.StartTime = availability.StartTime;
                    availabilityToUpdate.EndTime = availability.EndTime;
                    _context.Update(availabilityToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Disponibilità aggiornata con successo!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AvailabilityExists(availability.AvailabilityId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "La disponibilità è stata modificata da un altro processo. Riprova.";
                        return View(availabilityToUpdate);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Errore nell'aggiornamento della disponibilità. Controlla i campi.";
            return View(availability);
        }

        // GET: ExpertAvailability/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono eliminare le disponibilità.";
                return RedirectToAction(nameof(Index));
            }

            var availability = await _context.Availabilities
                .FirstOrDefaultAsync(m => m.AvailabilityId == id); // Non serve includere Expert qui
            if (availability == null) return NotFound();
            if (availability.ExpertUserId != userId)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a eliminare questa disponibilità.";
                return Forbid();
            }
            return View(availability);
        }

        // POST: ExpertAvailability/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var availability = await _context.Availabilities.FindAsync(id);
            if (availability == null)
            {
                TempData["WarningMessage"] = "Disponibilità non trovata. Potrebbe essere già stata eliminata.";
                return RedirectToAction(nameof(Index));
            }
            if (availability.ExpertUserId != userId)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a eliminare questa disponibilità.";
                return Forbid();
            }

            // --- CONTROLLO ESPLICITO PER RICHIESTE COLLEGATE ---
            bool hasLinkedConsultations = await _context.ConsultationRequests
                                                .AnyAsync(cr => cr.OriginalAvailabilityId == id &&
                                                                 (cr.Status == ConsultationRequestStatus.Pending ||
                                                                  cr.Status == ConsultationRequestStatus.Accepted)); // Controlla solo Pending o Accepted
            if (hasLinkedConsultations)
            {
                TempData["ErrorMessage"] = $"Impossibile eliminare questa disponibilità (ID: {id}) perché è collegata a una o più richieste di consulenza attive (In Attesa o Accettate). Risolvi prima quelle richieste.";
                return RedirectToAction(nameof(Delete), new { id = id }); // Torna alla pagina di conferma con l'errore
            }
            // --- FINE CONTROLLO ESPLICITO ---

            try
            {
                _context.Availabilities.Remove(availability);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Disponibilità eliminata con successo.";
            }
            catch (DbUpdateException ex) // Questo catch ora serve per altri errori DB, non per la FK da ConsultationRequests se il controllo sopra funziona
            {
                // _logger?.LogError(ex, "DbUpdateException durante l'eliminazione della disponibilità {AvailabilityId}", id);
                TempData["ErrorMessage"] = $"Errore database durante l'eliminazione: {ex.InnerException?.Message ?? ex.Message}. Se il problema persiste e riguarda un vincolo, potrebbero esserci altri dati collegati.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: ExpertAvailability/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono visualizzare i dettagli delle disponibilità.";
                return RedirectToAction(nameof(Index));
            }

            var availability = await _context.Availabilities
                .FirstOrDefaultAsync(m => m.AvailabilityId == id);
            if (availability == null) return NotFound();
            if (availability.ExpertUserId != userId)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a visualizzare questa disponibilità.";
                return Forbid();
            }
            return View(availability);
        }

        private bool AvailabilityExists(int id)
        {
            return _context.Availabilities.Any(e => e.AvailabilityId == id);
        }
    }
}