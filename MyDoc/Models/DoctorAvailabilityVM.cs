using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class DoctorAvailabilityVM
    {
        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeOnly StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeOnly EndTime { get; set; }

        [Range(5, 180)]
        [Display(Name = "Slot Duration (Minutes)")]
        public int SlotDurationMinutes { get; set; } = 30;
    }
}
