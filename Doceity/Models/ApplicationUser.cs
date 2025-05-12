// File: Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic; // Necessario per ICollection

namespace Doceity.Models // Verifica questo namespace
{
    public class ApplicationUser : IdentityUser
    {
        // --- Proprietà Base Personalizzate ---
        [Required]
        [StringLength(50)]
        [Display(Name = "Nome")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Cognome")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Esperto Approvato")]
        public bool IsApprovedExpert { get; set; } = false;

        [Display(Name = "Data Registrazione")]
        [DataType(DataType.DateTime)]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow; // Valore di default


        // --- Proprietà di Navigazione ---

        // Corsi creati da questo utente (se è un esperto)
        public virtual ICollection<Course> CreatedCourses { get; set; } = new List<Course>();

        // Iscrizioni ai corsi fatte da questo utente
        public virtual ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();

        // --- NUOVA PROPRIETÀ DI NAVIGAZIONE PER LE SPECIALIZZAZIONI DELL'ESPERTO ---
        // Rappresenta la tabella di join ExpertSpecialization per la relazione Molti-a-Molti con Specialization
        public virtual ICollection<ExpertSpecialization> ExpertSpecializations { get; set; } = new List<ExpertSpecialization>();
        // --- FINE NUOVA PROPRIETÀ ---


        // Altre proprietà di navigazione che potresti aver già implementato o che aggiungerai:
        // (Assicurati che queste siano definite se le stai usando attivamente,
        // altrimenti lasciale commentate o rimuovile per evitare confusione con il DbContext)

        // Servizi offerti da questo utente (se è un esperto)
        // public virtual ICollection<ExpertService> OfferedServices { get; set; } = new List<ExpertService>();

        // Disponibilità definite da questo utente (se è un esperto)
        // public virtual ICollection<Availability> ExpertAvailabilities { get; set; } = new List<Availability>();

        // Richieste di consulenza inviate da questo utente
        // public virtual ICollection<ConsultationRequest> SentConsultationRequests { get; set; } = new List<ConsultationRequest>();

        // Richieste di consulenza ricevute da questo utente (se è un esperto)
        // public virtual ICollection<ConsultationRequest> ReceivedConsultationRequests { get; set; } = new List<ConsultationRequest>();
    }
}