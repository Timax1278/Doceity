// File: Controllers/ExpertProfileController.cs
using Doceity.Data;
using Doceity.Models;
using Doceity.Constants;
using Doceity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Expert)] // Solo Esperti
    public class ExpertProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpertProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<ApplicationUser> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);

        // GET: ExpertProfile/ManageSpecializations
        [HttpGet]
        public async Task<IActionResult> ManageSpecializations()
        {
            var expert = await GetCurrentUserAsync();
            if (expert == null || !expert.IsApprovedExpert)
            {
                // Potresti reindirizzare o mostrare un messaggio specifico
                TempData["ErrorMessage"] = "Accesso non autorizzato o account non approvato.";
                return RedirectToAction("Index", "Home");
            }

            var allSpecializations = await _context.Specializations.OrderBy(s => s.Name).ToListAsync();
            var expertSpecializationIds = await _context.ExpertSpecializations
                                                .Where(es => es.ApplicationUserId == expert.Id)
                                                .Select(es => es.SpecializationId)
                                                .ToListAsync();

            var viewModel = new ExpertManageSpecializationsViewModel
            {
                ExpertUserId = expert.Id,
                ExpertFullName = $"{expert.FirstName} {expert.LastName}",
                AvailableSpecializations = allSpecializations.Select(s => new SpecializationSelectionViewModel
                {
                    SpecializationId = s.SpecializationId,
                    Name = s.Name,
                    IsSelected = expertSpecializationIds.Contains(s.SpecializationId)
                }).ToList()
            };

            return View(viewModel); // Si aspetta Views/ExpertProfile/ManageSpecializations.cshtml
        }

        // POST: ExpertProfile/ManageSpecializations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageSpecializations(ExpertManageSpecializationsViewModel viewModel)
        {
            var expert = await GetCurrentUserAsync();
            if (expert == null || !expert.IsApprovedExpert || expert.Id != viewModel.ExpertUserId)
            {
                TempData["ErrorMessage"] = "Operazione non autorizzata.";
                return RedirectToAction("Index", "Home");
            }

            // Non c'è molto da validare nel ViewModel stesso, la logica è nell'aggiornamento
            // delle selezioni. Ma potresti aggiungere validazioni se necessario.

            // Rimuovi tutte le specializzazioni esistenti per questo esperto
            var existingExpertSpecializations = _context.ExpertSpecializations
                                                    .Where(es => es.ApplicationUserId == expert.Id);
            _context.ExpertSpecializations.RemoveRange(existingExpertSpecializations);

            // Aggiungi le nuove specializzazioni selezionate
            if (viewModel.AvailableSpecializations != null)
            {
                foreach (var specSelection in viewModel.AvailableSpecializations)
                {
                    if (specSelection.IsSelected)
                    {
                        _context.ExpertSpecializations.Add(new ExpertSpecialization
                        {
                            ApplicationUserId = expert.Id,
                            SpecializationId = specSelection.SpecializationId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Le tue specializzazioni sono state aggiornate con successo.";
            return RedirectToAction(nameof(ManageSpecializations));
        }

        // TODO: Aggiungere azioni per modificare altri campi del profilo (Biografia, Foto, ecc.)
        // Esempio:
        // [HttpGet]
        // public async Task<IActionResult> EditProfileDetails() { ... }
        // [HttpPost]
        // public async Task<IActionResult> EditProfileDetails(EditProfileDetailsViewModel vm) { ... }
    }
}