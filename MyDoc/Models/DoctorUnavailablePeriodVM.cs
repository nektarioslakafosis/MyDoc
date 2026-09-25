using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class DoctorUnavailablePeriodVM
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ημερομηνία")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Ώρα Έναρξης")]
        public TimeOnly StartTime { get; set; }

        [Required]
        [Display(Name = "Ώρα Λήξης")]
        public TimeOnly EndTime { get; set; }

        [StringLength(250)]
        [Display(Name = "Λόγος")]
        public string? Reason { get; set; }
    }
}
