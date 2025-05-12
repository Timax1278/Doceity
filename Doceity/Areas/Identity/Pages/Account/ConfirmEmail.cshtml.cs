#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Doceity.Models; // Usa ApplicationUser
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging; // Per Logging

namespace Doceity.Areas.Identity.Pages.Account
{
    [AllowAnonymous] // Chiunque può accedere a questa pagina tramite il link
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger; // Per Logging

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData] // Per mostrare messaggi dopo il redirect o sulla stessa pagina
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                _logger.LogWarning("ConfirmEmail GET request received with missing userId or code.");
                // Reindirizza alla home page se mancano parametri
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmail GET: User not found for ID '{UserId}'.", userId);
                // Non rivelare che l'utente non esiste, mostra messaggio generico
                StatusMessage = "Errore nella conferma dell'email: utente non trovato.";
                // Potresti mostrare un messaggio più generico o una pagina Not Found standard
                // return NotFound($"Impossibile caricare utente con ID '{userId}'.");
                return Page();
            }

            _logger.LogInformation("ConfirmEmail GET: Attempting to confirm email for user {UserId} ({Email}).", userId, user.Email);

            try
            {
                // Decodifica il codice dal formato URL-safe Base64
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                _logger.LogInformation("Confirmation code decoded successfully for user {UserId}.", userId);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "ConfirmEmail GET: Error decoding confirmation code for user {UserId}. Invalid Base64 string.", userId);
                StatusMessage = "Errore nella conferma dell'email: il codice di conferma non è valido.";
                return Page();
            }

            // Esegui la conferma effettiva usando UserManager
            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for user {UserId} ({Email}).", userId, user.Email);
                StatusMessage = "Grazie per aver confermato la tua email. Ora puoi provare ad accedere.";
            }
            else
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Error confirming email for user {UserId} ({Email}). Errors: {Errors}", userId, user.Email, errors);
                StatusMessage = $"Errore nella conferma dell'email: {errors}";
            }

            // Mostra la pagina ConfirmEmail.cshtml con il StatusMessage impostato
            return Page();
        }
    }
}