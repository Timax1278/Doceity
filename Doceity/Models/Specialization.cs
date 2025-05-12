// File: Models/Specialization.cs
using System.Collections.Generic; // Per ICollection, se la aggiungerai in futuro
using System.ComponentModel.DataAnnotations;

namespace Doceity.Models // Assicurati che il namespace sia corretto
{
    public class Specialization
    {
        public int SpecializationId { get; set; } // Primary Key

        [Required(ErrorMessage = "Il nome della specializzazione è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il nome non può superare i 100 caratteri.")]
        [Display(Name = "Nome Specializzazione")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "La descrizione non può superare i 500 caratteri.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Descrizione (Opzionale)")]
        public string? Description { get; set; }

        // Proprietà di navigazione per la relazione molti-a-molti con ApplicationUser (tramite ExpertSpecialization)
        // Questa lista conterrà tutte le "righe di join" che collegano questa specializzazione agli esperti.
        public virtual ICollection<ExpertSpecialization> ExpertSpecializations { get; set; } = new List<ExpertSpecialization>();
    }
}