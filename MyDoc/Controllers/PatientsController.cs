using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Extensions;
using MyDoc.Models;

namespace MyDoc.Controllers
{
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PatientsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var patients =
                await _context.Patients
                    .OrderByDescending(p => p.IsActive)
                    .ThenBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToListAsync();

            return View(patients);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);

            if (patient == null)
                return NotFound();

            var now = DateTime.Now;

            ViewBag.TotalAppointmentsCount =
                await _context.Appointments
                    .CountAsync(a =>
                        a.PatientId == patient.Id);

            ViewBag.UpcomingAppointmentsCount =
                await _context.Appointments
                    .CountAsync(a =>
                        a.PatientId == patient.Id &&
                        a.AppointmentDate >= now &&
                        (
                            a.Status ==
                            AppointmentStatus.Pending ||
                            a.Status ==
                            AppointmentStatus.Approved
                        ));

            return View(patient);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var patient =
                await _context.Patients.FindAsync(id);

            if (patient == null)
                return NotFound();

            return View(patient);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    [Bind("Id,FirstName,LastName,Email,PhoneNumber,DateOfBirth")]
    Patient input)
        {
            if (id != input.Id)
                return NotFound();

            ModelState.Remove(
                nameof(Patient.UserId));

            ModelState.Remove(
                nameof(Patient.User));

            if (input.DateOfBirth == default)
            {
                ModelState.AddModelError(
                    nameof(Patient.DateOfBirth),
                    "Συμπληρώστε την ημερομηνία γέννησης.");
            }
            else if (
                input.DateOfBirth.Date >
                DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(Patient.DateOfBirth),
                    "Η ημερομηνία γέννησης δεν μπορεί να είναι μελλοντική.");
            }
            else if (
                input.DateOfBirth.Date <
                DateTime.Today.AddYears(-120))
            {
                ModelState.AddModelError(
                    nameof(Patient.DateOfBirth),
                    "Συμπληρώστε έγκυρη ημερομηνία γέννησης.");
            }

            if (!ModelState.IsValid)
                return View(input);

            input.FirstName =
                input.FirstName.Trim();

            input.LastName =
                input.LastName.Trim();

            input.Email =
                input.Email.Trim();

            input.PhoneNumber =
                input.PhoneNumber.Trim();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);

            if (patient == null)
                return NotFound();

            var user =
                await _userManager.FindByIdAsync(
                    patient.UserId);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Ο λογαριασμός σύνδεσης του ασθενή δεν βρέθηκε.");

                return View(input);
            }

            var emailChanged =
                !string.Equals(
                    patient.Email,
                    input.Email,
                    StringComparison.OrdinalIgnoreCase);

            if (emailChanged)
            {
                var existingUser =
                    await _userManager.FindByEmailAsync(
                        input.Email);

                if (existingUser != null &&
                    existingUser.Id != user.Id)
                {
                    ModelState.AddModelError(
                        nameof(Patient.Email),
                        "Υπάρχει ήδη λογαριασμός με αυτό το email.");

                    return View(input);
                }

                var setEmailResult =
                    await _userManager.SetEmailAsync(
                        user,
                        input.Email);

                if (!setEmailResult.Succeeded)
                {
                    AddIdentityErrors(
                        setEmailResult);

                    return View(input);
                }

                var setUserNameResult =
                    await _userManager.SetUserNameAsync(
                        user,
                        input.Email);

                if (!setUserNameResult.Succeeded)
                {
                    AddIdentityErrors(
                        setUserNameResult);

                    return View(input);
                }

                user.EmailConfirmed = true;

                var updateUserResult =
                    await _userManager.UpdateAsync(user);

                if (!updateUserResult.Succeeded)
                {
                    AddIdentityErrors(
                        updateUserResult);

                    return View(input);
                }
            }

            if (user.PhoneNumber !=
                input.PhoneNumber)
            {
                var setPhoneResult =
                    await _userManager
                        .SetPhoneNumberAsync(
                            user,
                            input.PhoneNumber);

                if (!setPhoneResult.Succeeded)
                {
                    AddIdentityErrors(
                        setPhoneResult);

                    return View(input);
                }
            }

            patient.FirstName =
                input.FirstName;

            patient.LastName =
                input.LastName;

            patient.Email =
                input.Email;

            patient.PhoneNumber =
                input.PhoneNumber;

            patient.DateOfBirth =
                input.DateOfBirth.Date;

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Τα στοιχεία του ασθενή ενημερώθηκαν.");

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);

            if (patient == null)
                return NotFound();

            ViewBag.FutureActiveAppointmentsCount =
                await _context.Appointments
                    .CountAsync(a =>
                        a.PatientId == patient.Id &&
                        a.AppointmentDate >= DateTime.Now &&
                        (
                            a.Status ==
                            AppointmentStatus.Pending ||
                            a.Status ==
                            AppointmentStatus.Approved
                        ));

            return View(patient);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(
    int id)
        {
            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);

            if (patient == null)
                return RedirectToAction(nameof(Index));

            if (!patient.IsActive)
                return RedirectToAction(nameof(Index));

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            var now = DateTime.Now;

            var adminUserId =
                _userManager.GetUserId(User);

            patient.IsActive = false;

            var futureAppointments =
                await _context.Appointments
                    .Where(a =>
                        a.PatientId == patient.Id &&
                        a.AppointmentDate >= now &&
                        (
                            a.Status ==
                            AppointmentStatus.Pending ||
                            a.Status ==
                            AppointmentStatus.Approved
                        ))
                    .ToListAsync();

            foreach (var appointment
                     in futureAppointments)
            {
                appointment.Status =
                    AppointmentStatus.Cancelled;

                appointment.CancellationReason =
                    "Το ραντεβού ακυρώθηκε λόγω απενεργοποίησης του λογαριασμού του ασθενή.";

                appointment.CancelledAt =
                    now;

                appointment.CancelledByRole =
                    "Admin";
            }

            var appointmentIds =
                futureAppointments
                    .Select(a => a.Id)
                    .ToList();

            if (appointmentIds.Any())
            {
                var pendingRequests =
                    await _context
                        .AppointmentRescheduleRequests
                        .Where(r =>
                            appointmentIds.Contains(
                                r.AppointmentId) &&
                            r.Status ==
                            AppointmentRescheduleRequestStatus.Pending)
                        .ToListAsync();

                foreach (var request
                         in pendingRequests)
                {
                    request.Status =
                        AppointmentRescheduleRequestStatus.Rejected;

                    request.ReviewedAt =
                        now;

                    request.ReviewedByUserId =
                        adminUserId;

                    request.ReviewComment =
                        "Το ραντεβού ακυρώθηκε λόγω απενεργοποίησης του λογαριασμού του ασθενή.";
                }
            }

            var lockUserResult =
                await LockUserAccountAsync(
                    patient.UserId);

            if (!lockUserResult.Succeeded)
            {
                await transaction.RollbackAsync();

                AddIdentityErrors(
                    lockUserResult);

                return View(
                    "Delete",
                    patient);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            if (futureAppointments.Any())
            {
                TempData.FlashWarning(
                    $"Ο ασθενής απενεργοποιήθηκε και ακυρώθηκαν {futureAppointments.Count} μελλοντικά ραντεβού.");
            }
            else
            {
                TempData.FlashSuccess(
                    "Ο ασθενής απενεργοποιήθηκε.");
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);

            if (patient == null)
                return NotFound();

            patient.IsActive = true;

            var unlockUserResult =
                await UnlockUserAccountAsync(
                    patient.UserId);

            if (!unlockUserResult.Succeeded)
            {
                AddIdentityErrors(
                    unlockUserResult);

                return RedirectToAction(
                    nameof(Index));
            }

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Ο ασθενής ενεργοποιήθηκε ξανά.");

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> CompleteProfile()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id);

            if (patient != null && !patient.IsActive)
                return Forbid();

            if (patient != null &&
                IsProfileComplete(patient))
            {
                return RedirectToAction(
                    "Index",
                    "PatientDashboard");
            }

            var vm = new CompleteProfileVM
            {
                FirstName = patient?.FirstName ?? string.Empty,
                LastName = patient?.LastName ?? string.Empty,
                PhoneNumber = patient?.PhoneNumber ?? string.Empty,
                DateOfBirth =
                    patient != null &&
                    patient.DateOfBirth != default
                        ? patient.DateOfBirth
                        : null
            };

            return View(vm);
        }

        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(
            CompleteProfileVM vm)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id);

            if (patient != null && !patient.IsActive)
                return Forbid();

            if (patient != null &&
                IsProfileComplete(patient))
            {
                return RedirectToAction(
                    "Index",
                    "PatientDashboard");
            }

            if (vm.DateOfBirth.HasValue &&
                vm.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(vm.DateOfBirth),
                    "Η ημερομηνία γέννησης δεν μπορεί να είναι μελλοντική.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var phoneNumber =
                vm.PhoneNumber.Trim();

            if (user.PhoneNumber != phoneNumber)
            {
                var phoneResult =
                    await _userManager.SetPhoneNumberAsync(
                        user,
                        phoneNumber);

                if (!phoneResult.Succeeded)
                {
                    AddIdentityErrors(phoneResult);
                    return View(vm);
                }
            }

            if (patient == null)
            {
                patient =
                    new Patient
                    {
                        UserId = user.Id,
                        Email = user.Email ?? string.Empty,
                        IsActive = true
                    };

                _context.Patients.Add(patient);
            }

            patient.FirstName =
                vm.FirstName.Trim();

            patient.LastName =
                vm.LastName.Trim();

            patient.PhoneNumber =
                phoneNumber;

            patient.DateOfBirth =
                vm.DateOfBirth!.Value.Date;

            if (string.IsNullOrWhiteSpace(patient.Email))
            {
                patient.Email =
                    user.Email ?? string.Empty;
            }

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Το προφίλ σας ολοκληρώθηκε.");

            return RedirectToAction(
                "Index",
                "PatientDashboard");
        }




        //PRIVATE METHODS

        private async Task<IdentityResult>
            LockUserAccountAsync(
                string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return IdentityResult.Success;

            var user =
                await _userManager.FindByIdAsync(
                    userId);

            if (user == null)
                return IdentityResult.Success;

            user.LockoutEnabled = true;

            user.LockoutEnd =
                DateTimeOffset.MaxValue;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
                return updateResult;

            return await _userManager
                .UpdateSecurityStampAsync(user);
        }

        private async Task<IdentityResult>
            UnlockUserAccountAsync(
                string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return IdentityResult.Success;

            var user =
                await _userManager.FindByIdAsync(
                    userId);

            if (user == null)
                return IdentityResult.Success;

            user.LockoutEnabled = true;
            user.LockoutEnd = null;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
                return updateResult;

            return await _userManager
                .UpdateSecurityStampAsync(user);
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

        private static bool IsProfileComplete(
            Patient patient)
        {
            return
                !string.IsNullOrWhiteSpace(patient.FirstName) &&
                !string.IsNullOrWhiteSpace(patient.LastName) &&
                !string.IsNullOrWhiteSpace(patient.PhoneNumber) &&
                patient.DateOfBirth != default;
        }
    }
}