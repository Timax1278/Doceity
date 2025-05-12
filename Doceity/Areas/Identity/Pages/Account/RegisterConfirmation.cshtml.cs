#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Doceity.Models; // Usa ApplicationUser
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Necessario anche se non usato direttamente qui
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Doceity.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        // Potresti iniettare IEmailSender qui se volessi aggiungere un pulsante "Reinvia Email"
        // private readonly IEmailSender _sender;

        public RegisterConfirmationModel(UserManager<ApplicationUser> userManager /*, IEmailSender sender*/)
        {
            _userManager = userManager;
            // _sender = sender;
        }

        // Email dell'utente appena registrato (passata come parametro route)
        public string Email { get; set; }

        // Flag per mostrare link di conferma diretto (non usato con vero email sender)
        public bool DisplayConfirmAccountLink { get; set; } = false;

        // URL di conferma (non usato con vero email sender)
        public string EmailConfirmationUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }
            ReturnUrl = returnUrl; // Potrebbe servire per futuri redirect

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Non rivelare che l'utente non esiste
                // Potresti loggare un warning qui
                return NotFound($"Impossibile trovare utente con email '{email}'.");
            }

            Email = email;

            // --- Logica per mostrare link diretto (COMMENTATA/DISABILITATA) ---
            // In un'app reale con invio email, non mostreresti il link qui.
            // Questa logica viene spesso usata nei template base quando l'invio email
            // non è ancora configurato, per permettere il test.
            /*
            DisplayConfirmAccountLink = true; // Imposta a false se usi email reali
            if (DisplayConfirmAccountLink)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);
            }
            */
            // --- Fine Logica Link Diretto ---

            return Page();
        }

        public string ReturnUrl { get; set; } // Proprietà aggiunta per returnUrl
    }
}