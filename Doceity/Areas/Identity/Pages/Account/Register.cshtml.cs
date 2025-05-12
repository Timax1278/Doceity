#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Doceity.Models; // <-- Usa ApplicationUser
using Doceity.Constants; // <-- Usa Roles
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Doceity.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager; // <-- Necessario per assegnare ruoli

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager) // <-- Aggiunto RoleManager
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager; // <-- Inizializzato RoleManager
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // --- InputModel AGGIORNATO ---
        public class InputModel
        {
            [Required]
            [StringLength(50)]
            [Display(Name = "Nome")]
            public string FirstName { get; set; }

            [Required]
            [StringLength(50)]
            [Display(Name = "Cognome")]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "La {0} deve essere lunga almeno {2} e massimo {1} caratteri.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Conferma password")]
            [Compare("Password", ErrorMessage = "La password e la conferma password non corrispondono.")]
            public string ConfirmPassword { get; set; }

            // --- NUOVO CAMPO PER SCELTA RUOLO ---
            [Required(ErrorMessage = "Devi selezionare un tipo di account.")]
            [Display(Name = "Tipo di Account")]
            public string AccountType { get; set; } // Valori: "User", "Expert"
            // ------------------------------------
        }
        // --- FINE InputModel ---


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        // --- OnPostAsync AGGIORNATO ---
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} created a new account with password.");

                    // --- ASSEGNAZIONE RUOLO E FLAG APPROVAZIONE ---
                    string selectedRole;
                    bool requiresApproval = false;

                    if (Input.AccountType == Roles.Expert) // Usa la costante
                    {
                        selectedRole = Roles.Expert;
                        user.IsApprovedExpert = false; // Non approvato di default
                        requiresApproval = true;
                        _logger.LogInformation($"User {user.Email} registered as Expert. Approval needed.");
                        // Aggiorna il flag nel DB *dopo* l'assegnazione del ruolo (o qui se necessario)
                        // await _userManager.UpdateAsync(user); // Lo facciamo dopo o non serve se CreateAsync lo fa già? Testare.
                    }
                    else // Se non è Expert, è User
                    {
                        selectedRole = Roles.User;
                        user.IsApprovedExpert = false; // Utente normale non è "esperto approvato"
                        _logger.LogInformation($"User {user.Email} registered as User.");
                    }

                    // Assegna il ruolo selezionato
                    if (!await _roleManager.RoleExistsAsync(selectedRole))
                    {
                        _logger.LogError($"Role '{selectedRole}' does not exist. Seeding might have failed.");
                        ModelState.AddModelError(string.Empty, $"Errore interno: Ruolo '{selectedRole}' non trovato. Contattare l'amministratore.");
                        // Potremmo cancellare l'utente creato? O lasciare che l'admin risolva? Per ora mostriamo errore.
                        // await _userManager.DeleteAsync(user);
                        return Page();
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, selectedRole);
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError($"Failed to add user {user.Email} to role '{selectedRole}'. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        ModelState.AddModelError(string.Empty, $"Errore nell'assegnazione del ruolo '{selectedRole}'. Contattare l'amministratore.");
                        // Potremmo cancellare l'utente?
                        // await _userManager.DeleteAsync(user);
                        return Page();
                    }
                    _logger.LogInformation($"Successfully added user {user.Email} to role '{selectedRole}'.");

                    // Se l'utente è un esperto, assicuriamoci che il flag IsApprovedExpert=false sia salvato.
                    // CreateAsync potrebbe non salvare modifiche fatte all'oggetto 'user' *dopo* la sua istanziazione
                    // e prima di CreateAsync, quindi un Update esplicito è più sicuro.
                    if (requiresApproval)
                    {
                        var updateResult = await _userManager.UpdateAsync(user);
                        if (!updateResult.Succeeded)
                        {
                            _logger.LogError($"Failed to update IsApprovedExpert flag for user {user.Email}. Errors: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                            ModelState.AddModelError(string.Empty, "Errore durante l'impostazione dello stato di approvazione.");
                            return Page();
                        }
                        _logger.LogInformation($"IsApprovedExpert flag set to false for user {user.Email}.");
                    }
                    // --- FINE ASSEGNAZIONE RUOLO E FLAG ---


                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Conferma la tua email per Doceity",
                        $"Ciao {user.FirstName},<br><br>Per favore conferma il tuo account Doceity <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>cliccando qui</a>.<br><br>Grazie,<br>Il Team Doceity");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        // L'esperto non approvato può comunque confermare l'email
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else // Login automatico solo per utenti normali, non per esperti in attesa
                    {
                        if (!requiresApproval)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            _logger.LogInformation($"User {user.Email} signed in automatically after registration.");
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            _logger.LogInformation($"User {user.Email} registered as expert and requires approval. Not signing in automatically.");
                            TempData["StatusMessage"] = "Registrazione come esperto completata! Il tuo account è ora in attesa di revisione e approvazione da parte di un amministratore. Riceverai una notifica via email non appena sarà attivo.";
                            return RedirectToPage("./Login"); // Manda alla pagina di login con messaggio
                        }
                    }
                }
                // Se CreateAsync fallisce
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Se ModelState non è valido all'inizio, o se CreateAsync fallisce
            return Page();
        }
        // --- FINE OnPostAsync ---

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}