// File: ViewModels/CreateCourseViewModel.cs
using Doceity.Models; // Assumi che ExpertService sia qui
using Microsoft.AspNetCore.Mvc.Rendering; // Per SelectList
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Doceity.ViewModels // O il tuo namespace per i ViewModel
{
    public class CreateCourseViewModel
    {
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
        public DateTime StartDateTime { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "La durata è obbligatoria.")]
        [Range(30, 600, ErrorMessage = "La durata deve essere tra 30 e 600 minuti.")]
        [Display(Name = "Durata (minuti)")]
        public int DurationMinutes { get; set; } = 60;

        [Required(ErrorMessage = "Il prezzo è obbligatorio.")]
        [Range(0.00, 5000.00, ErrorMessage = "Il prezzo deve essere tra 0.00 e 5000.00.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Prezzo (€)")]
        public decimal Price { get; set; }

        [Range(1, 100, ErrorMessage = "Il numero massimo di partecipanti deve essere tra 1 e 100.")]
        [Display(Name = "Max Partecipanti (Opzionale)")]
        public int? MaxParticipants { get; set; }

        // --- NUOVA PROPRIETÀ AGGIUNTA QUI ---
        [StringLength(500, ErrorMessage = "Le informazioni per il meeting non possono superare 500 caratteri")]
        [DataType(DataType.Url)] // Suggerisce validazione URL lato client, ma accetta stringa normale
        [Display(Name = "Info Meeting Online (es. Link) (Opzionale)")]
        public string? VideoMeetingInfo { get; set; } // Aggiunta per corrispondere all'assegnazione nel controller
        // --- FINE NUOVA PROPRIETÀ ---

        [Display(Name = "Tipo di Servizio Base (Opzionale)")]
        public int? SelectedExpertServiceId { get; set; }

        public SelectList? AvailableServiceTypes { get; set; }
    }
}