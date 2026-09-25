#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MyDoc.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        
        [BindProperty]
        public InputModel Input { get; set; }

        
        [TempData]
        public string StatusMessage { get; set; }

        
        public class InputModel
        {

            [Required(ErrorMessage = "Συμπληρώστε τον τρέχοντα κωδικό.")]
            [DataType(DataType.Password)]
            [Display(Name = "Τρέχων κωδικός")]
            public string OldPassword { get; set; }


            [Required(ErrorMessage = "Συμπληρώστε νέο κωδικό.")]
            [StringLength(
                100,
                ErrorMessage = "Ο νέος κωδικός πρέπει να έχει από {2} έως {1} χαρακτήρες.",
                MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Νέος κωδικός")]
            public string NewPassword { get; set; }


            [DataType(DataType.Password)]
            [Display(Name = "Επιβεβαίωση νέου κωδικού")]
            [Compare(
                "NewPassword",
                ErrorMessage = "Οι δύο κωδικοί δεν είναι ίδιοι.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToPage("./SetPassword");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var currentPasswordIsCorrect =
                await _userManager.CheckPasswordAsync(
                    user,
                    Input.OldPassword);

            if (currentPasswordIsCorrect &&
                Input.OldPassword == Input.NewPassword)
            {
                ModelState.AddModelError(
                    "Input.NewPassword",
                    "Ο νέος κωδικός πρέπει να είναι διαφορετικός από τον τρέχοντα.");

                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage =
                "Ο κωδικός σας άλλαξε επιτυχώς.";

            return RedirectToPage();
        }
    }
}
