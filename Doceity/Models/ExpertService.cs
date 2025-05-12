// File: Models/ExpertService.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Doceity.Models
{
    public class ExpertService
    {
        public int ExpertServiceId { get; set; } // Primary Key

        [Required]
        public string ExpertUserId { get; set; } // Foreign Key to ApplicationUser

        [Required]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters.")]
        [Display(Name = "Service Title")]
        public string Title { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters.")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Estimated Duration (Minutes)")]
        [Range(15, 480, ErrorMessage = "Duration must be between 15 and 480 minutes.")] // Example range: 15 mins to 8 hours
        public int EstimatedDurationMinutes { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be between 0.01 and 10000.00.")] // Example price range
        public decimal Price { get; set; }

        [Display(Name = "Is Service Active?")]
        public bool IsEnabled { get; set; } = true; // Default to active

        // Navigation Property
        [ForeignKey("ExpertUserId")]
        public virtual ApplicationUser Expert { get; set; }

        // Optional: Collection of requests related to this service type
        // public virtual ICollection<ConsultationRequest> ConsultationRequests { get; set; } = new List<ConsultationRequest>();
    }
}