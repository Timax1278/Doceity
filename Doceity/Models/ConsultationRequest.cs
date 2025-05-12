// File: Models/ConsultationRequest.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Per [NotMapped]

namespace Doceity.Models
{
    public enum ConsultationRequestStatus
    {
        [Display(Name = "Pending Approval")]
        Pending,
        Accepted,
        Rejected,
        [Display(Name = "Cancelled by User")]
        Cancelled
    }

    public class ConsultationRequest
    {
        public int ConsultationRequestId { get; set; }

        [Required]
        public string RequestingUserId { get; set; }

        [Required]
        public string ExpertUserId { get; set; }

        [Display(Name = "Requested Service (Optional)")]
        public int? RequestedExpertServiceId { get; set; }

        [Required]
        [Display(Name = "Proposed Date and Time (UTC)")]
        [DataType(DataType.DateTime)]
        public DateTime ProposedDateTime { get; set; }

        [StringLength(1500)]
        [Display(Name = "Your Message (Optional)")]
        [DataType(DataType.MultilineText)]
        public string? UserMessage { get; set; }

        [Required]
        [Display(Name = "Request Date")]
        public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public ConsultationRequestStatus Status { get; set; } = ConsultationRequestStatus.Pending;

        [Display(Name = "Response Date")]
        public DateTime? ResponseTimestamp { get; set; }

        [StringLength(500)]
        [Display(Name = "Expert's Response / Reason")]
        [DataType(DataType.MultilineText)]
        public string? ExpertResponseMessage { get; set; }

        public int? OriginalAvailabilityId { get; set; }

        [ForeignKey("OriginalAvailabilityId")]
        public virtual Availability? OriginalAvailabilitySlot { get; set; }

        // --- NUOVA PROPRIETÀ PER LA STANZA VIDEO ---
        [StringLength(256)] // Lunghezza sufficiente per un GUID o un nome descrittivo
        [Display(Name = "Video Room Identifier")]
        public string? VideoRoomIdentifier { get; set; }
        // --- FINE NUOVA PROPRIETÀ ---

        // --- NUOVA PROPRIETÀ CALCOLATA [NotMapped] ---
        [NotMapped]
        public bool IsSessionJoinable
        {
            get
            {
                // La sessione è partecipabile solo se è stata Accettata
                if (Status != ConsultationRequestStatus.Accepted || ProposedDateTime == default(DateTime))
                {
                    return false;
                }

                var now = DateTime.UtcNow;
                const int minutesBeforeStartToJoin = 10; // Può entrare 10 min prima
                const int minutesAfterEndToJoin = 30;    // La stanza rimane accessibile per 30 min dopo la fine teorica

                // Determina la durata della consulenza
                int durationMinutes = 60; // Durata di default in minuti
                if (RequestedExpertService != null && RequestedExpertService.EstimatedDurationMinutes > 0)
                {
                    durationMinutes = RequestedExpertService.EstimatedDurationMinutes;
                }
                // Potresti anche decidere di salvare una AgreedDurationMinutes in ConsultationRequest
                // se l'esperto può modificarla quando accetta.

                DateTime sessionStartTimeWindow = ProposedDateTime.AddMinutes(-minutesBeforeStartToJoin);
                DateTime sessionTheoreticalEnd = ProposedDateTime.AddMinutes(durationMinutes);
                DateTime sessionEndTimeWindow = sessionTheoreticalEnd.AddMinutes(minutesAfterEndToJoin);

                return now >= sessionStartTimeWindow && now <= sessionEndTimeWindow;
            }
        }
        // --- FINE NUOVA PROPRIETÀ CALCOLATA ---

        // Navigation Properties
        [ForeignKey("RequestingUserId")]
        public virtual ApplicationUser RequestingUser { get; set; }

        [ForeignKey("ExpertUserId")]
        public virtual ApplicationUser Expert { get; set; }

        [ForeignKey("RequestedExpertServiceId")]
        public virtual ExpertService RequestedExpertService { get; set; }
    }
}