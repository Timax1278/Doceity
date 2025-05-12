// File: ViewModels/EditCourseViewModel.cs
 using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

namespace Doceity.ViewModels
{
    public class EditCourseViewModel
    {
        [Required]
        public int CourseId { get; set; } // Fondamentale per l'aggiornamento

        [Required(ErrorMessage = "Il titolo del corso è obbligatorio.")]
        [StringLength(150, ErrorMessage = "Il titolo non può superare i 150 caratteri.")]
        [Display(Name = "Titolo del Corso")]
        public string Title { get; set; }

        [Required(ErrorMessage = "La descrizione è obbligatoria.")]
        [StringLength(2000, ErrorMessage = "La descrizione non può superare i 2000 caratteri.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Descrizione Dettagliata")]
        public string Description { get; set; }

        [Required(ErrorMessage = "La data e l'ora di inizio sono obbligatorie.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Data e Ora Inizio")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "La durata è obbligatoria.")]
        [Range(30, 600, ErrorMessage = "La durata deve essere tra 30 e 600 minuti.")]
        [Display(Name = "Durata (minuti)")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Il prezzo è obbligatorio.")]
        [Range(0.00, 5000.00, ErrorMessage = "Il prezzo deve essere tra 0.00 e 5000.00.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Prezzo (€)")]
        public decimal Price { get; set; }

        [Range(1, 100, ErrorMessage = "Il numero massimo di partecipanti deve essere tra 1 e 100.")]
        [Display(Name = "Max Partecipanti (Opzionale)")]
        public int? MaxParticipants { get; set; }

        [StringLength(500, ErrorMessage = "Le informazioni per il meeting non possono superare 500 caratteri")]
        [DataType(DataType.Url)]
        [Display(Name = "Info Meeting Online (es. Link) (Opzionale)")]
        public string? VideoMeetingInfo { get; set; }

        // Manteniamo CreatorExpertId per coerenza, anche se non è modificabile dall'utente nel form
        // Verrà usato per verifiche di sicurezza nel POST
        public string CreatorExpertId { get; set; }


        // --- Collegamento opzionale a ExpertService ---
        [Display(Name = "Tipo di Servizio Base (Opzionale)")]
        public int? SelectedExpertServiceId { get; set; }

        public SelectList? AvailableServiceTypes { get; set; } // Se usi il dropdown
    }
}
