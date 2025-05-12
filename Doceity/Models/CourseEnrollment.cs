// File: Models/CourseEnrollment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doceity.Models // Assicurati che il namespace sia corretto
{
    public enum EnrollmentStatus
    {
        Enrolled,   // Iscritto e confermato
        PendingPayment, // Iscrizione in attesa di pagamento (se implementi pagamenti)
        CancelledByUser, // Iscrizione cancellata dall'utente
        CancelledByAdminOrExpert // Iscrizione cancellata dall'admin o dall'esperto
    }

    public class CourseEnrollment
    {
        public int CourseEnrollmentId { get; set; } // Primary Key

        [Required]
        public int CourseId { get; set; } // Foreign Key to Course

        [Required]
        public string UserId { get; set; } // Foreign Key to ApplicationUser (l'utente iscritto)

        [Required]
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled; // Default a iscritto

        // Potresti aggiungere un campo per il prezzo pagato al momento dell'iscrizione,
        // se il prezzo del corso potesse cambiare nel tempo.
        // [Column(TypeName = "decimal(18, 2)")]
        // public decimal PricePaid { get; set; }

        // Navigation Properties
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}