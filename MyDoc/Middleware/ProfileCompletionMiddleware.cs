using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Models;

namespace MyDoc.Middleware
{
    public class ProfileCompletionMiddleware
    {
        private readonly RequestDelegate _next;

        public ProfileCompletionMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            if (!context.User.IsInRole("Patient"))
            {
                await _next(context);
                return;
            }

            var path =
                context.Request.Path;

            
            if (
                path.StartsWithSegments("/Patients/CompleteProfile") ||
                path.StartsWithSegments("/Identity/Account/Logout") ||
                path.StartsWithSegments("/Identity/Account/AccessDenied") ||
                path.StartsWithSegments("/Home/Privacy") ||
                path.StartsWithSegments("/Home/Contact") ||
                path.StartsWithSegments("/css") ||
                path.StartsWithSegments("/js") ||
                path.StartsWithSegments("/lib") ||
                path.StartsWithSegments("/favicon"))
            {
                await _next(context);
                return;
            }

            var user =
                await userManager.GetUserAsync(
                    context.User);

            if (user == null)
            {
                await _next(context);
                return;
            }

            var patient =
                await db.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id);

            if (patient == null)
            {
                db.Patients.Add(
                    new Patient
                    {
                        UserId = user.Id,
                        Email = user.Email ?? string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        PhoneNumber = string.Empty,
                        DateOfBirth = default,
                        IsActive = true
                    });

                await db.SaveChangesAsync();

                context.Response.Redirect(
                    "/Patients/CompleteProfile");

                return;
            }

            if (!patient.IsActive)
            {
                context.Response.Redirect(
                    "/Identity/Account/AccessDenied");

                return;
            }

            var profileIncomplete =
                string.IsNullOrWhiteSpace(
                    patient.FirstName) ||
                string.IsNullOrWhiteSpace(
                    patient.LastName) ||
                string.IsNullOrWhiteSpace(
                    patient.PhoneNumber) ||
                patient.DateOfBirth == default;

            if (profileIncomplete)
            {
                context.Response.Redirect(
                    "/Patients/CompleteProfile");

                return;
            }

            await _next(context);
        }
    }
}