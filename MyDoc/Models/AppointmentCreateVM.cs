using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyDoc.Models
{
    public class AppointmentCreateVM
    {
        public int DoctorId { get; set; }

        public DateTime CalendarMonth { get; set; } = DateTime.Today;

        public string? SelectedSlot { get; set; }

        public string Description { get; set; } = string.Empty;

        public List<SelectListItem> Doctors { get; set; } = new();

        public List<CalendarSlotVM> CalendarSlots { get; set; } = new();
    }
}
