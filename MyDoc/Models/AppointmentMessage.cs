using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentMessage
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required]
        public string SenderUserId { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string SenderRole { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        public Appointment Appointment { get; set; } = null!;
    }
}
