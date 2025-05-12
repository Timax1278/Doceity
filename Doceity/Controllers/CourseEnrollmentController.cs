// File: Controllers/CourseEnrollmentController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Doceity.Controllers
{
    [Authorize] // Richiede che l'utente sia loggato per iscriversi
    public class CourseEnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseEnrollmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: CourseEnrollment/Enroll/{courseId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); // Non dovrebbe succedere con [Authorize]
            }

            // Solo utenti con ruolo "User" possono iscriversi tramite questo flusso
            if (!await _userManager.IsInRoleAsync(currentUser, Roles.User))
            {
                TempData["ErrorMessage"] = "Solo gli utenti standard possono iscriversi ai corsi tramite questa pagina.";
                return RedirectToAction("Details", "PublicCourses", new { id = courseId });
            }

            var course = await _context.Courses
                                 .Include(c => c.Enrollments) // Includi per contare le iscrizioni
                                 .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                TempData["ErrorMessage"] = "Corso non trovato.";
                return RedirectToAction("Index", "PublicCourses");
            }

            // Controlla se il corso è già iniziato
            if (course.StartDateTime <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Non è più possibile iscriversi a questo corso perché è già iniziato o è passato.";
                return RedirectToAction("Details", "PublicCourses", new { id = courseId });
            }

            // Controlla se l'utente è già iscritto
            bool alreadyEnrolled = await _context.CourseEnrollments
                                         .AnyAsync(ce => ce.CourseId == courseId && ce.UserId == currentUser.Id && ce.Status == EnrollmentStatus.Enrolled);
            if (alreadyEnrolled)
            {
                TempData["WarningMessage"] = "Sei già iscritto a questo corso.";
                return RedirectToAction("Details", "PublicCourses", new { id = courseId });
            }

            // Controlla se ci sono posti disponibili (se MaxParticipants è impostato)
            if (course.MaxParticipants.HasValue)
            {
                // Conta solo le iscrizioni attive (Enrolled, PendingPayment)
                int currentEnrollmentCount = course.Enrollments
                                                .Count(e => e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.PendingPayment);
                if (currentEnrollmentCount >= course.MaxParticipants.Value)
                {
                    TempData["ErrorMessage"] = "Spiacenti, questo corso ha raggiunto il numero massimo di partecipanti.";
                    return RedirectToAction("Details", "PublicCourses", new { id = courseId });
                }
            }

            // Se tutti i controlli passano, crea l'iscrizione
            var enrollment = new CourseEnrollment
            {
                CourseId = courseId,
                UserId = currentUser.Id,
                EnrollmentDate = DateTime.UtcNow,
                Status = EnrollmentStatus.Enrolled // O PendingPayment se implementi pagamenti
                // PricePaid = course.Price // Salva il prezzo al momento dell'iscrizione
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // TODO: Inviare email di conferma iscrizione all'utente
            // TODO: Inviare notifica all'esperto del corso (course.CreatorExpertId) per una nuova iscrizione (opzionale)

            TempData["SuccessMessage"] = $"Complimenti! Ti sei iscritto con successo al corso: {course.Title}.";
            // Reindirizza alla pagina dei dettagli del corso o a "I Miei Corsi Iscritti"
            return RedirectToAction("Details", "PublicCourses", new { id = courseId });
        }


        // GET: CourseEnrollment/MyEnrolledCourses
        // Azione per l'utente per vedere i corsi a cui è iscritto
        [HttpGet]
        public async Task<IActionResult> MyEnrolledCourses()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var enrollments = await _context.CourseEnrollments
                .Where(ce => ce.UserId == currentUser.Id && (ce.Status == EnrollmentStatus.Enrolled || ce.Status == EnrollmentStatus.PendingPayment))
                .Include(ce => ce.Course)
                    .ThenInclude(c => c.CreatorExpert) // Per mostrare nome esperto
                .OrderByDescending(ce => ce.EnrollmentDate)
                .ToListAsync();

            // Potresti voler creare un ViewModel per questa lista per passare dati più strutturati
            return View(enrollments); // Si aspetta Views/CourseEnrollment/MyEnrolledCourses.cshtml
        }

        // TODO Opzionale: Azione per l'UTENTE per CANCELLARE l'iscrizione a un corso (se permesso e prima dell'inizio)
        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> CancelEnrollment(int enrollmentId) { ... }
    }
}