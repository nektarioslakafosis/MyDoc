using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class AppointmentRescheduleVM
    {
        public int AppointmentId { get; set; }

        public int DoctorId { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public string Specialty { get; set; } = string.Empty;

        public AppointmentStatus CurrentStatus { get; set; }

        public DateTime CurrentAppointmentDate { get; set; }

        public DateTime CalendarMonth { get; set; } = DateTime.Today;

        public string? SelectedSlot { get; set; }

        [Required(ErrorMessage = "Συμπληρώστε τον λόγο αλλαγής ραντεβού.")]
        [StringLength(600, ErrorMessage = "Ο λόγος αλλαγής δεν μπορεί να ξεπερνά τους 600 χαρακτήρες.")]
        [Display(Name = "Λόγος αλλαγής")]
        public string Reason { get; set; } = string.Empty;

        public List<CalendarSlotVM> CalendarSlots { get; set; } = new();
    }
}