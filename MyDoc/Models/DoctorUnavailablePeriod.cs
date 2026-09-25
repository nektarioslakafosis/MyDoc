using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class DoctorUnavailablePeriod
    {
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        [StringLength(250)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Doctor Doctor { get; set; } = null!;
    }
}
