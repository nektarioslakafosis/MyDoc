using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Extensions;
using MyDoc.Models.Settings;

namespace MyDoc.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SettingsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Patient"))
                return RedirectToAction(nameof(PatientProfile));

            if (User.IsInRole("Doctor"))
                return RedirectToAction(nameof(DoctorProfile));

            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(AdminAccount));

            return Forbid();
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> PatientProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.UserId == user.Id &&
                    p.IsActive);

            if (patient == null)
                return Forbid();

            return View(new PatientSettingsVM
            {
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                PhoneNumber = patient.PhoneNumber,
                DateOfBirth = patient.DateOfBirth
            });
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientProfile(
            PatientSettingsVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.UserId == user.Id &&
                    p.IsActive);

            if (patient == null)
                return Forbid();

            patient.FirstName = vm.FirstName;
            patient.LastName = vm.LastName;
            patient.PhoneNumber = vm.PhoneNumber;
            patient.DateOfBirth = vm.DateOfBirth;

            if (user.PhoneNumber != vm.PhoneNumber)
            {
                var phoneResult =
                    await _userManager.SetPhoneNumberAsync(
                        user,
                        vm.PhoneNumber);

                if (!phoneResult.Succeeded)
                {
                    AddIdentityErrors(phoneResult);
                    return View(vm);
                }
            }

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Οι αλλαγές αποθηκεύτηκαν.");

            return RedirectToAction(
                nameof(PatientProfile));
        }

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DoctorProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d =>
                    d.UserId == user.Id &&
                    d.IsActive);

            if (doctor == null)
                return Forbid();

            return View(new DoctorSettingsVM
            {
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Specialty = doctor.Specialty,
                Email = doctor.Email,
                PhoneNumber = doctor.PhoneNumber
            });
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorProfile(
            DoctorSettingsVM vm)
        {
            ModelState.Remove(
                nameof(DoctorSettingsVM.FirstName));

            ModelState.Remove(
                nameof(DoctorSettingsVM.LastName));

            ModelState.Remove(
                nameof(DoctorSettingsVM.Specialty));

            ModelState.Remove(
                nameof(DoctorSettingsVM.Email));

            if (!ModelState.IsValid)
                return View(vm);

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var doctor =
                await _context.Doctors
                    .FirstOrDefaultAsync(d =>
                        d.UserId == user.Id &&
                        d.IsActive);

            if (doctor == null)
                return Forbid();

            doctor.PhoneNumber =
                vm.PhoneNumber;

            if (user.PhoneNumber != vm.PhoneNumber)
            {
                var phoneResult =
                    await _userManager.SetPhoneNumberAsync(
                        user,
                        vm.PhoneNumber);

                if (!phoneResult.Succeeded)
                {
                    AddIdentityErrors(phoneResult);

                    vm.FirstName =
                        doctor.FirstName;

                    vm.LastName =
                        doctor.LastName;

                    vm.Specialty =
                        doctor.Specialty;

                    vm.Email =
                        doctor.Email;

                    return View(vm);
                }
            }

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Οι αλλαγές αποθηκεύτηκαν.");

            return RedirectToAction(
                nameof(DoctorProfile));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccount()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            return View(new AdminSettingsVM
            {
                Email =
                    user.Email ?? string.Empty,

                UserName =
                    user.UserName ?? string.Empty
            });
        }

        private void AddIdentityErrors(
            IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }
        }
    }
}