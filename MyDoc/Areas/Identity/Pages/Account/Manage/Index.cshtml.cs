#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyDoc.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToAction("Index", "Settings", new { area = "" });
        }

        public IActionResult OnPost()
        {
            return RedirectToAction("Index", "Settings", new { area = "" });
        }
    }
}
