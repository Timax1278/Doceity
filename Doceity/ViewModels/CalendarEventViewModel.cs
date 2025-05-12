// File: ViewModels/CalendarEventViewModel.cs
namespace Doceity.ViewModels
{
    public class CalendarEventViewModel
    {
        public string Id { get; set; } // ID univoco dell'evento (es. "availability_1", "course_5")
        public string Title { get; set; }
        public string Start { get; set; } // Data/Ora inizio in formato ISO8601 (es. "2024-05-15T10:00:00")
        public string? End { get; set; }  // Data/Ora fine in formato ISO8601 (opzionale)
        public string? Color { get; set; } // Colore dell'evento (es. "green", "#FF5733")
        public string? Url { get; set; }   // URL a cui navigare se l'evento è cliccabile
        public bool AllDay { get; set; } = false; // Se l'evento dura tutto il giorno
        // Potresti aggiungere altre proprietà personalizzate che FullCalendar può usare
        // public string EventType { get; set; } // Es. "availability", "consultation", "course"
    }
}