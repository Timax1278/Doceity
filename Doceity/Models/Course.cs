// File: Models/Course.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Necessario per ICollection

namespace Doceity.Models // Assicurati che il namespace sia corretto
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Il titolo è obbligatorio")]
        [StringLength(150, ErrorMessage = "Il titolo non può superare 150 caratteri")]
        [Display(Name = "Titolo Corso")]
        public string Title { get; set; }

        [Required(ErrorMessage = "La descrizione è obbligatoria")]
        [StringLength(2000, ErrorMessage = "La descrizione non può superare 2000 caratteri")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Descrizione")]
        public string Description { get; set; }

        [Required(ErrorMessage = "La data e ora di inizio sono obbligatorie")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Data e Ora Inizio (UTC)")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "La durata è obbligatoria")]
        [Range(15, 600, ErrorMessage = "La durata deve essere tra 15 e 600 minuti")]
        [Display(Name = "Durata (minuti)")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Il prezzo è obbligatorio")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.00, 10000.00, ErrorMessage = "Il prezzo deve essere tra 0.00 e 10000.00")]
        [DataType(DataType.Currency)]
        [Display(Name = "Prezzo (€)")]
        public decimal Price { get; set; }

        [Range(1, 200, ErrorMessage = "Il numero massimo di partecipanti deve essere compreso tra 1 e 200.")]
        [Display(Name = "Max Partecipanti")]
        public int? MaxParticipants { get; set; }

        [StringLength(500, ErrorMessage = "Le informazioni per il meeting non possono superare 500 caratteri")]
        [DataType(DataType.Url)]
        [Display(Name = "Info Meeting Online (es. Link o ID Stanza)")]
        public string? VideoMeetingInfo { get; set; } // Potrebbe contenere l'ID Stanza generato, es. "corso_123" o un GUID

        // Foreign Key per il creatore (Esperto)
        [Required]
        public string CreatorExpertId { get; set; }

        // Navigation Property per il creatore (Esperto)
        [ForeignKey("CreatorExpertId")]
        public virtual ApplicationUser CreatorExpert { get; set; }

        public virtual ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();

        // --- NUOVA PROPRIETÀ CALCOLATA [NotMapped] ---
        [NotMapped] // Questa proprietà non viene salvata nel database
        public bool IsJoinable
        {
            get
            {
                if (StartDateTime == default(DateTime)) return false;

                var now = DateTime.UtcNow;
                // L'utente può entrare X minuti prima dell'inizio
                const int minutesBeforeStartToJoin = 10;
                // La stanza rimane accessibile fino a Y minuti dopo la fine calcolata del corso
                const int minutesAfterEndToJoin = 30;

                DateTime joinWindowStart = StartDateTime.AddMinutes(-minutesBeforeStartToJoin);
                DateTime courseTheoreticalEnd = StartDateTime.AddMinutes(DurationMinutes);
                DateTime joinWindowEnd = courseTheoreticalEnd.AddMinutes(minutesAfterEndToJoin);

                return now >= joinWindowStart && now <= joinWindowEnd;
            }
        }
        // --- FINE NUOVA PROPRIETÀ ---
    }
}