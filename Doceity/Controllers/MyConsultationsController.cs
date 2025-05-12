// File: Controllers/MyConsultationsController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants; // Per Roles
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Per DateTime
using System.Linq;
using System.Threading.Tasks;
// using Doceity.Services; // Decommenta se e quando userai IEmailSender
// using Microsoft.Extensions.Logging; // Decommenta se e quando userai ILogger

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.User)] // Solo utenti con ruolo "User" possono accedere
    public class MyConsultationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        // private readonly IEmailSender _emailSender; // Decommenta per l'invio di email
        // private readonly ILogger<MyConsultationsController> _logger; // Decommenta per il logging

        public MyConsultationsController(ApplicationDbContext context,
                                         UserManager<ApplicationUser> userManager
                                         /*, IEmailSender emailSender, ILogger<MyConsultationsController> logger */) // Aggiungi IEmailSender e ILogger se li usi
        {
            _context = context;
            _userManager = userManager;
            // _emailSender = emailSender;
            // _logger = logger;
        }

        // GET: MyConsultations  (o MyConsultations/Index)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var myRequests = await _context.ConsultationRequests
                .Where(r => r.RequestingUserId == currentUser.Id)
                .Include(r => r.Expert)
                .Include(r => r.RequestedExpertService)
                .Include(r => r.OriginalAvailabilitySlot) // Includi per accedere a IsBooked nella vista se necessario
                .OrderByDescending(r => r.RequestTimestamp)
                .ToListAsync();

            return View(myRequests);
        }

        // GET: MyConsultations/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var request = await _context.ConsultationRequests
                .Include(r => r.Expert)
                .Include(r => r.RequestedExpertService)
                .Include(r => r.OriginalAvailabilitySlot) // Includi per accedere a IsBooked nella vista se necessario
                .FirstOrDefaultAsync(r => r.ConsultationRequestId == id && r.RequestingUserId == currentUser.Id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Richiesta non trovata o non autorizzato a visualizzarla.";
                return NotFound();
            }
            return View(request);
        }

        // POST: MyConsultations/CancelRequest/5
        // Permette all'utente di cancellare una sua richiesta Pending o Accepted (con preavviso)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var requestToCancel = await _context.ConsultationRequests
                                        .Include(r => r.Expert) // Per notificare l'esperto
                                        .Include(r => r.RequestedExpertService) // Per info aggiuntive nell'email
                                        .Include(r => r.OriginalAvailabilitySlot) // Cruciale per liberare lo slot
                                        .FirstOrDefaultAsync(r => r.ConsultationRequestId == id);

            if (requestToCancel == null)
            {
                TempData["ErrorMessage"] = "La richiesta che stai cercando di cancellare non è stata trovata.";
                return RedirectToAction(nameof(Index));
            }

            if (requestToCancel.RequestingUserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a cancellare questa richiesta.";
                return Forbid();
            }

            // --- LOGICA DI CANCELLABILITÀ MIGLIORATA ---
            bool canCancel = false;
            string cancellationIssueMessage = string.Empty;

            if (requestToCancel.Status == ConsultationRequestStatus.Pending)
            {
                canCancel = true;
            }
            else if (requestToCancel.Status == ConsultationRequestStatus.Accepted)
            {
                // Esempio: L'utente può cancellare una richiesta accettata fino a 24 ore prima dell'inizio.
                // Questo valore (24 ore) dovrebbe essere configurabile o una costante definita.
                const int hoursNoticeRequiredForUserCancellation = 24;
                if (requestToCancel.ProposedDateTime > DateTime.UtcNow.AddHours(hoursNoticeRequiredForUserCancellation))
                {
                    canCancel = true;
                }
                else
                {
                    cancellationIssueMessage = $"Questa consulenza accettata è troppo vicina (mancano meno di {hoursNoticeRequiredForUserCancellation} ore) per essere cancellata autonomamente. Contatta direttamente l'esperto o il supporto.";
                }
            }
            else // Già Rejected, Cancelled, o altro stato non cancellabile dall'utente
            {
                cancellationIssueMessage = "Questa richiesta non può più essere cancellata perché il suo stato non lo permette (es. già rifiutata o cancellata).";
            }
            // --- FINE LOGICA CANCELLABILITÀ ---

            if (!canCancel)
            {
                TempData["WarningMessage"] = cancellationIssueMessage; // Usa il messaggio specifico
                return RedirectToAction(nameof(Details), new { id = requestToCancel.ConsultationRequestId }); // Torna ai dettagli della richiesta
            }

            // Se la richiesta era ACCETTATA e legata a uno slot di disponibilità, LIBERA lo slot.
            if (requestToCancel.Status == ConsultationRequestStatus.Accepted && requestToCancel.OriginalAvailabilityId.HasValue)
            {
                // OriginalAvailabilitySlot è già stato caricato grazie a .Include()
                if (requestToCancel.OriginalAvailabilitySlot != null && requestToCancel.OriginalAvailabilitySlot.IsBooked)
                {
                    requestToCancel.OriginalAvailabilitySlot.IsBooked = false;
                    _context.Update(requestToCancel.OriginalAvailabilitySlot);
                    // _logger?.LogInformation("Availability slot {AvailabilityId} marked as NOT booked due to user cancellation of ConsultationRequest {ConsultationRequestId}.", requestToCancel.OriginalAvailabilitySlot.AvailabilityId, requestToCancel.ConsultationRequestId);
                }
            }

            requestToCancel.Status = ConsultationRequestStatus.Cancelled; // O un enum specifico come CancelledByUser
            // requestToCancel.CancellationTimestamp = DateTime.UtcNow; // Potresti aggiungere questi campi
            // requestToCancel.CancelledByUserId = currentUser.Id;

            _context.ConsultationRequests.Update(requestToCancel);

            try
            {
                await _context.SaveChangesAsync(); // Salva le modifiche (sia per ConsultationRequest che per Availability)
            }
            catch (DbUpdateException ex)
            {
                // _logger?.LogError(ex, "DbUpdateException while cancelling ConsultationRequest {ConsultationRequestId} by user {UserId}", requestToCancel.ConsultationRequestId, currentUser.Id);
                TempData["ErrorMessage"] = "Si è verificato un errore durante il salvataggio della cancellazione. Riprova.";
                return RedirectToAction(nameof(Details), new { id = requestToCancel.ConsultationRequestId });
            }


            // --- TODO Opzionale: Notificare l'esperto della cancellazione via email ---
            // if (_emailSender != null && requestToCancel.Expert != null)
            // {
            //     try
            //     {
            //         // ... Logica invio email all'esperto ...
            //     }
            //     catch (Exception ex) { /* _logger?.LogError(ex, "..."); */ }
            // }
            // --- Fine TODO Email ---

            TempData["SuccessMessage"] = "La tua richiesta di consulenza è stata cancellata con successo.";
            return RedirectToAction(nameof(Index));
        }
    }
}