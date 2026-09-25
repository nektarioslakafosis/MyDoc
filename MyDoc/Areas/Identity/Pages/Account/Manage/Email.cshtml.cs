#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;

namespace MyDoc.Areas.Identity.Pages.Account.Manage
{
    [Authorize(Roles = "Patient")]
    public class EmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public EmailModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
        }

        public string Email { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Συμπληρώστε το νέο email.")]
            [EmailAddress(ErrorMessage = "Συμπληρώστε έγκυρο email.")]
            [RegularExpression(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                ErrorMessage = "Χρησιμοποιήστε έγκυρη μορφή email, π.χ. name@example.com.")]
            [Display(Name = "Νέο email")]
            public string NewEmail { get; set; }
        }

        private async Task<bool> IsActivePatientAsync(IdentityUser user)
        {
            return await _context.Patients
                .AnyAsync(p => p.UserId == user.Id && p.IsActive);
        }

        private async Task LoadAsync(
            IdentityUser user)
        {
            Email =
                await _userManager.GetEmailAsync(user);

            IsEmailConfirmed =
                await _userManager
                    .IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!await IsActivePatientAsync(user))
                return Forbid();

            await LoadAsync(user);
            Input = new InputModel();
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!await IsActivePatientAsync(user))
                return Forbid();

            if (!string.IsNullOrEmpty(Input?.NewEmail))
            {
                Input.NewEmail = Input.NewEmail.Trim();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var currentEmail = await _userManager.GetEmailAsync(user);

            if (string.Equals(
                Input.NewEmail?.Trim(),
                currentEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage =
                    "Warning: Το νέο email πρέπει να είναι διαφορετικό από το τρέχον email.";

                return RedirectToPage();
            }

            var existingUser = await _userManager.FindByEmailAsync(Input.NewEmail);

            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(string.Empty, "Υπάρχει ήδη λογαριασμός με αυτό το email.");
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChange",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    userId = userId,
                    email = Input.NewEmail,
                    code = code
                },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.NewEmail,
                "Επιβεβαίωση αλλαγής email",
                $"Για να ολοκληρώσετε την αλλαγή email, πατήστε εδώ: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Επιβεβαίωση email</a>.");

            StatusMessage = "Στάλθηκε σύνδεσμος επιβεβαίωσης στο νέο email. Η αλλαγή θα ολοκληρωθεί μόνο αφού πατήσετε τον σύνδεσμο.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!await IsActivePatientAsync(user))
                return Forbid();

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    userId = userId,
                    code = code
                },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "Επιβεβαίωση email",
                $"Πατήστε εδώ για να επιβεβαιώσετε το email σας: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Επιβεβαίωση email</a>.");

            StatusMessage = "Στάλθηκε email επιβεβαίωσης.";
            return RedirectToPage();
        }
    }
}
