using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace MyDoc.Areas.Identity.Pages.Account
{
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Συμπληρώστε το email σας.")]
            [EmailAddress(ErrorMessage = "Συμπληρώστε έγκυρο email.")]
            public string Email { get; set; } = "";
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null && !user.EmailConfirmed)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Επιβεβαίωση λογαριασμού MyDoc",
                    $"""
                    <p>Γεια σας,</p>
                    <p>Για να ενεργοποιήσετε τον λογαριασμό σας στο MyDoc, επιβεβαιώστε το email σας.</p>
                    <p>
                        <a href="{HtmlEncoder.Default.Encode(callbackUrl)}">
                            Επιβεβαίωση email
                        </a>
                    </p>
                    <p>Αν δεν δημιουργήσατε εσείς αυτόν τον λογαριασμό, αγνοήστε το μήνυμα.</p>
                    """);
            }

            StatusMessage =
                "Αν το email χρειάζεται επιβεβαίωση, στάλθηκε νέος σύνδεσμος. Αν έχει ήδη επιβεβαιωθεί, δεν απαιτείται άλλη ενέργεια.";
            return RedirectToPage();
        }
    }
}
