// File: Controllers/UserDashboardController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants;
using Doceity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.User)] // Accessibile solo agli utenti con ruolo "User"
    public class UserDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: UserDashboard  (o UserDashboard/Index)
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); // Non dovrebbe accadere a causa di [Authorize]
            }

            var viewModel = new UserDashboardViewModel();
            var now = DateTime.UtcNow;

            // 1. Prossime Consulenze Accettate
            viewModel.UpcomingAcceptedConsultations = await _context.ConsultationRequests
                .Where(r => r.RequestingUserId == currentUser.Id &&
                            r.Status == ConsultationRequestStatus.Accepted &&
                            r.ProposedDateTime > now) // Solo quelle future
                .Include(r => r.Expert)
                .Include(r => r.RequestedExpertService)
                .OrderBy(r => r.ProposedDateTime)
                .Take(5) // Mostra solo le prossime 5, per esempio
                .ToListAsync();

            // 2. Prossimi Corsi Iscritti
            viewModel.UpcomingEnrolledCourses = await _context.CourseEnrollments
                .Where(ce => ce.UserId == currentUser.Id &&
                             ce.Status == EnrollmentStatus.Enrolled &&
                             ce.Course.StartDateTime > now) // Solo corsi futuri
                .Include(ce => ce.Course)
                    .ThenInclude(c => c.CreatorExpert)
                .OrderBy(ce => ce.Course.StartDateTime)
                .Take(5) // Mostra solo i prossimi 5, per esempio
                .ToListAsync();

            // 3. Ultime Richieste di Consulenza Inviate (Pending o recenti)
            viewModel.RecentConsultationRequests = await _context.ConsultationRequests
                .Where(r => r.RequestingUserId == currentUser.Id)
                .Include(r => r.Expert)
                // Mostra prima le Pending, poi le altre per data decrescente
                .OrderBy(r => r.Status == ConsultationRequestStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.RequestTimestamp)
                .Take(5) // Mostra le ultime 5
                .ToListAsync();

            return View(viewModel); // Passa il ViewModel alla vista Views/UserDashboard/Index.cshtml
        }
    }
}