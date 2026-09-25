using Microsoft.AspNetCore.Identity;

namespace MyDoc.Services
{
    public class UserRoleService
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UserRoleService(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task SetUserRoleAsync(IdentityUser user, string newRole)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Any())
                await _userManager.RemoveFromRolesAsync(user, roles);

            await _userManager.AddToRoleAsync(user, newRole);
        }
    }
}
