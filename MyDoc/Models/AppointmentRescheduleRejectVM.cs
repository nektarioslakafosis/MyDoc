using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentRescheduleRejectVM
    {
        public int RescheduleRequestId { get; set; }

        [Required(ErrorMessage = "Συμπληρώστε λόγο απόρριψης.")]
        [StringLength(600, ErrorMessage = "Ο λόγος απόρριψης δεν μπορεί να ξεπερνά τους 600 χαρακτήρες.")]
        public string RejectionReason { get; set; } = string.Empty;
    }
}
