#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using MyDoc.Data;
using MyDoc.Models;

namespace MyDoc.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Συμπληρώστε το email.")]
            [EmailAddress(ErrorMessage = "Συμπληρώστε έγκυρο email.")]
            [RegularExpression(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                ErrorMessage = "Χρησιμοποιήστε έγκυρη μορφή email, π.χ. name@example.com.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Συμπληρώστε τον κωδικό πρόσβασης.")]
            [StringLength(
                100,
                ErrorMessage = "Ο κωδικός πρέπει να έχει τουλάχιστον {2} χαρακτήρες.",
                MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Κωδικός πρόσβασης")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Επιβεβαιώστε τον κωδικό πρόσβασης.")]
            [DataType(DataType.Password)]
            [Display(Name = "Επιβεβαίωση κωδικού")]
            [Compare(
                "Password",
                ErrorMessage = "Οι δύο κωδικοί δεν ταιριάζουν.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Υπάρχει ήδη λογαριασμός με αυτό το email.");
                return Page();
            }

            var user = CreateUser();

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                await _userManager.AddToRoleAsync(user, "Patient");

                var patientExists = _context.Patients.Any(p => p.UserId == user.Id);
                if (!patientExists)
                {
                    var patient = new Patient
                    {
                        UserId = user.Id,
                        Email = Input.Email,
                        FirstName = "",
                        LastName = "",
                        PhoneNumber = "",
                        DateOfBirth = default
                    };

                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }

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

                return RedirectToPage("./RegisterConfirmation", new { email = Input.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
