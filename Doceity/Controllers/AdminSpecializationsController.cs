// File: Controllers/AdminSpecializationsController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants; // Per Roles
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
// Potrebbe essere utile aggiungere un logger se non già presente a livello base
// using Microsoft.Extensions.Logging;

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Admin)] // Solo Admin può accedere
    public class AdminSpecializationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        // private readonly ILogger<AdminSpecializationsController> _logger; // Opzionale, per logging

        public AdminSpecializationsController(ApplicationDbContext context /*, ILogger<AdminSpecializationsController> logger */)
        {
            _context = context;
            // _logger = logger; // Opzionale
        }

        // GET: AdminSpecializations
        public async Task<IActionResult> Index()
        {
            var specializations = await _context.Specializations
                                                .OrderBy(s => s.Name)
                                                .Include(s => s.ExpertSpecializations) // Per contare gli esperti associati (se serve in vista)
                                                .ToListAsync();
            return View(specializations); // Passa alla vista Views/AdminSpecializations/Index.cshtml
        }

        // GET: AdminSpecializations/Create
        public IActionResult Create()
        {
            return View(); // Mostra Views/AdminSpecializations/Create.cshtml
        }

        // POST: AdminSpecializations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Specialization specialization)
        {
            // Verifica unicità nome prima di validare il modello completo
            if (!string.IsNullOrEmpty(specialization.Name) &&
                await _context.Specializations.AnyAsync(s => s.Name.ToLower() == specialization.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Una specializzazione con questo nome esiste già.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(specialization);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Specializzazione '{specialization.Name}' creata con successo.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Errore nella creazione della specializzazione. Controlla i campi.";
            return View(specialization);
        }

        // GET: AdminSpecializations/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            // Usare id.Value perché id è nullable int?
            var specialization = await _context.Specializations.FindAsync(id.Value);
            if (specialization == null)
            {
                return NotFound();
            }
            return View(specialization); // Passa a Views/AdminSpecializations/Edit.cshtml
        }

        // POST: AdminSpecializations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SpecializationId,Name,Description")] Specialization specialization)
        {
            if (id != specialization.SpecializationId)
            {
                return NotFound();
            }

            // Verifica unicità nome escludendo se stessa
            if (!string.IsNullOrEmpty(specialization.Name) &&
                await _context.Specializations.AnyAsync(s => s.Name.ToLower() == specialization.Name.ToLower() && s.SpecializationId != id))
            {
                ModelState.AddModelError("Name", "Un'altra specializzazione con questo nome esiste già.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(specialization);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Specializzazione '{specialization.Name}' aggiornata con successo.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await SpecializationExists(specialization.SpecializationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "La specializzazione è stata modificata o eliminata da un altro utente. Ricarica la pagina e riprova.");
                        // _logger.LogWarning($"Concurrency conflict for SpecializationId {specialization.SpecializationId}"); // Esempio di logging
                        return View(specialization); // Ritorna alla vista con il modello per mostrare l'errore
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Errore nell'aggiornamento della specializzazione. Controlla i campi.";
            return View(specialization);
        }

        // GET: AdminSpecializations/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialization = await _context.Specializations
                                             .Include(s => s.ExpertSpecializations) // Includi per vedere gli esperti associati
                                                .ThenInclude(es => es.User) // Includi i dettagli dell'utente (esperto)
                                             .FirstOrDefaultAsync(m => m.SpecializationId == id.Value);

            if (specialization == null)
            {
                return NotFound();
            }

            return View(specialization); // Passa a Views/AdminSpecializations/Details.cshtml
        }

        // GET: AdminSpecializations/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specialization = await _context.Specializations
                                             .Include(s => s.ExpertSpecializations) // Necessario per il controllo sugli esperti associati
                                             .FirstOrDefaultAsync(m => m.SpecializationId == id.Value);
            if (specialization == null)
            {
                return NotFound();
            }

            // Potresti voler passare informazioni aggiuntive alla vista Delete se ci sono esperti associati
            if (specialization.ExpertSpecializations.Any())
            {
                ViewData["WarningMessage"] = $"Attenzione: questa specializzazione è assegnata a {specialization.ExpertSpecializations.Count} esperto/i. L'eliminazione è bloccata.";
            }

            return View(specialization); // Passa a Views/AdminSpecializations/Delete.cshtml
        }

        // POST: AdminSpecializations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var specialization = await _context.Specializations
                                             .Include(s => s.ExpertSpecializations) // Necessario per il controllo
                                             .FirstOrDefaultAsync(s => s.SpecializationId == id);
            if (specialization == null)
            {
                TempData["WarningMessage"] = "Specializzazione non trovata. Potrebbe essere già stata eliminata.";
                return RedirectToAction(nameof(Index));
            }

            // Controllo di business: non eliminare se la specializzazione è usata da esperti.
            if (specialization.ExpertSpecializations.Any())
            {
                TempData["ErrorMessage"] = $"Impossibile eliminare la specializzazione '{specialization.Name}' perché è attualmente assegnata a {specialization.ExpertSpecializations.Count} esperto/i. Rimuovi prima l'assegnazione dagli esperti o modifica le loro specializzazioni.";
                // Reindirizza alla pagina di conferma Delete, passando l'ID, così l'utente vede il messaggio lì.
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            try
            {
                _context.Specializations.Remove(specialization);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Specializzazione '{specialization.Name}' eliminata con successo.";
            }
            catch (DbUpdateException ex) // Catch per altri vincoli FK o problemi DB
            {
                // _logger.LogError(ex, "Errore DB eliminazione specializzazione {Id}", id);
                TempData["ErrorMessage"] = $"Errore database durante l'eliminazione: {ex.InnerException?.Message ?? ex.Message}. Verifica che non ci siano dipendenze residue.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> SpecializationExists(int id)
        {
            return _context.Specializations.AnyAsync(e => e.SpecializationId == id);
        }
    }
}