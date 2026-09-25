using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace MyDoc.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Συμπληρώστε το όνομα.")]
        [StringLength(
            60,
            ErrorMessage = "Το όνομα δεν μπορεί να ξεπερνά τους 60 χαρακτήρες.")]
        [RegularExpression(
            @"^[A-Za-zΑ-ΩΆΈΉΊΌΎΏΪΫα-ωάέήίόύώϊϋΐΰ\s'’-]+$",
            ErrorMessage = "Το όνομα μπορεί να περιέχει μόνο γράμματα.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Συμπληρώστε το επώνυμο.")]
        [StringLength(
            60,
            ErrorMessage = "Το επώνυμο δεν μπορεί να ξεπερνά τους 60 χαρακτήρες.")]
        [RegularExpression(
            @"^[A-Za-zΑ-ΩΆΈΉΊΌΎΏΪΫα-ωάέήίόύώϊϋΐΰ\s'’-]+$",
            ErrorMessage = "Το επώνυμο μπορεί να περιέχει μόνο γράμματα.")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Συμπληρώστε την ειδικότητα.")]
        [StringLength(
            100,
            ErrorMessage = "Η ειδικότητα δεν μπορεί να ξεπερνά τους 100 χαρακτήρες.")]
        public string Specialty { get; set; } = null!;

        [Required(ErrorMessage = "Συμπληρώστε το email.")]
        [StringLength(
            150,
            ErrorMessage = "Το email δεν μπορεί να ξεπερνά τους 150 χαρακτήρες.")]
        [RegularExpression(
            @"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[Hh][Oo][Ss][Pp][Ii][Tt][Aa][Ll]\.[Cc][Oo][Mm]$",
            ErrorMessage = "Χρησιμοποιήστε διεύθυνση email του νοσοκομείου, π.χ. name@hospital.com.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Συμπληρώστε το τηλέφωνο.")]
        [RegularExpression(
            @"^\d{10}$",
            ErrorMessage = "Το τηλέφωνο πρέπει να αποτελείται από 10 ψηφία.")]
        public string PhoneNumber { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public string? UserId { get; set; }

        public IdentityUser? User { get; set; }

        public ICollection<DoctorAvailability> Availabilities { get; set; }
            = new List<DoctorAvailability>();

        [NotMapped]
        public string DisplayName
        {
            get
            {
                return $"{FirstName} {LastName} ({Specialty})";
            }
        }
    }
}