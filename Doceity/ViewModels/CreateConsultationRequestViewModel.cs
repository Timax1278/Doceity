// File: ViewModels/CreateConsultationRequestViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Per [NotMapped]
using Microsoft.AspNetCore.Mvc.Rendering;         // Per SelectList (anche se non usato attivamente ora, è buona norma averlo se potrebbe servire)
using System.Collections.Generic;                 // Per List<>
using Doceity.Models;                           // Per usare il modello Availability

namespace Doceity.ViewModels // Assicurati che il namespace sia corretto
{
    public class CreateConsultationRequestViewModel
    {
        // --- Dati che verranno inviati dal form o sono necessari per il processo ---
        [Required]
        public string ExpertUserId { get; set; } // Popolato dal controller, passato come hidden

        public int? RequestedExpertServiceId { get; set; } // Popolato dal controller, passato come hidden

        // Questa proprietà riceverà il valore selezionato dai radio button (formato "ID|ISODate")
        // o il valore dall'input datetime-local di fallback (formato stringa datetime).
        [Required(ErrorMessage = "Devi selezionare uno slot disponibile o proporre una data e un'ora.")]
        [Display(Name = "Slot Selezionato o Data/Ora Proposta")]
        public string SelectedSlot { get; set; } = string.Empty; // <-- INIZIALIZZATO A STRINGA VUOTA

        [StringLength(1500, ErrorMessage = "Il messaggio non può superare i 1500 caratteri.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Messaggio per l'Esperto (Opzionale)")]
        public string? UserMessage { get; set; }


        // --- Proprietà calcolate/usate nel controller dopo il parsing di SelectedSlot ---
        // Non vengono direttamente mappate a input del form con asp-for, ma usate internamente.
        [NotMapped] // Indica a EF Core (se mai si tentasse di usare questo VM come entità) di non mappare queste proprietà.
        public DateTime ParsedProposedDateTime { get; set; }

        [NotMapped]
        public int? ParsedAvailabilityId { get; set; }


        // --- Proprietà solo per la visualizzazione nel form (popolate dal controller GET) ---
        [Display(Name = "Esperto Selezionato")]
        public string ExpertFullName { get; set; } // Mostrato nella UI, passato come hidden per il POST

        [Display(Name = "Servizio Selezionato")]
        public string? ExpertServiceTitle { get; set; } // Mostrato nella UI, passato come hidden per il POST


        // --- Lista delle disponibilità dell'esperto da mostrare nella UI ---
        public List<Availability>? ExpertAvailabilities { get; set; }


        // Opzionale: Se si volesse un dropdown per selezionare un servizio specifico di quell'esperto nel form
        // public SelectList? AvailableServicesForThisExpert { get; set; }
    }
}