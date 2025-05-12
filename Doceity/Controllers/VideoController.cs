// File: Controllers/VideoController.cs
using Doceity.Configuration; // Per TwilioSettings
using Doceity.Models;         // Per ApplicationUser, Course, CourseEnrollment, ConsultationRequest
using Doceity.ViewModels;    // Per TokenRequestViewModel
using Doceity.Data;           // Per ApplicationDbContext
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;  // Per IOptions<TwilioSettings>
using Microsoft.EntityFrameworkCore; // Per Entity Framework Core
using System.Collections.Generic;    // Per HashSet
using Twilio.Jwt.AccessToken;        // Per Token e VideoGrant
using System.Threading.Tasks;        // Per Task
using System;                       // Per DateTime, Guid, ecc.

namespace Doceity.Controllers
{
    [Authorize] // Richiede che l'utente sia autenticato per tutte le azioni in questo controller
    public class VideoController : Controller
    {
        private readonly TwilioSettings _twilioSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; // Aggiunto DbContext per la logica di autorizzazione

        public VideoController(IOptions<TwilioSettings> twilioOptions,
                               UserManager<ApplicationUser> userManager,
                               ApplicationDbContext context) // Aggiunto DbContext
        {
            _twilioSettings = twilioOptions.Value;
            _userManager = userManager;
            _context = context; // Assegnato
        }

        // GET: /Video/Room?roomName=NOME_STANZA&userName=IDENTITA_UTENTE
        [HttpGet]
        public async Task<IActionResult> Room(string roomName, string userName)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Non dovrebbe succedere a causa di [Authorize]
                TempData["ErrorMessage"] = "Utente non autenticato.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(userName))
            {
                TempData["ErrorMessage"] = "Informazioni sulla stanza o sull'utente mancanti per avviare la sessione video.";
                // Reindirizza a una pagina sensata, es. la dashboard dell'utente o la home
                if (User.IsInRole(Constants.Roles.User)) return RedirectToAction("Index", "UserDashboard");
                if (User.IsInRole(Constants.Roles.Expert)) return RedirectToAction("Index", "ExpertAvailability"); // O una dashboard esperto
                return RedirectToAction("Index", "Home");
            }

            // Verifica se l'identità passata corrisponde all'utente loggato
            // Questo previene che un utente cerchi di entrare con l'identità di un altro passandola nell'URL
            if (userName != currentUser.UserName && userName != currentUser.Id && userName != $"{currentUser.FirstName} {currentUser.LastName}") // Considera diverse forme di identità
            {
                //  _logger.LogWarning("Tentativo di accesso alla stanza video con identità non corrispondente. Utente loggato: {AuthenticatedUser}, Identità richiesta: {RequestedIdentity}, Stanza: {RoomName}", currentUser.UserName, userName, roomName);
                TempData["ErrorMessage"] = "Identità utente non valida per questa sessione.";
                return Forbid(); // O reindirizza
            }

            // --- Logica di Autorizzazione per entrare nella stanza ---
            bool isAuthorizedForRoom = await CheckUserAuthorizationForRoom(currentUser.Id, roomName);
            if (!isAuthorizedForRoom)
            {
                TempData["ErrorMessage"] = "Non sei autorizzato ad accedere a questa stanza video o la sessione non è attiva.";
                // Reindirizza in base al tipo di stanza se possibile
                if (roomName.StartsWith("corso_")) return RedirectToAction("Details", "PublicCourses", new { id = GetIdFromRoomName(roomName, "corso_") });
                if (roomName.StartsWith("cons_")) return RedirectToAction("Details", "MyConsultations", new { id = GetIdFromRoomName(roomName, "cons_") }); // Assumendo che VideoRoomIdentifier sia "cons_IDRichiesta"
                return RedirectToAction("Index", "Home");
            }
            // --- Fine Logica di Autorizzazione ---

            ViewData["RoomName"] = roomName;
            ViewData["UserIdentity"] = userName; // Usa l'identità validata/passata
            return View(); // Si aspetta Views/Video/Room.cshtml
        }


        // POST: /Video/GenerateToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateToken([FromBody] TokenRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { message = "Utente non autenticato." });
            }

            // Validazione di sicurezza: L'identità nel token DEVE corrispondere all'utente autenticato
            // o essere un'identità che l'utente autenticato è autorizzato a usare.
            // Per semplicità, forziamo l'identità ad essere l'UserName dell'utente loggato.
            // O l'ID utente se preferisci.
            string tokenIdentity = currentUser.UserName; // O currentUser.Id;
            if (string.IsNullOrEmpty(tokenIdentity)) // Fallback se UserName è vuoto
            {
                tokenIdentity = currentUser.Id;
            }

            // Se vuoi permettere al client di specificare l'identità (model.Identity),
            // devi validare che model.Identity sia uguale a currentUser.UserName o currentUser.Id.
            // Per maggiore sicurezza, il server dovrebbe SEMPRE decidere/confermare l'identità.
            // if (model.Identity != tokenIdentity)
            // {
            //     _logger.LogWarning("Richiesta token con identità non corrispondente. Utente: {User}, Identità Richiesta: {ReqIdentity}, Stanza: {Room}", currentUser.UserName, model.Identity, model.RoomName);
            //     return BadRequest(new { message = "Identità non valida per la generazione del token." });
            // }


            // L'utente autenticato deve essere autorizzato ad entrare nella stanza 'model.RoomName'.
            bool isAuthorizedForRoom = await CheckUserAuthorizationForRoom(currentUser.Id, model.RoomName);
            if (!isAuthorizedForRoom)
            {
                // _logger.LogWarning("Tentativo non autorizzato di generare token per stanza. Utente: {User}, Stanza: {Room}", currentUser.UserName, model.RoomName);
                return Forbid("Non sei autorizzato a generare un token per questa stanza video.");
            }

            var grant = new VideoGrant { Room = model.RoomName };

            var token = new Token(
                _twilioSettings.AccountSid,
                _twilioSettings.ApiKeySid,
                _twilioSettings.ApiKeySecret,
                identity: tokenIdentity, // Usa l'identità verificata/impostata dal server
                grants: new HashSet<IGrant> { grant }
            );

            return Json(new
            {
                token = token.ToJwt(),
                room = model.RoomName,
                identity = tokenIdentity // Restituisci l'identità usata nel token
            });
        }

        // --- Logica di autorizzazione alla stanza ---
        private async Task<bool> CheckUserAuthorizationForRoom(string userId, string roomName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roomName))
            {
                return false;
            }

            // Stanza per un CORSO (es. roomName = "corso_123" o il valore di VideoMeetingInfo/VideoRoomIdentifier del corso)
            if (roomName.StartsWith("corso_"))
            {
                if (int.TryParse(roomName.Substring("corso_".Length), out int courseIdFromRoomName))
                {
                    var course = await _context.Courses
                                           .AsNoTracking() // Non serve tracciare per un controllo di autorizzazione
                                           .FirstOrDefaultAsync(c => c.CourseId == courseIdFromRoomName);
                    if (course == null || !course.IsJoinable) return false; // Corso non trovato o non più partecipabile

                    // L'utente è il creatore del corso?
                    if (course.CreatorExpertId == userId) return true;

                    // L'utente è iscritto attivamente al corso?
                    bool isEnrolled = await _context.CourseEnrollments
                        .AnyAsync(ce => ce.CourseId == courseIdFromRoomName &&
                                       ce.UserId == userId &&
                                       ce.Status == EnrollmentStatus.Enrolled);
                    return isEnrolled;
                }
            }
            // Stanza per una CONSULENZA (es. roomName = "cons_guid_univoco" o "consulenza_123")
            // Usiamo il campo VideoRoomIdentifier che abbiamo aggiunto a ConsultationRequest
            else if (roomName.StartsWith("cons_")) // Assumendo il prefisso che abbiamo usato
            {
                var consultation = await _context.ConsultationRequests
                                            .AsNoTracking()
                                            .Include(cr => cr.RequestedExpertService) // Per usare IsSessionJoinable
                                            .FirstOrDefaultAsync(cr => cr.VideoRoomIdentifier == roomName);

                if (consultation == null || consultation.Status != ConsultationRequestStatus.Accepted || !consultation.IsSessionJoinable)
                {
                    return false; // Consulenza non trovata, non accettata, o non più partecipabile
                }

                // L'utente è il richiedente o l'esperto della consulenza?
                return consultation.RequestingUserId == userId || consultation.ExpertUserId == userId;
            }
            // Aggiungi qui altre logiche per tipi di stanze diverse se necessario

            // _logger.LogWarning("Formato RoomName non riconosciuto o accesso negato. RoomName: {RoomName}, UserId: {UserId}", roomName, userId);
            return false; // Di default, nega l'accesso se il formato roomName non è gestito
        }

        // Helper per estrarre ID numerico da roomName (semplice, da rendere più robusto se necessario)
        private int? GetIdFromRoomName(string roomName, string prefix)
        {
            if (roomName.StartsWith(prefix) && int.TryParse(roomName.Substring(prefix.Length), out int id))
            {
                return id;
            }
            return null;
        }
    }
}