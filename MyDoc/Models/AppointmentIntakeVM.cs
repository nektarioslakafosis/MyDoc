using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentIntakeVM
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public string Specialty { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε τον βασικό λόγο επίσκεψης.")]
        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Βασικός λόγος επίσκεψης")]
        public string MainConcern { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Συμπτώματα")]
        public string? Symptoms { get; set; }

        [StringLength(500, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 500 χαρακτήρες.")]
        [Display(Name = "Πότε ξεκίνησε")]
        public string? StartedWhen { get; set; }

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Φάρμακα που λαμβάνετε")]
        public string? CurrentMedications { get; set; }

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Αλλεργίες")]
        public string? Allergies { get; set; }

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Προηγούμενες σχετικές εξετάσεις")]
        public string? PreviousRelevantExams { get; set; }

        [StringLength(1500, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1500 χαρακτήρες.")]
        [Display(Name = "Επιπλέον σημειώσεις")]
        public string? ExtraNotes { get; set; }
    }
}
