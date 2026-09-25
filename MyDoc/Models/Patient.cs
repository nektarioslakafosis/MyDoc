using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MyDoc.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Συμπληρώστε το όνομα.")]
        [StringLength(60)]
        [RegularExpression(
            @"^[\p{L}\s'-]+$",
            ErrorMessage = "Το όνομα μπορεί να περιέχει μόνο γράμματα.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Συμπληρώστε το επώνυμο.")]
        [StringLength(60)]
        [RegularExpression(
            @"^[\p{L}\s'-]+$",
            ErrorMessage = "Το επώνυμο μπορεί να περιέχει μόνο γράμματα.")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Συμπληρώστε το email.")]
        [StringLength(150)]
        [RegularExpression(
            @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
            ErrorMessage = "Συμπληρώστε έγκυρη διεύθυνση email.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Συμπληρώστε το τηλέφωνο.")]
        [RegularExpression(
            @"^\d{10}$",
            ErrorMessage = "Το τηλέφωνο πρέπει να αποτελείται από 10 ψηφία.")]
        public string PhoneNumber { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public bool IsActive { get; set; } = true;

        public string UserId { get; set; } = null!;

        public IdentityUser User { get; set; } = null!;
    }
}