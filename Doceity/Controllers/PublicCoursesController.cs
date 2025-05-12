// File: Controllers/PublicCoursesController.cs
using Doceity.Data;
using Doceity.Models; // Per Course e ApplicationUser
using Microsoft.AspNetCore.Authorization; // Per [AllowAnonymous]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Doceity.Controllers
{
    [AllowAnonymous] // Permetti a chiunque di accedere a questo controller
    public class PublicCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PublicCoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PublicCourses  (o /PublicCourses/Index)
        // Mostra una lista di tutti i corsi disponibili (con data futura)
        public async Task<IActionResult> Index()
        {
            // Recupera solo i corsi che devono ancora iniziare
            // Potresti aggiungere altri filtri qui, es. se un corso è "pubblicato" o "attivo"
            var availableCourses = await _context.Courses
                .Where(c => c.StartDateTime > DateTime.UtcNow) // Solo corsi futuri
                .Include(c => c.CreatorExpert) // Per mostrare il nome dell'esperto
                .OrderBy(c => c.StartDateTime) // Ordina per data di inizio più vicina
                .ToListAsync();

            // Potresti voler usare un ViewModel qui se vuoi passare dati più specifici o aggregati alla vista
            // Per ora, passiamo direttamente la lista dei modelli Course.
            return View(availableCourses); // Si aspetta una vista in Views/PublicCourses/Index.cshtml
        }

        // GET: PublicCourses/Details/5
        // Mostra i dettagli di un singolo corso
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // O RedirectToAction("Index") con un messaggio di errore
            }

            var course = await _context.Courses
                .Include(c => c.CreatorExpert) // Includi l'esperto per mostrare i suoi dettagli
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null)
            {
                TempData["ErrorMessage"] = "Corso non trovato.";
                return NotFound(); // O RedirectToAction("Index")
            }

            // Anche qui, un ViewModel dedicato potrebbe essere utile per la vista dei dettagli.
            return View(course); // Si aspetta una vista in Views/PublicCourses/Details.cshtml
        }
    }
}