// File: Controllers/ConsultationBookingController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization; // Per DateTime.TryParse

namespace Doceity.Controllers
{
    [Authorize(Roles = Constants.Roles.User + "," + Constants.Roles.Admin)]
    public class ConsultationBookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConsultationBookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> CreateRequest(string expertUserId, int? expertServiceId)
        {
            if (string.IsNullOrEmpty(expertUserId))
            {
                TempData["ErrorMessage"] = "È necessario specificare un esperto per la richiesta.";
                return RedirectToAction("Index", "FindExperts");
            }

            var expert = await _userManager.FindByIdAsync(expertUserId);
            if (expert == null || !await _userManager.IsInRoleAsync(expert, Constants.Roles.Expert) || !expert.IsApprovedExpert)
            {
                TempData["ErrorMessage"] = "L'esperto selezionato non è valido o non è disponibile.";
                return RedirectToAction("Index", "FindExperts");
            }

            var viewModel = new CreateConsultationRequestViewModel
            {
                ExpertUserId = expert.Id,
                ExpertFullName = $"{expert.FirstName} {expert.LastName}",
                // SelectedSlot sarà popolato dalla vista se l'utente sceglie uno slot,
                // o dall'input manuale. Il default per ProposedDateTime (ora ParsedProposedDateTime)
                // non è più strettamente necessario qui nel GET se ci si affida alla vista.
            };

            if (expertServiceId.HasValue)
            {
                var service = await _context.ExpertServices
                                      .FirstOrDefaultAsync(s => s.ExpertServiceId == expertServiceId.Value && s.ExpertUserId == expertUserId && s.IsEnabled);
                if (service != null)
                {
                    viewModel.RequestedExpertServiceId = service.ExpertServiceId;
                    viewModel.ExpertServiceTitle = service.Title;
                }
                else
                {
                    TempData["WarningMessage"] = "Il servizio specifico richiesto non è stato trovato o non è attivo. Puoi comunque inviare una richiesta generica all'esperto.";
                }
            }

            // Carica le disponibilità dell'esperto
            var today = DateTime.UtcNow.Date;
            viewModel.ExpertAvailabilities = await _context.Availabilities
                .Where(a => a.ExpertUserId == expertUserId && a.AvailableDate >= today /*&& !a.IsBooked*/) // Aggiungi !a.IsBooked qui
                .OrderBy(a => a.AvailableDate).ThenBy(a => a.StartTime)
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(CreateConsultationRequestViewModel viewModel)
        {
            var requestingUser = await _userManager.GetUserAsync(User);
            if (requestingUser == null) return Challenge();

            // --- PARSING DI SelectedSlot ---
            if (!string.IsNullOrEmpty(viewModel.SelectedSlot))
            {
                if (viewModel.SelectedSlot.Contains("|")) // Formato: "AvailabilityId|ISODateTimeString"
                {
                    var parts = viewModel.SelectedSlot.Split('|');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int availId) &&
                        DateTime.TryParse(parts[1], null, DateTimeStyles.RoundtripKind, out DateTime dt))
                    {
                        viewModel.ParsedAvailabilityId = availId;
                        viewModel.ParsedProposedDateTime = dt; // Questa è già UTC dal formato "o"
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(viewModel.SelectedSlot), "Lo slot di disponibilità selezionato non è valido.");
                    }
                }
                else // Tentativo di parsare come input datetime-local (che dovrebbe essere in ora locale del client)
                {
                    if (DateTime.TryParse(viewModel.SelectedSlot, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dtLocal))
                    {
                        viewModel.ParsedProposedDateTime = dtLocal.ToUniversalTime(); // Converti in UTC
                        viewModel.ParsedAvailabilityId = null; // Nessuna availability specifica selezionata
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(viewModel.SelectedSlot), "Il formato della data e ora proposte non è valido.");
                    }
                }
            }
            else
            {
                // Se SelectedSlot è vuoto, ma il campo è Required, la validazione degli attributi lo intercetterà.
                // Se non fosse Required, qui potresti impostare un default o un errore specifico.
                ModelState.AddModelError(nameof(viewModel.SelectedSlot), "Devi selezionare o proporre una data e un'ora.");
            }
            // --- FINE PARSING ---


            var expert = await _userManager.FindByIdAsync(viewModel.ExpertUserId);
            if (expert == null || !await _userManager.IsInRoleAsync(expert, Constants.Roles.Expert) || !expert.IsApprovedExpert)
            {
                ModelState.AddModelError("", "L'esperto selezionato per la richiesta non è valido o non è più disponibile.");
            }
            else // Ripopola per la vista in caso di errore
            {
                viewModel.ExpertFullName = $"{expert.FirstName} {expert.LastName}";
                if (viewModel.RequestedExpertServiceId.HasValue)
                {
                    var service = await _context.ExpertServices.FirstOrDefaultAsync(s => s.ExpertServiceId == viewModel.RequestedExpertServiceId.Value && s.ExpertUserId == expert.Id);
                    if (service != null) viewModel.ExpertServiceTitle = service.Title;
                }
            }

            // Ricontrolla validazioni che dipendono da ParsedProposedDateTime
            if (viewModel.ParsedProposedDateTime != default(DateTime) && viewModel.ParsedProposedDateTime <= DateTime.UtcNow.AddMinutes(30))
            {
                ModelState.AddModelError(nameof(viewModel.SelectedSlot), "La data e l'ora proposte devono essere future.");
            }
            if (requestingUser.Id == viewModel.ExpertUserId)
            {
                ModelState.AddModelError("", "Non puoi inviare una richiesta di consulenza a te stesso.");
            }

            // Verifica se lo slot selezionato (se ne è stato selezionato uno da Availability) è ancora disponibile e non prenotato
            if (ModelState.IsValid && viewModel.ParsedAvailabilityId.HasValue)
            {
                var chosenSlot = await _context.Availabilities.FirstOrDefaultAsync(a => a.AvailabilityId == viewModel.ParsedAvailabilityId.Value);
                if (chosenSlot == null || chosenSlot.IsBooked || chosenSlot.ExpertUserId != viewModel.ExpertUserId)
                {
                    ModelState.AddModelError(nameof(viewModel.SelectedSlot), "Lo slot selezionato non è più disponibile o valido.");
                }
                // Verifica anche che il ParsedProposedDateTime corrisponda effettivamente allo slot, per sicurezza
                if (chosenSlot != null && chosenSlot.AvailableDate.Add(chosenSlot.StartTime) != viewModel.ParsedProposedDateTime)
                {
                    ModelState.AddModelError(nameof(viewModel.SelectedSlot), "La data/ora selezionata non corrisponde a uno slot valido dell'esperto.");
                }
            }


            if (ModelState.IsValid)
            {
                var consultationRequest = new ConsultationRequest
                {
                    RequestingUserId = requestingUser.Id,
                    ExpertUserId = viewModel.ExpertUserId,
                    RequestedExpertServiceId = viewModel.RequestedExpertServiceId,
                    ProposedDateTime = viewModel.ParsedProposedDateTime, // È già UTC
                    OriginalAvailabilityId = viewModel.ParsedAvailabilityId, // Salva l'ID dello slot
                    UserMessage = viewModel.UserMessage,
                    RequestTimestamp = DateTime.UtcNow,
                    Status = ConsultationRequestStatus.Pending
                };

                _context.ConsultationRequests.Add(consultationRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "La tua richiesta di consulenza è stata inviata con successo!";
                return RedirectToAction("Index", "FindExperts");
            }

            TempData["ErrorMessage"] = "Non è stato possibile inviare la richiesta. Controlla gli errori.";
            // Ripopola le disponibilità se si ritorna alla vista
            if (expert != null) // Solo se l'esperto è valido
            {
                var today = DateTime.UtcNow.Date;
                viewModel.ExpertAvailabilities = await _context.Availabilities
                   .Where(a => a.ExpertUserId == expert.Id && a.AvailableDate >= today /*&& !a.IsBooked*/)
                   .OrderBy(a => a.AvailableDate).ThenBy(a => a.StartTime)
                   .ToListAsync();
            }
            return View(viewModel);
        }
    }
}