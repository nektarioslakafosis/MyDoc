using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentPrivateNote
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public int DoctorId { get; set; }

        [Required]
        [StringLength(4000)]
        public string Notes { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public Appointment Appointment { get; set; } = null!;

        public Doctor Doctor { get; set; } = null!;
    }
}
