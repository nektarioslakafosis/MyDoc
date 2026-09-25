using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentDocument
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required]
        public string UploadedByUserId { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string UploadedByRole { get; set; } = null!;

        [Required]
        [StringLength(260)]
        public string OriginalFileName { get; set; } = null!;

        [Required]
        [StringLength(120)]
        public string StoredFileName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public Appointment Appointment { get; set; } = null!;
    }
}
