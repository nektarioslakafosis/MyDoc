using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentRescheduleRequest
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public DateTime PreviousAppointmentDate { get; set; }

        public DateTime RequestedAppointmentDate { get; set; }

        [Required]
        [StringLength(600)]
        public string Reason { get; set; } = null!;

        public AppointmentRescheduleRequestStatus Status { get; set; } = AppointmentRescheduleRequestStatus.Pending;

        [Required]
        [StringLength(30)]
        public string RequestedByRole { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedByUserId { get; set; }

        [StringLength(600)]
        public string? ReviewComment { get; set; }

        public Appointment Appointment { get; set; } = null!;
    }
}
