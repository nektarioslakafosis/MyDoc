using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class DoctorEditAppointmentVM
    {
        public int Id { get; set; }

        public AppointmentStatus CurrentStatus { get; set; }

        public AppointmentStatus Status { get; set; }

        [StringLength(600, ErrorMessage = "Ο λόγος ακύρωσης δεν μπορεί να ξεπερνά τους 600 χαρακτήρες.")]
        [Display(Name = "Λόγος ακύρωσης")]
        public string? CancellationReason { get; set; }

        public List<SelectListItem> AvailableStatuses { get; set; } = new();
    }
}
