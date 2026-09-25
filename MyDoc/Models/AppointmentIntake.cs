using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentIntake
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required]
        [StringLength(1000)]
        public string MainConcern { get; set; } = null!;

        [StringLength(1000)]
        public string? Symptoms { get; set; }

        [StringLength(500)]
        public string? StartedWhen { get; set; }

        [StringLength(1000)]
        public string? CurrentMedications { get; set; }

        [StringLength(1000)]
        public string? Allergies { get; set; }

        [StringLength(1000)]
        public string? PreviousRelevantExams { get; set; }

        [StringLength(1500)]
        public string? ExtraNotes { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public Appointment Appointment { get; set; } = null!;
    }
}
