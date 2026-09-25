using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentPrivateNoteVM
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public AppointmentStatus AppointmentStatus { get; set; }

        [Required(ErrorMessage = "Συμπληρώστε τη σημείωση.")]
        [StringLength(4000, ErrorMessage = "Η σημείωση δεν μπορεί να ξεπερνά τους 4000 χαρακτήρες.")]
        [Display(Name = "Private doctor notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
