// File: Controllers/CourseManagementController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Per SelectList
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants;
using Doceity.ViewModels;

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Expert)] // Solo Esperti Approvati possono accedere
    public class CourseManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper per controllare stato esperto
        private async Task<(bool isApproved, string userId)> CheckExpertStatusAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (false, null);
            return (user.IsApprovedExpert, user.Id);
        }

        // GET: CourseManagement
        public async Task<IActionResult> Index()
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Account esperto non approvato. Non puoi gestire corsi.";
                return RedirectToAction("Index", "Home"); // O una pagina "in attesa di approvazione"
            }
            var courses = await _context.Courses
                                  .Where(c => c.CreatorExpertId == userId)
                                  .OrderByDescending(c => c.StartDateTime)
                                  .ToListAsync();
            return View(courses); // Si aspetta Views/CourseManagement/Index.cshtml
        }

        // GET: CourseManagement/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono creare corsi.";
                return RedirectToAction(nameof(Index));
            }
            var viewModel = new CreateCourseViewModel();
            // Popola eventuali dropdown necessari, es. per i servizi
            await PopulateAvailableServicesDropdown(viewModel, userId);
            return View(viewModel); // Si aspetta Views/CourseManagement/Create.cshtml
        }

        // POST: CourseManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseViewModel viewModel)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid(); // O un redirect con messaggio di errore

            // Validazione custom
            if (viewModel.StartDateTime <= DateTime.UtcNow.AddHours(1)) // Es. almeno un'ora nel futuro
            {
                ModelState.AddModelError(nameof(viewModel.StartDateTime), "La data e l'ora di inizio devono essere future.");
            }
            // Potresti aggiungere altri controlli qui, es. su MaxParticipants vs. logica di business

            if (ModelState.IsValid)
            {
                var newCourse = new Course
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    StartDateTime = viewModel.StartDateTime.ToUniversalTime(), // Salva sempre in UTC
                    DurationMinutes = viewModel.DurationMinutes,
                    Price = viewModel.Price,
                    MaxParticipants = viewModel.MaxParticipants,
                    VideoMeetingInfo = viewModel.VideoMeetingInfo,
                    CreatorExpertId = userId, // Assegna l'esperto corrente come creatore
                    // RelatedExpertServiceId = viewModel.SelectedExpertServiceId, // Se usi questa relazione
                };
                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Corso '{newCourse.Title}' creato con successo!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Errore nella creazione del corso. Controlla i campi evidenziati.";
            await PopulateAvailableServicesDropdown(viewModel, userId); // Ripopola dropdown in caso di errore
            return View(viewModel);
        }

        // GET: CourseManagement/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono modificare i corsi.";
                return RedirectToAction(nameof(Index));
            }

            var course = await _context.Courses
                                 // .Include(c => c.RelatedExpertServiceId) // Se usi la relazione
                                 .FirstOrDefaultAsync(c => c.CourseId == id.Value);

            if (course == null) return NotFound();
            if (course.CreatorExpertId != userId) // Verifica proprietà
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a modificare questo corso.";
                return Forbid();
            }

            var viewModel = new EditCourseViewModel
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                StartDateTime = course.StartDateTime.ToLocalTime(), // Converti in LocalTime per l'input
                DurationMinutes = course.DurationMinutes,
                Price = course.Price,
                MaxParticipants = course.MaxParticipants,
                VideoMeetingInfo = course.VideoMeetingInfo,
                CreatorExpertId = course.CreatorExpertId, // Per verifica nel POST
                // SelectedExpertServiceId = course.RelatedExpertServiceId, // Se usi la relazione
            };
            await PopulateAvailableServicesDropdown(viewModel, userId); // Popola dropdown
            return View(viewModel); // Si aspetta Views/CourseManagement/Edit.cshtml
        }

        // POST: CourseManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCourseViewModel viewModel)
        {
            if (id != viewModel.CourseId) return NotFound();

            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            if (viewModel.CreatorExpertId != userId) // Verifica proprietà cruciale
            {
                TempData["ErrorMessage"] = "Tentativo di modifica non autorizzato (creatore non corrispondente).";
                return Forbid();
            }

            if (viewModel.StartDateTime <= DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError(nameof(viewModel.StartDateTime), "La data e l'ora di inizio devono essere future.");
            }
            // Rimuovi proprietà non rilevanti dalla validazione se necessario
            // ModelState.Remove("AvailableServiceTypes");

            if (ModelState.IsValid)
            {
                try
                {
                    var courseToUpdate = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == viewModel.CourseId && c.CreatorExpertId == userId);
                    if (courseToUpdate == null)
                    {
                        TempData["ErrorMessage"] = "Corso non trovato o non autorizzato per la modifica.";
                        return RedirectToAction(nameof(Index));
                    }

                    courseToUpdate.Title = viewModel.Title;
                    courseToUpdate.Description = viewModel.Description;
                    courseToUpdate.StartDateTime = viewModel.StartDateTime.ToUniversalTime(); // Salva in UTC
                    courseToUpdate.DurationMinutes = viewModel.DurationMinutes;
                    courseToUpdate.Price = viewModel.Price;
                    courseToUpdate.MaxParticipants = viewModel.MaxParticipants;
                    courseToUpdate.VideoMeetingInfo = viewModel.VideoMeetingInfo;
                    // courseToUpdate.RelatedExpertServiceId = viewModel.SelectedExpertServiceId; // Se usi la relazione

                    _context.Update(courseToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Corso '{courseToUpdate.Title}' aggiornato con successo!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Courses.AnyAsync(e => e.CourseId == viewModel.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "Il corso è stato modificato da un altro utente. Ricarica la pagina e riprova.");
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Errore nell'aggiornamento del corso. Controlla i campi.";
            await PopulateAvailableServicesDropdown(viewModel, userId); // Ripopola dropdown
            return View(viewModel);
        }

        // --- AZIONI DELETE IMPLEMENTATE ---
        // GET: CourseManagement/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Solo gli esperti approvati possono eliminare i corsi.";
                return RedirectToAction(nameof(Index));
            }

            var course = await _context.Courses
                .Include(c => c.CreatorExpert) // Per mostrare il nome dell'esperto nella conferma
                .Include(c => c.Enrollments)   // Per controllare se ci sono iscritti
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null) return NotFound();
            if (course.CreatorExpertId != userId) // Verifica proprietà
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a eliminare questo corso.";
                return Forbid();
            }
            return View(course); // Si aspetta Views/CourseManagement/Delete.cshtml
        }

        // POST: CourseManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var course = await _context.Courses
                                 .Include(c => c.Enrollments) // Cruciale per il controllo logico
                                 .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                TempData["WarningMessage"] = "Corso non trovato. Potrebbe essere già stato eliminato.";
                return RedirectToAction(nameof(Index));
            }
            if (course.CreatorExpertId != userId) // Verifica proprietà
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a eliminare questo corso.";
                return Forbid();
            }

            // Controllo di business: non eliminare se ci sono utenti iscritti attivamente
            if (course.Enrollments.Any(e => e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.PendingPayment))
            {
                TempData["ErrorMessage"] = $"Impossibile eliminare il corso '{course.Title}' perché ci sono utenti iscritti. Considera di annullare prima le iscrizioni o di archiviare il corso.";
                return RedirectToAction(nameof(Delete), new { id = id }); // Ritorna alla pagina di conferma con l'errore
            }

            try
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Corso '{course.Title}' eliminato con successo.";
            }
            catch (DbUpdateException ex)
            {
                // _logger.LogError(ex, "Errore DB durante l'eliminazione del corso {CourseId}", id);
                TempData["ErrorMessage"] = $"Errore database durante l'eliminazione del corso: {ex.InnerException?.Message ?? ex.Message}.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            return RedirectToAction(nameof(Index));
        }


        // --- Metodo Helper Privato per popolare il Dropdown dei Servizi ---
        private async Task PopulateAvailableServicesDropdown(CreateCourseViewModel viewModel, string userId)
        {
            var activeServices = await _context.ExpertServices
                                        .Where(s => s.ExpertUserId == userId && s.IsEnabled)
                                        .OrderBy(s => s.Title)
                                        .Select(s => new { s.ExpertServiceId, s.Title })
                                        .ToListAsync();
            viewModel.AvailableServiceTypes = new SelectList(activeServices, "ExpertServiceId", "Title", viewModel.SelectedExpertServiceId);
        }

        private async Task PopulateAvailableServicesDropdown(EditCourseViewModel viewModel, string userId) // Overload per EditCourseViewModel
        {
            var activeServices = await _context.ExpertServices
                                        .Where(s => s.ExpertUserId == userId && s.IsEnabled)
                                        .OrderBy(s => s.Title)
                                        .Select(s => new { s.ExpertServiceId, s.Title })
                                        .ToListAsync();
            viewModel.AvailableServiceTypes = new SelectList(activeServices, "ExpertServiceId", "Title", viewModel.SelectedExpertServiceId);
        }
    }
}