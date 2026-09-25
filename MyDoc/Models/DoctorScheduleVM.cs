namespace MyDoc.Models
{
    public class DoctorScheduleVM
    {
        public List<DoctorAvailability> Availabilities { get; set; } = new();

        public List<DoctorUnavailablePeriod> UnavailablePeriods { get; set; } = new();

        public List<DoctorUnavailablePeriod> PastUnavailablePeriods { get; set; } = new();
    }
}
