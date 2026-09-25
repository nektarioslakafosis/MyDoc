using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentFollowUpPlan
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required]
        [StringLength(1500)]
        public string Instructions { get; set; } = null!;

        [StringLength(1000)]
        public string? RecommendedTests { get; set; }

        [StringLength(1000)]
        public string? MedicationNotes { get; set; }

        [StringLength(1000)]
        public string? WarningSigns { get; set; }

        [StringLength(1000)]
        public string? FollowUpRecommendation { get; set; }

        public DateTime? SuggestedFollowUpDate { get; set; }

        [StringLength(1500)]
        public string? ExtraNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public Appointment Appointment { get; set; } = null!;
    }
}
