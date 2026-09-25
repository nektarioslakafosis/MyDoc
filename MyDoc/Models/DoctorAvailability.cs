using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class DoctorAvailability
    {
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        [Range(5, 180)]
        public int SlotDurationMinutes { get; set; } = 30;

        public bool IsActive { get; set; } = true;

        public Doctor Doctor { get; set; } = null!;
    }
}
