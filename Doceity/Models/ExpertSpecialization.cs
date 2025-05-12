// File: Models/ExpertSpecialization.cs
namespace Doceity.Models // Assicurati che il namespace sia corretto
{
    public class ExpertSpecialization
    {
        // Chiave Primaria Composta (configurata tramite Fluent API nel DbContext)
        public string ApplicationUserId { get; set; } // Parte della PK, Foreign Key a ApplicationUser
        public int SpecializationId { get; set; }   // Parte della PK, Foreign Key a Specialization

        // Proprietà di navigazione verso le entità collegate
        public virtual ApplicationUser User { get; set; }
        public virtual Specialization Specialization { get; set; }
    }
}