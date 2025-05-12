// File: Data/ApplicationDbContext.cs
using Doceity.Models; // Assicurati che questo sia il namespace corretto per TUTTI i tuoi modelli
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doceity.Data // Assicurati che il namespace del context sia corretto
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> // Conferma che usi ApplicationUser
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // --- DbSet DEFINITI ---
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<ExpertService> ExpertServices { get; set; } = null!;
        public DbSet<ConsultationRequest> ConsultationRequests { get; set; } = null!;
        public DbSet<Availability> Availabilities { get; set; } = null!;
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; } = null!;
        public DbSet<Specialization> Specializations { get; set; } = null!;           // <-- NUOVO DbSet
        public DbSet<ExpertSpecialization> ExpertSpecializations { get; set; } = null!; // <-- NUOVO DbSet

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Configura Identity PRIMA DI TUTTO

            // --- Configurazione Relazione ApplicationUser <-> Course ---
            builder.Entity<Course>(entity =>
            {
                entity.HasOne(c => c.CreatorExpert)
                      .WithMany(u => u.CreatedCourses)
                      .HasForeignKey(c => c.CreatorExpertId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(c => c.CreatorExpertId);
                entity.HasIndex(c => c.StartDateTime);
                entity.Property(c => c.Price).HasColumnType("decimal(18, 2)");
            });

            // --- Configurazione ApplicationUser (Expert) <-> ExpertService ---
            builder.Entity<ExpertService>(entity =>
            {
                entity.HasOne(es => es.Expert)
                      .WithMany(/*u => u.OfferedServices*/) // Se ApplicationUser ha ICollection<ExpertService> OfferedServices
                      .HasForeignKey(es => es.ExpertUserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(es => es.Price).HasColumnType("decimal(18, 2)");
                entity.HasIndex(es => es.ExpertUserId);
            });

            // --- Configurazione ConsultationRequest e le sue relazioni ---
            builder.Entity<ConsultationRequest>(entity =>
            {
                entity.HasOne(cr => cr.RequestingUser)
                      .WithMany(/*u => u.SentConsultationRequests*/) // Se ApplicationUser ha ICollection<ConsultationRequest> SentConsultationRequests
                      .HasForeignKey(cr => cr.RequestingUserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cr => cr.Expert)
                      .WithMany(/*u => u.ReceivedConsultationRequests*/) // Se ApplicationUser ha ICollection<ConsultationRequest> ReceivedConsultationRequests
                      .HasForeignKey(cr => cr.ExpertUserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cr => cr.RequestedExpertService)
                      .WithMany(/*es => es.ConsultationRequests*/) // Se ExpertService ha ICollection<ConsultationRequest> ConsultationRequests
                      .HasForeignKey(cr => cr.RequestedExpertServiceId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(cr => cr.OriginalAvailabilitySlot)
                      .WithMany() // Se Availability non ha una collection di ConsultationRequests
                      .HasForeignKey(cr => cr.OriginalAvailabilityId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict); // O NoAction, come discusso

                entity.HasIndex(cr => cr.RequestingUserId);
                entity.HasIndex(cr => cr.ExpertUserId);
                entity.HasIndex(cr => cr.RequestedExpertServiceId);
                entity.HasIndex(cr => cr.OriginalAvailabilityId);
            });

            // --- Configurazione per AVAILABILITY ---
            builder.Entity<Availability>(entity =>
            {
                entity.HasOne(a => a.Expert)
                      .WithMany(/*u => u.ExpertAvailabilities*/) // Se ApplicationUser ha ICollection<Availability> ExpertAvailabilities
                      .HasForeignKey(a => a.ExpertUserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(a => new { a.ExpertUserId, a.AvailableDate });
            });

            // --- Configurazione per COURSEENROLLMENT ---
            builder.Entity<CourseEnrollment>(entity =>
            {
                entity.HasOne(ce => ce.Course)
                      .WithMany(c => c.Enrollments) // Assumendo che Course abbia ICollection<CourseEnrollment> Enrollments
                      .HasForeignKey(ce => ce.CourseId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ce => ce.User)
                      .WithMany(u => u.CourseEnrollments) // Assumendo che ApplicationUser abbia ICollection<CourseEnrollment> CourseEnrollments
                      .HasForeignKey(ce => ce.UserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(ce => new { ce.CourseId, ce.UserId }).IsUnique();
            });

            // --- NUOVA CONFIGURAZIONE PER SPECIALIZATION E EXPERTSPECIALIZATION ---
            builder.Entity<Specialization>(entity =>
            {
                // Indice sul nome della specializzazione per ricerche veloci e per unicità (se applicata)
                entity.HasIndex(s => s.Name).IsUnique();
            });

            builder.Entity<ExpertSpecialization>(entity =>
            {
                // 1. Definisci la Chiave Primaria Composta
                entity.HasKey(es => new { es.ApplicationUserId, es.SpecializationId });

                // 2. Configura la relazione con ApplicationUser (Expert)
                entity.HasOne(es => es.User) // Proprietà di navigazione 'User' in ExpertSpecialization
                      .WithMany(u => u.ExpertSpecializations) // Proprietà 'ExpertSpecializations' in ApplicationUser
                      .HasForeignKey(es => es.ApplicationUserId)
                      .OnDelete(DeleteBehavior.Cascade); // Se un utente/esperto viene eliminato, rimuovi i suoi link alle specializzazioni

                // 3. Configura la relazione con Specialization
                entity.HasOne(es => es.Specialization) // Proprietà di navigazione 'Specialization' in ExpertSpecialization
                      .WithMany(s => s.ExpertSpecializations) // Proprietà 'ExpertSpecializations' in Specialization
                      .HasForeignKey(es => es.SpecializationId)
                      .OnDelete(DeleteBehavior.Cascade); // Se una specializzazione viene eliminata, rimuovi i link degli esperti ad essa
            });
            // --- FINE NUOVA CONFIGURAZIONE ---
        }
    }
}