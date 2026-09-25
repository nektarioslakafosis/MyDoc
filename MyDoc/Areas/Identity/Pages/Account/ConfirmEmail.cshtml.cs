using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace MyDoc.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ConfirmEmailModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public bool IsSuccess { get; set; }
        public string StatusMessage { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                StatusMessage = "Μη έγκυρο link επιβεβαίωσης.";
                IsSuccess = false;
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Ο χρήστης δεν βρέθηκε.";
                IsSuccess = false;
                return Page();
            }

            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

            IsSuccess = result.Succeeded;
            StatusMessage = result.Succeeded
                ? "Το email σας επιβεβαιώθηκε επιτυχώς. Μπορείτε τώρα να συνδεθείτε."
                : "Η επιβεβαίωση απέτυχε. Το link ίσως είναι άκυρο ή έχει ήδη χρησιμοποιηθεί.";

            return Page();
        }
    }
}