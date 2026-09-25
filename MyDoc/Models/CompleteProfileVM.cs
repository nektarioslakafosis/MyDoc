using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class CompleteProfileVM
    {
        [Required(ErrorMessage = "Συμπληρώστε το όνομά σας.")]
        [StringLength(80, ErrorMessage = "Το όνομα δεν μπορεί να ξεπερνά τους 80 χαρακτήρες.")]
        [Display(Name = "Όνομα")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε το επώνυμό σας.")]
        [StringLength(80, ErrorMessage = "Το επώνυμο δεν μπορεί να ξεπερνά τους 80 χαρακτήρες.")]
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

        [Required(ErrorMessage = "Επιλέξτε ημερομηνία γέννησης.")]
        [DataType(DataType.Date)]
        [MyDoc.Models.Validation.ReasonableBirthDate]
        [Display(Name = "Ημερομηνία γέννησης")]
        public DateTime? DateOfBirth { get; set; }
    }
}
