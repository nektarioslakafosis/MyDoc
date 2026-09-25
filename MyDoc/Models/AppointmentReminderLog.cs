namespace MyDoc.Models
{
    public class AppointmentReminderLog
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public AppointmentReminderType ReminderType { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public Appointment Appointment { get; set; } = null!;
    }
}
