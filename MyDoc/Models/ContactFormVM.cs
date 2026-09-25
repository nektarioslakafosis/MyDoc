using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class ContactFormVM
    {
        [Required(ErrorMessage = "Συμπληρώστε το όνομά σας.")]
        [StringLength(100, ErrorMessage = "Το όνομα δεν μπορεί να ξεπερνά τους 100 χαρακτήρες.")]
        [Display(Name = "Ονοματεπώνυμο")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε το email σας.")]
        [EmailAddress(ErrorMessage = "Συμπληρώστε έγκυρο email.")]
        [StringLength(150, ErrorMessage = "Το email δεν μπορεί να ξεπερνά τους 150 χαρακτήρες.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε θέμα.")]
        [StringLength(150, ErrorMessage = "Το θέμα δεν μπορεί να ξεπερνά τους 150 χαρακτήρες.")]
        [Display(Name = "Θέμα")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Συμπληρώστε μήνυμα.")]
        [StringLength(2000, ErrorMessage = "Το μήνυμα δεν μπορεί να ξεπερνά τους 2000 χαρακτήρες.")]
        [Display(Name = "Μήνυμα")]
        public string Message { get; set; } = string.Empty;
    }
}