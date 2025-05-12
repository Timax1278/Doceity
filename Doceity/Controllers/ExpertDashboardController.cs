// File: Controllers/ExpertDashboardController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants;
using Doceity.ViewModels; // Per CalendarEventViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization; // Per formattare le date

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Expert)] // Solo per Esperti
    public class ExpertDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpertDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Azione per mostrare la pagina della Dashboard con il calendario
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var expert = await _userManager.GetUserAsync(User);
            if (expert == null || !expert.IsApprovedExpert)
            {
                TempData["ErrorMessage"] = "Accesso non autorizzato o account non approvato.";
                return RedirectToAction("Index", "Home");
            }
            return View(); // Si aspetta Views/ExpertDashboard/Index.cshtml
        }


        // Endpoint API per FullCalendar
        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(string start, string end)
        {
            var expert = await _userManager.GetUserAsync(User);
            if (expert == null || !expert.IsApprovedExpert)
            {
                return Unauthorized("Accesso non autorizzato.");
            }

            if (!DateTime.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime startDate) ||
                !DateTime.TryParse(end, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime endDate))
            {
                return BadRequest("Formato date non valido.");
            }

            var events = new List<CalendarEventViewModel>();
            var expertUserId = expert.Id;

            // 1. Recupera Disponibilità
            var availabilities = await _context.Availabilities
                .Where(a => a.ExpertUserId == expertUserId &&
                            a.AvailableDate >= startDate.Date &&
                            a.AvailableDate < endDate.Date)
                .ToListAsync();

            foreach (var av in availabilities)
            {
                events.Add(new CalendarEventViewModel
                {
                    Id = $"avail_{av.AvailabilityId}",
                    Title = av.IsBooked ? $"Prenotato (Slot ID: {av.AvailabilityId})" : $"Disponibile (ID: {av.AvailabilityId})",
                    Start = av.AvailableDate.Add(av.StartTime).ToString("o"),
                    End = av.AvailableDate.Add(av.EndTime).ToString("o"),
                    Color = av.IsBooked ? "#6c757d" : "#198754", // Grigio scuro se prenotato, Verde scuro se disponibile
                    Url = Url.Action("Edit", "ExpertAvailability", new { id = av.AvailabilityId })
                });
            }

            // 2. Recupera Consulenze Accettate
            var consultations = await _context.ConsultationRequests
                .Where(cr => cr.ExpertUserId == expertUserId &&
                             cr.Status == ConsultationRequestStatus.Accepted &&
                             cr.ProposedDateTime >= startDate &&
                             cr.ProposedDateTime < endDate)
                .Include(cr => cr.RequestingUser)
                .Include(cr => cr.RequestedExpertService)
                .ToListAsync();

            foreach (var cons in consultations)
            {
                int durationMinutes = cons.RequestedExpertService?.EstimatedDurationMinutes ?? 60;
                events.Add(new CalendarEventViewModel
                {
                    Id = $"cons_{cons.ConsultationRequestId}",
                    Title = $"Consulenza: {cons.RequestingUser?.FirstName} {cons.RequestingUser?.LastName}",
                    Start = cons.ProposedDateTime.ToString("o"),
                    End = cons.ProposedDateTime.AddMinutes(durationMinutes).ToString("o"),
                    Color = "#0d6efd", // Blu Bootstrap
                    Url = Url.Action("Details", "ConsultationRequests", new { id = cons.ConsultationRequestId })
                });
            }

            // 3. Recupera Corsi Programmati
            var courses = await _context.Courses
                .Where(c => c.CreatorExpertId == expertUserId &&
                            c.StartDateTime >= startDate &&
                            c.StartDateTime < endDate)
                .ToListAsync();

            foreach (var course in courses)
            {
                events.Add(new CalendarEventViewModel
                {
                    Id = $"course_{course.CourseId}",
                    Title = $"Corso: {course.Title}",
                    Start = course.StartDateTime.ToString("o"),
                    End = course.StartDateTime.AddMinutes(course.DurationMinutes).ToString("o"),
                    Color = "#ffc107", // Giallo/Arancio Bootstrap (colore warning)
                    Url = Url.Action("Edit", "CourseManagement", new { id = course.CourseId })
                });
            }

            return Ok(events);
        }
    }
}