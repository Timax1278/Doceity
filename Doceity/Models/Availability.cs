// File: Models/Availability.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doceity.Models // Assicurati che questo namespace sia corretto
{
    public class Availability
    {
        public int AvailabilityId { get; set; }

        [Required]
        public string ExpertUserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Data Disponibile")]
        public DateTime AvailableDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Ora Inizio")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Ora Fine")]
        public TimeSpan EndTime { get; set; }

        // --- NUOVA PROPRIETÀ AGGIUNTA ---
        [Display(Name = "Slot Prenotato")]
        public bool IsBooked { get; set; } = false; // Default a non prenotato
        // --- FINE NUOVA PROPRIETÀ ---

        [ForeignKey("ExpertUserId")]
        public virtual ApplicationUser Expert { get; set; }
    }
}