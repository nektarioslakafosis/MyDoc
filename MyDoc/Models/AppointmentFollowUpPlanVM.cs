using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentFollowUpPlanVM
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string PatientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε τις οδηγίες μετά την επίσκεψη.")]
        [StringLength(1500, ErrorMessage = "Οι οδηγίες δεν μπορούν να ξεπερνούν τους 1500 χαρακτήρες.")]
        [Display(Name = "Οδηγίες μετά την επίσκεψη")]
        public string Instructions { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Προτεινόμενες εξετάσεις")]
        public string? RecommendedTests { get; set; }

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Σημειώσεις για φάρμακα / αγωγή")]
        public string? MedicationNotes { get; set; }

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Σημάδια που πρέπει να προσέξει ο ασθενής")]
        public string? WarningSigns { get; set; }

        [StringLength(1000, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.")]
        [Display(Name = "Σύσταση επανεξέτασης")]
        public string? FollowUpRecommendation { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Προτεινόμενη ημερομηνία επανεξέτασης")]
        public DateTime? SuggestedFollowUpDate { get; set; }

        [StringLength(1500, ErrorMessage = "Το πεδίο δεν μπορεί να ξεπερνά τους 1500 χαρακτήρες.")]
        [Display(Name = "Επιπλέον σημειώσεις")]
        public string? ExtraNotes { get; set; }
    }
}
