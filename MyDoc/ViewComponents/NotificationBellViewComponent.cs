using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDoc.Services;

namespace MyDoc.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotificationService _notificationService;

        public NotificationBellViewComponent(
            UserManager<IdentityUser> userManager,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
                return Content("");

            var unreadCount = await _notificationService.GetUnreadCountAsync(user.Id);

            return View(unreadCount);
        }
    }
}
