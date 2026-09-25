#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Models;

namespace MyDoc.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Συμπληρώστε το email.")]
            [EmailAddress(ErrorMessage = "Συμπληρώστε έγκυρο email.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Συμπληρώστε τον κωδικό πρόσβασης.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Να παραμείνω συνδεδεμένος")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user != null)
            {
                var inactiveAccountMessage = await GetInactiveAccountMessageAsync(user);

                if (!string.IsNullOrWhiteSpace(inactiveAccountMessage))
                {
                    ModelState.AddModelError(string.Empty, inactiveAccountMessage);
                    return Page();
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                user ??= await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "Δεν ήταν δυνατή η σύνδεση. Παρακαλώ δοκιμάστε ξανά.");
                    return Page();
                }

                var inactiveAccountMessage = await GetInactiveAccountMessageAsync(user);

                if (!string.IsNullOrWhiteSpace(inactiveAccountMessage))
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, inactiveAccountMessage);
                    return Page();
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "AdminDashboard");

                if (await _userManager.IsInRoleAsync(user, "Doctor"))
                    return RedirectToAction("Index", "DoctorDashboard");

                if (await _userManager.IsInRoleAsync(user, "Patient"))
                    return RedirectToAction("Index", "PatientDashboard");

                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty, "Ο λογαριασμός σας δεν έχει έγκυρο ρόλο στην εφαρμογή.");
                return Page();
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");

                ModelState.AddModelError(
                    string.Empty,
                    "Ο λογαριασμός σας είναι προσωρινά ή μόνιμα κλειδωμένος. Αν θεωρείτε ότι έγινε λάθος, επικοινωνήστε με τον διαχειριστή.");

                return Page();
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Πρέπει πρώτα να επιβεβαιώσετε το email σας πριν συνδεθείτε.");

                return Page();
            }

            ModelState.AddModelError(string.Empty, "Το email ή ο κωδικός πρόσβασης δεν είναι σωστά.");
            return Page();
        }

        private async Task<string> GetInactiveAccountMessageAsync(IdentityUser user)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (doctor != null && !doctor.IsActive)
            {
                return "Η πρόσβασή σας ως ιατρός έχει απενεργοποιηθεί. Δεν μπορείτε πλέον να συνδεθείτε στον λογαριασμό σας. Αν θεωρείτε ότι έγινε λάθος, επικοινωνήστε με τον διαχειριστή.";
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patient != null && !patient.IsActive)
            {
                return "Ο λογαριασμός σας έχει απενεργοποιηθεί και δεν μπορείτε να συνδεθείτε στην εφαρμογή. Αν θεωρείτε ότι έγινε λάθος, επικοινωνήστε με τον διαχειριστή.";
            }

            return null;
        }
    }
}
