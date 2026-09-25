namespace MyDoc.Models
{
    public class PatientDashboardVM
    {
        public List<PatientAppointmentItemVM> UpcomingAppointments { get; set; } = new();

        public List<PatientAppointmentItemVM> HistoryAppointments { get; set; } = new();

        public PatientAppointmentItemVM? NextAppointment
        {
            get
            {
                return UpcomingAppointments.FirstOrDefault();
            }
        }

        public bool HasActionNeeded
        {
            get
            {
                return UpcomingAppointments.Any(x => x.NeedsPreAppointmentInfo);
            }
        }
    }

    public class PatientAppointmentItemVM
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public AppointmentStatus Status { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public string Specialty { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool HasPreAppointmentInfo { get; set; }

        public bool NeedsPreAppointmentInfo { get; set; }

        public bool HasPendingRescheduleRequest { get; set; }

        public bool HasFollowUpPlan { get; set; }

        public bool CanReschedule { get; set; }

        public bool CanCancel { get; set; }
    }
}
