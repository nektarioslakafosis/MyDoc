using Microsoft.AspNetCore.Identity;

namespace MyDoc.Services
{
    public class GreekIdentityErrorDescriber
        : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = nameof(DefaultError),
                Description =
                    "Παρουσιάστηκε σφάλμα. Δοκιμάστε ξανά."
            };
        }

        public override IdentityError ConcurrencyFailure()
        {
            return new IdentityError
            {
                Code = nameof(ConcurrencyFailure),
                Description =
                    "Τα στοιχεία του λογαριασμού άλλαξαν στο μεταξύ. Ανανεώστε τη σελίδα και δοκιμάστε ξανά."
            };
        }

        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description =
                    "Ο τρέχων κωδικός δεν είναι σωστός."
            };
        }

        public override IdentityError InvalidToken()
        {
            return new IdentityError
            {
                Code = nameof(InvalidToken),
                Description =
                    "Ο σύνδεσμος δεν είναι έγκυρος ή έχει λήξει."
            };
        }

        public override IdentityError LoginAlreadyAssociated()
        {
            return new IdentityError
            {
                Code = nameof(LoginAlreadyAssociated),
                Description =
                    "Αυτός ο τρόπος σύνδεσης χρησιμοποιείται ήδη από άλλο λογαριασμό."
            };
        }

        public override IdentityError InvalidUserName(
            string? userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description =
                    "Το email σύνδεσης δεν είναι έγκυρο."
            };
        }

        public override IdentityError InvalidEmail(
            string? email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                Description =
                    "Το email δεν είναι έγκυρο."
            };
        }

        public override IdentityError DuplicateUserName(
            string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description =
                    "Υπάρχει ήδη λογαριασμός με αυτό το email."
            };
        }

        public override IdentityError DuplicateEmail(
            string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description =
                    "Υπάρχει ήδη λογαριασμός με αυτό το email."
            };
        }

        public override IdentityError InvalidRoleName(
            string? role)
        {
            return new IdentityError
            {
                Code = nameof(InvalidRoleName),
                Description =
                    "Ο ρόλος του λογαριασμού δεν είναι έγκυρος."
            };
        }

        public override IdentityError DuplicateRoleName(
            string role)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateRoleName),
                Description =
                    "Αυτός ο ρόλος υπάρχει ήδη."
            };
        }

        public override IdentityError UserAlreadyHasPassword()
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyHasPassword),
                Description =
                    "Ο λογαριασμός διαθέτει ήδη κωδικό πρόσβασης."
            };
        }

        public override IdentityError UserLockoutNotEnabled()
        {
            return new IdentityError
            {
                Code = nameof(UserLockoutNotEnabled),
                Description =
                    "Δεν είναι ενεργοποιημένο το κλείδωμα για αυτόν τον λογαριασμό."
            };
        }

        public override IdentityError UserAlreadyInRole(
            string role)
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyInRole),
                Description =
                    "Ο λογαριασμός έχει ήδη αυτόν τον ρόλο."
            };
        }

        public override IdentityError UserNotInRole(
            string role)
        {
            return new IdentityError
            {
                Code = nameof(UserNotInRole),
                Description =
                    "Ο λογαριασμός δεν έχει αυτόν τον ρόλο."
            };
        }

        public override IdentityError PasswordTooShort(
            int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description =
                    $"Ο κωδικός πρέπει να έχει τουλάχιστον {length} χαρακτήρες."
            };
        }

        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description =
                    "Ο κωδικός πρέπει να περιέχει τουλάχιστον έναν ειδικό χαρακτήρα, όπως !, @ ή #."
            };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description =
                    "Ο κωδικός πρέπει να περιέχει τουλάχιστον έναν αριθμό."
            };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description =
                    "Ο κωδικός πρέπει να περιέχει τουλάχιστον ένα πεζό γράμμα."
            };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description =
                    "Ο κωδικός πρέπει να περιέχει τουλάχιστον ένα κεφαλαίο γράμμα."
            };
        }

        public override IdentityError PasswordRequiresUniqueChars(
            int uniqueChars)
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUniqueChars),
                Description =
                    $"Ο κωδικός πρέπει να περιέχει τουλάχιστον {uniqueChars} διαφορετικούς χαρακτήρες."
            };
        }

        public override IdentityError RecoveryCodeRedemptionFailed()
        {
            return new IdentityError
            {
                Code = nameof(RecoveryCodeRedemptionFailed),
                Description =
                    "Ο κωδικός ανάκτησης δεν είναι έγκυρος."
            };
        }
    }
}
