// File: ViewModels/TokenRequestViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Doceity.ViewModels // O il tuo namespace per i ViewModel
{
    public class TokenRequestViewModel
    {
        [Required(ErrorMessage = "L'identità dell'utente è richiesta.")]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "L'identità deve avere tra 1 e 128 caratteri.")]
        // L'identità è il nome utente che apparirà nella stanza Twilio.
        // Potrebbe essere l'ID utente dell'applicazione, il nome utente, o un GUID.
        // Deve essere univoca per utente nella stanza, ma non globalmente univoca in Twilio.
        public string Identity { get; set; }

        [Required(ErrorMessage = "Il nome della stanza è richiesto.")]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "Il nome della stanza deve avere tra 1 e 128 caratteri.")]
        // Il nome della stanza a cui l'utente vuole connettersi.
        public string RoomName { get; set; }
    }
}