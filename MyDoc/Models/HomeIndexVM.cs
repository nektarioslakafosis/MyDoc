namespace MyDoc.Models
{
    public class HomeIndexVM
    {
        public string? Search { get; set; }

        public string? Specialty { get; set; }

        public List<string> Specialties { get; set; } = new();

        public List<HomeDoctorPreviewVM> Doctors { get; set; } = new();

        // Patient smart area
        public Appointment? NextPatientAppointment { get; set; }

        public int PatientPendingAppointments { get; set; }

        public int PatientApprovedAppointments { get; set; }

        public int PatientMissingIntakeCount { get; set; }

        // Doctor smart area
        public Appointment? DoctorNextAppointment { get; set; }

        public int DoctorTodayAppointments { get; set; }

        public int DoctorPendingAppointments { get; set; }

        public int DoctorPendingRescheduleRequests { get; set; }

        // Admin smart area
        public int AdminActiveDoctors { get; set; }

        public int AdminActivePatients { get; set; }

        public int AdminTodayAppointments { get; set; }

        public int AdminPendingAppointments { get; set; }
    }

    public class HomeDoctorPreviewVM
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Specialty { get; set; } = string.Empty;

        public DateTime? NextAvailableSlot { get; set; }
    }
}
