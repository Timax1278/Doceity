// File: Controllers/ExpertServicesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Doceity.Data;          // Assicurati namespace corretto
using Doceity.Models;         // Assicurati namespace corretto
using Doceity.Constants;      // Assicurati namespace corretto per Roles

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Expert)]
    public class ExpertServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpertServicesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<(bool isApproved, string userId)> CheckExpertStatusAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (false, null);
            return (user.IsApprovedExpert, user.Id);
        }

        // GET: ExpertServices
        public async Task<IActionResult> Index()
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Your expert account is pending approval.";
                return RedirectToAction("Index", "Home");
            }
            var expertServices = await _context.ExpertServices
                                        .Where(s => s.ExpertUserId == userId)
                                        .OrderBy(s => s.Title)
                                        .ToListAsync();
            return View(expertServices);
        }

        // GET: ExpertServices/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var (isApproved, _) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono definire nuovi servizi.";
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        // POST: ExpertServices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,EstimatedDurationMinutes,Price,IsEnabled")] ExpertService expertService)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            expertService.ExpertUserId = userId;
            ModelState.Remove("ExpertUserId"); // Rimuovi per evitare validazione su campo impostato server-side
            ModelState.Remove("Expert");     // Rimuovi navigation property dalla validazione

            if (ModelState.IsValid)
            {
                _context.Add(expertService);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Service created successfully.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Please correct the errors below.";
            return View(expertService);
        }

        // GET: ExpertServices/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var expertService = await _context.ExpertServices.FindAsync(id);
            if (expertService == null) return NotFound();
            if (expertService.ExpertUserId != userId) return Forbid();

            return View(expertService);
        }

        // POST: ExpertServices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ExpertServiceId,Title,Description,EstimatedDurationMinutes,Price,IsEnabled")] ExpertService expertService)
        {
            if (id != expertService.ExpertServiceId) return NotFound();

            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var originalService = await _context.ExpertServices
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync(s => s.ExpertServiceId == id);
            if (originalService == null) return NotFound();
            if (originalService.ExpertUserId != userId) return Forbid();

            // Imposta la chiave esterna manualmente sull'oggetto che stai per validare/aggiornare.
            // Questo è cruciale perché non è parte del binding del form.
            expertService.ExpertUserId = userId;

            // --- MODIFICA CHIAVE: Rimuovi proprietà non dal form PRIMA di ModelState.IsValid ---
            // Rimuovi la proprietà di navigazione 'Expert' dalla validazione.
            // È buona pratica rimuoverla perché non è un input del form e potrebbe causare problemi di validazione.
            ModelState.Remove("Expert");

            // Rimuovi 'ExpertUserId' dalla validazione. Anche se è Required nel modello,
            // la stiamo impostando noi server-side. Se non la rimuovessimo, e per qualche motivo
            // il model binder non la vedesse (anche se l'abbiamo appena impostata),
            // ModelState.IsValid fallirebbe.
            ModelState.Remove("ExpertUserId");
            // ------------------------------------------------------------------------------------

            if (ModelState.IsValid) // Ora controlla la validità
            {
                try
                {
                    _context.Update(expertService);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Service updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpertServiceExists(expertService.ExpertServiceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "The service was modified by another user. Please try again.";
                        // Ritorna i dati che l'utente ha cercato di salvare per permettergli di riprovare.
                        return View(expertService);
                    }
                }
                return RedirectToAction(nameof(Index)); // Successo, torna all'elenco
            }

            // Se ModelState NON è valido:
            // Il messaggio "Please correct the errors below." viene già gestito dalla vista
            // se si usa asp-validation-summary="All" o asp-validation-summary="ModelOnly"
            // e gli errori sono associati ai campi.
            // Se l'errore è "nascosto", il debug o asp-validation-summary="All" nella vista lo rivelerà.
            TempData["ErrorMessage"] = "Please correct the errors below."; // Messaggio generico se nessun errore di campo è mostrato
            return View(expertService); // Ritorna alla vista Edit mostrando i dati inseriti
        }


        // GET: ExpertServices/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var expertService = await _context.ExpertServices
                .Include(s => s.Expert) // Includi se vuoi mostrare info dell'esperto nella conferma
                .FirstOrDefaultAsync(m => m.ExpertServiceId == id);
            if (expertService == null) return NotFound();
            if (expertService.ExpertUserId != userId) return Forbid();

            return View(expertService);
        }

        // POST: ExpertServices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var expertService = await _context.ExpertServices.FindAsync(id);
            if (expertService == null) return NotFound();
            if (expertService.ExpertUserId != userId) return Forbid();

            try
            {
                _context.ExpertServices.Remove(expertService);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Service deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting service (likely FK constraint): {ex.InnerException?.Message ?? ex.Message}");
                if (ex.InnerException?.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase) == true)
                {
                    ModelState.AddModelError(string.Empty, "Cannot delete this service because it has associated consultation requests or other dependencies.");
                    TempData["ErrorMessage"] = "Cannot delete this service due to existing dependencies (e.g., requests). Please resolve them first.";
                }
                else
                {
                    ModelState.AddModelError("", "Unable to delete service due to a database error.");
                    TempData["ErrorMessage"] = "Unable to delete service. Please contact support.";
                }
                expertService = await _context.ExpertServices.Include(s => s.Expert).FirstOrDefaultAsync(m => m.ExpertServiceId == id);
                if (expertService == null) return NotFound();
                return View("Delete", expertService);
            }
        }

        private bool ExpertServiceExists(int id)
        {
            return _context.ExpertServices.Any(e => e.ExpertServiceId == id);
        }
    }
}