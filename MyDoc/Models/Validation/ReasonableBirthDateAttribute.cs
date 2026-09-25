using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models.Validation
{
    public class ReasonableBirthDateAttribute : ValidationAttribute
    {
        public int MaximumAge { get; set; } = 120;

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not DateTime date)
            {
                return new ValidationResult(
                    "Η ημερομηνία γέννησης δεν είναι έγκυρη.");
            }

            var today = DateTime.Today;
            var oldestAllowedDate = today.AddYears(-MaximumAge);

            if (date.Date > today)
            {
                return new ValidationResult(
                    "Η ημερομηνία γέννησης δεν μπορεί να είναι στο μέλλον.");
            }

            if (date.Date < oldestAllowedDate)
            {
                return new ValidationResult(
                    $"Η ημερομηνία γέννησης δεν μπορεί να είναι παλαιότερη από {MaximumAge} χρόνια.");
            }

            return ValidationResult.Success;
        }
    }
}
