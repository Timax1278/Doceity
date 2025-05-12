// File: Controllers/ConsultationRequestsController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants; // Per Roles
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Expert)]
    public class ConsultationRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConsultationRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        [HttpGet]
        public async Task<IActionResult> Index(string statusFilter = null)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved)
            {
                TempData["ErrorMessage"] = "Il tuo account esperto non è ancora stato approvato o l'accesso è limitato.";
                return RedirectToAction("Index", "Home");
            }
            IQueryable<ConsultationRequest> query = _context.ConsultationRequests
                .Where(r => r.ExpertUserId == userId);

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<ConsultationRequestStatus>(statusFilter, true, out var status))
            {
                query = query.Where(r => r.Status == status);
                ViewData["CurrentFilter"] = statusFilter;
            }

            var requests = await query
                .Include(r => r.RequestingUser)
                .Include(r => r.RequestedExpertService)
                .Include(r => r.OriginalAvailabilitySlot)
                .OrderByDescending(r => r.RequestTimestamp)
                .ToListAsync();
            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var request = await _context.ConsultationRequests
                .Include(r => r.RequestingUser)
                .Include(r => r.RequestedExpertService)
                .Include(r => r.OriginalAvailabilitySlot)
                .FirstOrDefaultAsync(m => m.ConsultationRequestId == id.Value);

            if (request == null) return NotFound();
            if (request.ExpertUserId != userId)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato a visualizzare questa richiesta.";
                return Forbid();
            }
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id, string responseMessage)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var request = await _context.ConsultationRequests
                                  .Include(r => r.RequestingUser)
                                  .Include(r => r.OriginalAvailabilitySlot) // Assicurati di includere questo se non è già caricato
                                  .FirstOrDefaultAsync(r => r.ConsultationRequestId == id);

            if (request == null) return NotFound();
            if (request.ExpertUserId != userId || request.Status != ConsultationRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Questa richiesta non può essere accettata.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = ConsultationRequestStatus.Accepted;
            request.ResponseTimestamp = DateTime.UtcNow;
            request.ExpertResponseMessage = responseMessage?.Trim();
            if (string.IsNullOrEmpty(request.VideoRoomIdentifier))
            {
                request.VideoRoomIdentifier = $"cons_{Guid.NewGuid().ToString("N").Substring(0, 16)}";
            }
            _context.Update(request);

            if (request.OriginalAvailabilityId.HasValue)
            {
                // Se OriginalAvailabilitySlot non è stato caricato con Include, caricalo ora.
                // Ma se è stato già incluso (come nel GET Details o Index se ci passi), puoi usarlo direttamente.
                var originalSlot = request.OriginalAvailabilitySlot ?? await _context.Availabilities.FindAsync(request.OriginalAvailabilityId.Value);
                if (originalSlot != null && originalSlot.ExpertUserId == userId)
                {
                    if (!originalSlot.IsBooked)
                    {
                        originalSlot.IsBooked = true;
                        _context.Update(originalSlot);
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Richiesta accettata, ma lo slot risultava già prenotato.";
                    }
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Richiesta accettata! Identificativo stanza video: " + request.VideoRoomIdentifier;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string responseMessage)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var request = await _context.ConsultationRequests
                                  .Include(r => r.RequestingUser)
                                  .FirstOrDefaultAsync(r => r.ConsultationRequestId == id);

            if (request == null) return NotFound();
            if (request.ExpertUserId != userId || request.Status != ConsultationRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Questa richiesta non può essere rifiutata.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrWhiteSpace(responseMessage))
            {
                TempData["ErrorMessage"] = "Una motivazione è richiesta per rifiutare.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            request.Status = ConsultationRequestStatus.Rejected;
            request.ResponseTimestamp = DateTime.UtcNow;
            request.ExpertResponseMessage = responseMessage.Trim();
            _context.Update(request);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Richiesta rifiutata.";
            return RedirectToAction(nameof(Index));
        }

        // --- NUOVE AZIONI PER L'ESPERTO PER ANNULLARE UNA CONSULENZA ACCETTATA ---
        [HttpGet]
        public async Task<IActionResult> CancelByExpert(int? id)
        {
            if (id == null) return NotFound();

            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var requestToCancel = await _context.ConsultationRequests
                .Include(r => r.RequestingUser)
                .Include(r => r.OriginalAvailabilitySlot)
                .FirstOrDefaultAsync(r => r.ConsultationRequestId == id.Value);

            if (requestToCancel == null) return NotFound();
            if (requestToCancel.ExpertUserId != userId)
            {
                TempData["ErrorMessage"] = "Non autorizzato.";
                return Forbid();
            }
            if (requestToCancel.Status != ConsultationRequestStatus.Accepted)
            {
                TempData["WarningMessage"] = "Solo le consulenze Accettate possono essere annullate da qui.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            // Aggiungi qui logica per il preavviso se necessario, es:
            // if (requestToCancel.ProposedDateTime <= DateTime.UtcNow.AddHours(4)) // Non puoi cancellare se mancano meno di 4 ore
            // {
            //    TempData["ErrorMessage"] = "Troppo tardi per annullare la consulenza.";
            //    return RedirectToAction(nameof(Details), new { id = id });
            // }
            return View(requestToCancel); // Passa a Views/ConsultationRequests/CancelByExpert.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelByExpertConfirmed(int id, string cancellationReason)
        {
            var (isApproved, userId) = await CheckExpertStatusAsync();
            if (!isApproved) return Forbid();

            var requestToCancel = await _context.ConsultationRequests
                .Include(r => r.RequestingUser)
                .Include(r => r.OriginalAvailabilitySlot)
                .FirstOrDefaultAsync(r => r.ConsultationRequestId == id);

            if (requestToCancel == null) return NotFound();
            if (requestToCancel.ExpertUserId != userId || requestToCancel.Status != ConsultationRequestStatus.Accepted)
            {
                TempData["ErrorMessage"] = "Impossibile annullare.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["ErrorMessage"] = "Motivazione obbligatoria per annullare.";
                // Dovresti ritornare alla vista CancelByExpert passando il modello
                // return View("CancelByExpert", requestToCancel);
                // Per ora, per semplicità, reindirizzo a Details per mostrare l'errore
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Libera lo slot Availability
            if (requestToCancel.OriginalAvailabilityId.HasValue)
            {
                var originalSlot = requestToCancel.OriginalAvailabilitySlot;
                if (originalSlot != null && originalSlot.IsBooked)
                {
                    originalSlot.IsBooked = false;
                    _context.Update(originalSlot);
                }
            }

            requestToCancel.Status = ConsultationRequestStatus.Cancelled; // O un nuovo "CancelledByExpert"
            requestToCancel.ExpertResponseMessage = $"ANNULATA DALL'ESPERTO: {cancellationReason}";
            requestToCancel.ResponseTimestamp = DateTime.UtcNow; // Aggiorna timestamp
            _context.Update(requestToCancel);
            await _context.SaveChangesAsync();

            // TODO: Notificare l'UTENTE dell'annullamento

            TempData["SuccessMessage"] = "Consulenza annullata e slot liberato.";
            return RedirectToAction(nameof(Index));
        }
    }
}