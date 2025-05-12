// File: Controllers/FindExpertsController.cs
using Doceity.Data;
using Doceity.Models; // Per ApplicationUser
using Microsoft.AspNetCore.Authorization; // Per [AllowAnonymous] o [Authorize]
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Per List
using Doceity.ViewModels; // Se decidi di usare ViewModel per la lista

namespace Doceity.Controllers
{
    [AllowAnonymous] // Permetti a chiunque (anche non loggato) di vedere la lista esperti
                     // Se vuoi che solo utenti loggati la vedano, cambia in [Authorize] o [Authorize(Roles = "User,Admin")]
    public class FindExpertsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Anche se non strettamente necessario qui, può essere utile

        public FindExpertsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: FindExperts
        // Questa azione mostrerà una lista di esperti approvati
        public async Task<IActionResult> Index()
        {
            // Recupera tutti gli utenti che hanno il ruolo "Expert" e sono approvati
            // NOTA: Questo modo di ottenere utenti per ruolo può essere meno performante su DB grandi.
            // Un approccio migliore potrebbe essere avere una tabella di join o una proprietà diretta in ApplicationUser.
            // Per ora, questo è il modo più diretto con Identity base.

            var expertUsers = await _userManager.GetUsersInRoleAsync(Constants.Roles.Expert);

            var approvedExperts = expertUsers
                                    .Where(u => u.IsApprovedExpert)
                                    .OrderBy(u => u.LastName)
                                    .ThenBy(u => u.FirstName)
                                    .ToList(); // Converti in lista per passarla alla vista

            // Opzionale: Potresti voler creare un ViewModel per ogni esperto
            // per non passare l'intero ApplicationUser alla vista e includere solo i dati necessari.
            // Es. List<ExpertPublicProfileViewModel> expertViewModels = ...;
            // Per ora passiamo la lista di ApplicationUser.

            return View(approvedExperts); // Passa la lista alla vista Views/FindExperts/Index.cshtml
        }

        // Placeholder per una futura pagina di dettaglio dell'esperto
        public async Task<IActionResult> Details(string id) // id sarà l'ExpertUserId
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var expert = await _userManager.FindByIdAsync(id);
            if (expert == null || !await _userManager.IsInRoleAsync(expert, Constants.Roles.Expert) || !expert.IsApprovedExpert)
            {
                return NotFound(); // Esperto non trovato, non è un esperto, o non è approvato
            }

            // Potresti caricare i servizi dell'esperto qui:
            // var expertServices = await _context.ExpertServices
            //                              .Where(s => s.ExpertUserId == id && s.IsEnabled)
            //                              .ToListAsync();
            // ViewData["ExpertServices"] = expertServices;


            // TODO: Creare un ViewModel per il dettaglio esperto
            return View(expert); // Per ora passiamo ApplicationUser
        }
    }
}