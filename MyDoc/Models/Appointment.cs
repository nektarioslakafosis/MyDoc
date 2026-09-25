using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string Description { get; set; } = null!;

        [BindNever]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(600)]
        public string? CancellationReason { get; set; }

        public DateTime? CancelledAt { get; set; }

        [StringLength(30)]
        public string? CancelledByRole { get; set; }

        public DateTime? ClosedAt { get; set; }

        [StringLength(30)]
        public string? ClosedByRole { get; set; }

        [StringLength(1000)]
        public string? DoctorOutcomeNote { get; set; }

        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        public int? PatientId { get; set; }

        [BindNever]
        public Doctor Doctor { get; set; } = null!;

        [BindNever]
        public Patient? Patient { get; set; }
    }
}




