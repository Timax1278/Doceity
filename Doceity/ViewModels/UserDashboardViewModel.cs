// File: ViewModels/UserDashboardViewModel.cs
using Doceity.Models; // Per ConsultationRequest e CourseEnrollment
using System.Collections.Generic;

namespace Doceity.ViewModels
{
    public class UserDashboardViewModel
    {
        // Consulenze Accettate e Future
        public List<ConsultationRequest> UpcomingAcceptedConsultations { get; set; } = new List<ConsultationRequest>();

        // Corsi Iscritti e Futuri
        public List<CourseEnrollment> UpcomingEnrolledCourses { get; set; } = new List<CourseEnrollment>();

        // Ultime Richieste di Consulenza Inviate (es. le ultime 5 o quelle Pending)
        public List<ConsultationRequest> RecentConsultationRequests { get; set; } = new List<ConsultationRequest>();

        // Potresti aggiungere altre sezioni, come:
        // public List<Notification> UnreadNotifications { get; set; } // Se avessi un sistema di notifiche interno
        // public int PendingRequestsCount { get; set; }
        // public int UpcomingCoursesCount { get; set; }

        public UserDashboardViewModel()
        {
            UpcomingAcceptedConsultations = new List<ConsultationRequest>();
            UpcomingEnrolledCourses = new List<CourseEnrollment>();
            RecentConsultationRequests = new List<ConsultationRequest>();
        }
    }
}