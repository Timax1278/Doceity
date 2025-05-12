using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Doceity.Constants; // Per Roles
using Doceity.Models;   // Per ApplicationUser
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.UI.Services; // Per IEmailSender
using System; // Per Exception

namespace Doceity.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IEmailSender _emailSender;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        // GET: Admin/ApproveExperts
        public async Task<IActionResult> ApproveExperts()
        {
            // ... (Codice ApproveExperts GET come prima) ...
            _logger.LogInformation("AdminController.ApproveExperts GET: Fetching pending experts.");
            List<ApplicationUser> pendingExperts = new List<ApplicationUser>();
            try
            {
                var usersInExpertRole = await _userManager.GetUsersInRoleAsync(Roles.Expert);
                pendingExperts = usersInExpertRole
                                    .Where(u => !u.IsApprovedExpert)
                                    .OrderBy(u => u.Email)
                                    .ToList();
                _logger.LogInformation($"AdminController.ApproveExperts GET: Found {pendingExperts.Count} experts pending approval.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminController.ApproveExperts GET: Error retrieving pending experts.");
                TempData["ErrorMessage"] = "Errore durante il caricamento degli esperti in attesa.";
                return View(new List<ApplicationUser>());
            }
            return View(pendingExperts);
        }

        // POST: Admin/ApproveExpert/{userId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveExpert(string userId)
        {
            // ... (Codice ApproveExpert POST come prima) ...
            _logger.LogInformation($"AdminController.ApproveExpert POST: Attempting to approve user with ID {userId}.");
            if (string.IsNullOrEmpty(userId)) { /*...*/ return RedirectToAction(nameof(ApproveExperts)); }
            ApplicationUser user = null;
            try
            {
                user = await _userManager.FindByIdAsync(userId);
                if (user == null) { /*...*/ return RedirectToAction(nameof(ApproveExperts)); }
                bool isExpertRole = await _userManager.IsInRoleAsync(user, Roles.Expert);
                if (!isExpertRole || user.IsApprovedExpert) { /*...*/ return RedirectToAction(nameof(ApproveExperts)); }
                user.IsApprovedExpert = true;
                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Esperto {user.FirstName} {user.LastName} ({user.Email}) approvato con successo!";
                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Il tuo Account Esperto Doceity è Attivo!", /*...*/ $"...");
                        _logger.LogInformation($"Approval notification email sent successfully to {user.Email}.");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send approval notification email to {user.Email}.");
                        TempData["WarningMessage"] = "Esperto approvato, ma errore invio email di notifica.";
                    }
                }
                else
                {
                    string errorDescriptions = string.Join("; ", updateResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    _logger.LogError($"AdminController.ApproveExpert POST: Failed to update user {user.Email} (ID: {userId}). Errors: {errorDescriptions}");
                    TempData["ErrorMessage"] = $"Errore durante l'approvazione di {user.Email}. Dettagli: {errorDescriptions}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AdminController.ApproveExpert POST: Generic error occurred for user ID {userId}.");
                TempData["ErrorMessage"] = "Si è verificato un errore imprevisto durante l'approvazione.";
            }
            return RedirectToAction(nameof(ApproveExperts));
        }


        // POST: Admin/RejectExpert/{userId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectExpert(string userId)
        {
            // ... (Codice RejectExpert POST come prima) ...
            _logger.LogInformation($"AdminController.RejectExpert POST: Attempting to reject user with ID {userId}.");
            if (string.IsNullOrEmpty(userId)) { /*...*/ return RedirectToAction(nameof(ApproveExperts)); }
            ApplicationUser user = null;
            try
            {
                user = await _userManager.FindByIdAsync(userId);
                if (user == null) { /*...*/ return RedirectToAction(nameof(ApproveExperts)); }
                bool isExpertRole = await _userManager.IsInRoleAsync(user, Roles.Expert);
                if (!isExpertRole || user.IsApprovedExpert) { /*...*/ return RedirectToAction(nameof(ApproveExperts)); }
                var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, Roles.Expert);
                if (!removeRoleResult.Succeeded)
                {
                    string errorDescriptions = string.Join("; ", removeRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    _logger.LogError($"AdminController.RejectExpert POST: Failed to remove Expert role from user {user.Email} (ID: {userId}). Errors: {errorDescriptions}");
                    TempData["ErrorMessage"] = $"Errore durante la rimozione del ruolo Esperto per {user.Email}. Dettagli: {errorDescriptions}";
                    return RedirectToAction(nameof(ApproveExperts));
                }
                bool isUserRole = await _userManager.IsInRoleAsync(user, Roles.User);
                if (!isUserRole) { /* ... prova ad aggiungere ruolo User, logga warning se fallisce ... */ }
                TempData["SuccessMessage"] = $"Richiesta esperto per {user.FirstName} {user.LastName} ({user.Email}) rifiutata. L'utente è stato impostato come Utente standard.";
                try
                {
                    await _emailSender.SendEmailAsync(user.Email, "Aggiornamento sulla tua Richiesta Esperto Doceity", /*...*/ $"...");
                    _logger.LogInformation($"Rejection notification email sent successfully to {user.Email}.");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Failed to send rejection notification email to {user.Email}.");
                    TempData["WarningMessage"] = "Esperto rifiutato, ma errore invio email di notifica.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AdminController.RejectExpert POST: Generic error occurred for user ID {userId}.");
                TempData["ErrorMessage"] = "Si è verificato un errore imprevisto durante il rifiuto.";
            }
            return RedirectToAction(nameof(ApproveExperts));
        }

        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard Admin";
            return View();
        }


        // --- INIZIO NUOVE AZIONI PER GESTIONE UTENTI ---

        // ViewModel interno per passare dati combinati alla vista UserManagement
        public class UserViewModel
        {
            public ApplicationUser User { get; set; }
            public IList<string> Roles { get; set; }
        }

        // GET: Admin/UserManagement
        // Mostra tutti gli utenti con filtri opzionali
        public async Task<IActionResult> UserManagement(string searchString, string roleFilter)
        {
            ViewData["Title"] = "Gestione Utenti";
            _logger.LogInformation("Admin accessing UserManagement page. Search: '{Search}', Role: '{Role}'", searchString, roleFilter);

            ViewData["CurrentFilter"] = searchString; // Per mantenere valore nella casella di ricerca
            ViewData["RoleFilter"] = roleFilter;     // Per mantenere valore nel dropdown ruolo

            var userQuery = _userManager.Users; // Inizia con tutti gli utenti

            // Applica filtro di ricerca (se presente)
            if (!String.IsNullOrEmpty(searchString))
            {
                userQuery = userQuery.Where(u => u.FirstName.Contains(searchString)
                                               || u.LastName.Contains(searchString)
                                               || u.Email.Contains(searchString));
            }

            // Applica filtro per ruolo (se presente)
            // Nota: Questo richiede di caricare tutti gli utenti che passano il filtro searchString
            // e poi filtrare in memoria in base al ruolo, il che può essere inefficiente
            // per un numero molto grande di utenti. Un approccio più ottimizzato richiederebbe
            // join con AspNetUserRoles nel database. Per ora, usiamo questo approccio più semplice.
            var filteredUsers = await userQuery.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in filteredUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                // Applica filtro ruolo DOPO aver ottenuto i ruoli
                if (string.IsNullOrEmpty(roleFilter) || userRoles.Contains(roleFilter))
                {
                    userViewModels.Add(new UserViewModel { User = user, Roles = userRoles });
                }
            }

            // Ottieni lista ruoli per il dropdown del filtro
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            _logger.LogInformation("Displaying {UserCount} users after filtering.", userViewModels.Count);
            return View(userViewModels); // Passa la lista di ViewModel alla vista
        }

        // POST: Admin/DeleteUser
        // Cancella un utente (con prevenzione auto-cancellazione)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            _logger.LogInformation("Admin attempting to delete user with ID: {UserId}", userId);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "ID Utente non valido.";
                _logger.LogWarning("DeleteUser POST received invalid userId (null or empty).");
                return RedirectToAction(nameof(UserManagement));
            }

            var currentAdminId = _userManager.GetUserId(User);
            if (userId.Equals(currentAdminId, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Non puoi cancellare il tuo stesso account amministratore.";
                _logger.LogWarning("Admin (ID: {AdminId}) attempted to delete their own account (ID: {UserId}).", currentAdminId, userId);
                return RedirectToAction(nameof(UserManagement));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utente da cancellare non trovato.";
                _logger.LogWarning("DeleteUser POST: User with ID {UserId} not found for deletion.", userId);
                return RedirectToAction(nameof(UserManagement));
            }

            try
            {
                _logger.LogWarning("Proceeding with deletion of user {Email} (ID: {UserId})...", user.Email, userId);

                // Qui potresti aggiungere logica per cancellare dati correlati NON gestiti da cascade delete
                // Esempio: Rimuovere prenotazioni future, riassegnare corsi, ecc.

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} (ID: {UserId}) deleted successfully by admin {AdminId}.", user.Email, userId, currentAdminId);
                    TempData["SuccessMessage"] = $"Utente {user.FirstName} {user.LastName} ({user.Email}) cancellato con successo.";
                }
                else
                {
                    string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Error deleting user {Email} (ID: {UserId}): {Errors}", user.Email, userId, errors);
                    TempData["ErrorMessage"] = $"Errore durante la cancellazione dell'utente {user.Email}: {errors}";
                }
            }
            catch (DbUpdateException dbEx) // Cattura specificamente errori DB (es. violazione FK se cascade non è impostato)
            {
                _logger.LogError(dbEx, "Database error occurred while deleting user {Email} (ID: {UserId}). Check foreign key constraints or cascade delete settings.", user.Email, userId);
                TempData["ErrorMessage"] = $"Errore database durante la cancellazione dell'utente {user.Email}. Potrebbero esserci dati collegati.";
            }
            catch (Exception ex) // Cattura altri errori imprevisti
            {
                _logger.LogError(ex, "Generic error occurred while deleting user {Email} (ID: {UserId}).", user.Email, userId);
                TempData["ErrorMessage"] = $"Errore imprevisto durante la cancellazione dell'utente {user.Email}.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // --- FINE NUOVE AZIONI PER GESTIONE UTENTI ---


        // GET: Admin/ContentManagement (Placeholder)
        public IActionResult ContentManagement()
        {
            ViewData["Title"] = "Gestione Contenuti";
            return View(); // Crea la vista Views/Admin/ContentManagement.cshtml
        }

    }
}