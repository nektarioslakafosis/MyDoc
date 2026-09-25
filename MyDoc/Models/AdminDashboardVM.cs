namespace MyDoc.Models
{
    public class AdminDashboardVM
    {
        public int TotalDoctors { get; set; }
        public int ActiveDoctors { get; set; }
        public int InactiveDoctors { get; set; }

        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int InactivePatients { get; set; }

        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }

        public int PendingAppointments { get; set; }
        public int ApprovedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int ExpiredAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int NoShowAppointments { get; set; }

        public List<Appointment> UpcomingAppointments { get; set; } = new();
    }
}
