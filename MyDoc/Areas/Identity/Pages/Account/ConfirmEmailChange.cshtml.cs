using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;

namespace MyDoc.Areas.Identity.Pages.Account
{
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ConfirmEmailChangeModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public bool IsSuccess { get; set; }
        public string StatusMessage { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code))
            {
                IsSuccess = false;
                StatusMessage = "Μη έγκυρο link αλλαγής email.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                IsSuccess = false;
                StatusMessage = "Ο χρήστης δεν βρέθηκε.";
                return Page();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);

            if (patient == null)
            {
                IsSuccess = false;
                StatusMessage = "Η αλλαγή email επιτρέπεται μόνο σε ενεργούς λογαριασμούς ασθενών.";
                return Page();
            }

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null && existingUser.Id != user.Id)
            {
                IsSuccess = false;
                StatusMessage = "Υπάρχει ήδη λογαριασμός με αυτό το email.";
                return Page();
            }

            string decodedCode;

            try
            {
                decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch
            {
                IsSuccess = false;
                StatusMessage = "Το link αλλαγής email δεν είναι έγκυρο.";
                return Page();
            }

            var changeEmailResult = await _userManager.ChangeEmailAsync(user, email, decodedCode);

            if (!changeEmailResult.Succeeded)
            {
                IsSuccess = false;
                StatusMessage = "Η αλλαγή email απέτυχε. Το link ίσως είναι άκυρο, έχει λήξει ή έχει ήδη χρησιμοποιηθεί.";
                return Page();
            }

            var setUserNameResult = await _userManager.SetUserNameAsync(user, email);

            if (!setUserNameResult.Succeeded)
            {
                IsSuccess = false;
                StatusMessage = "Το email άλλαξε, αλλά απέτυχε η ενημέρωση του username. Επικοινωνήστε με τον διαχειριστή.";
                return Page();
            }

            patient.Email = email;

            await _context.SaveChangesAsync();

            await _userManager.UpdateSecurityStampAsync(user);

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUserId = _userManager.GetUserId(User);

                if (currentUserId == user.Id)
                {
                    await _signInManager.SignOutAsync();
                }
            }

            IsSuccess = true;
            StatusMessage = "Το email σας άλλαξε και επιβεβαιώθηκε επιτυχώς. Για λόγους ασφαλείας, συνδεθείτε ξανά χρησιμοποιώντας το νέο email.";

            return Page();
        }
    }
}