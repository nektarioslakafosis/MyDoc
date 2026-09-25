using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models.Settings
{
    public class PatientSettingsVM
    {
        [Required]
        [Display(Name = "Όνομα")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Επώνυμο")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε το τηλέφωνο.")]
        [Phone(ErrorMessage = "Συμπληρώστε έγκυρο τηλέφωνο.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Το τηλέφωνο πρέπει να έχει ακριβώς 10 ψηφία.")]
        [RegularExpression(
            @"^\d{10}$",
            ErrorMessage = "Το τηλέφωνο πρέπει να αποτελείται από ακριβώς 10 ψηφία.")]
        [Display(Name = "Τηλέφωνο")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [MyDoc.Models.Validation.ReasonableBirthDate]
        [Display(Name = "Ημερομηνία Γέννησης")]
        public DateTime DateOfBirth { get; set; }
    }
}