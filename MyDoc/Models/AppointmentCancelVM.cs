using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentCancelVM
    {
        public int Id { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public string Specialty { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε τον λόγο ακύρωσης.")]
        [StringLength(600, ErrorMessage = "Ο λόγος ακύρωσης δεν μπορεί να ξεπερνά τους 600 χαρακτήρες.")]
        [Display(Name = "Λόγος ακύρωσης")]
        public string CancellationReason { get; set; } = string.Empty;
    }
}
