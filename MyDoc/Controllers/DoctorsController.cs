using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Extensions;
using MyDoc.Models;

namespace MyDoc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DoctorsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .OrderByDescending(d => d.IsActive)
                .ThenBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();

            return View(doctors);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor =
                await _context.Doctors
                    .FirstOrDefaultAsync(d =>
                        d.Id == id);

            if (doctor == null)
                return NotFound();

            var now = DateTime.Now;

            ViewBag.TotalAppointmentsCount =
                await _context.Appointments
                    .CountAsync(a =>
                        a.DoctorId == doctor.Id);

            ViewBag.UpcomingAppointmentsCount =
                await _context.Appointments
                    .CountAsync(a =>
                        a.DoctorId == doctor.Id &&
                        a.AppointmentDate >= now &&
                        (
                            a.Status ==
                            AppointmentStatus.Pending ||
                            a.Status ==
                            AppointmentStatus.Approved
                        ));

            return View(doctor);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    Doctor doctor,
    string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Συμπληρώστε κωδικό πρόσβασης.");
            }

            if (!ModelState.IsValid)
                return View(doctor);

            doctor.FirstName =
                doctor.FirstName.Trim();

            doctor.LastName =
                doctor.LastName.Trim();

            doctor.Specialty =
                doctor.Specialty.Trim();

            doctor.Email =
                doctor.Email.Trim();

            doctor.PhoneNumber =
                doctor.PhoneNumber.Trim();

            var existingUser =
                await _userManager.FindByEmailAsync(
                    doctor.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(Doctor.Email),
                    "Υπάρχει ήδη λογαριασμός με αυτό το email.");

                return View(doctor);
            }

            var user = new IdentityUser
            {
                UserName = doctor.Email,
                Email = doctor.Email,
                EmailConfirmed = true,
                PhoneNumber = doctor.PhoneNumber,
                LockoutEnabled = true
            };

            var createUserResult =
                await _userManager.CreateAsync(
                    user,
                    password);

            if (!createUserResult.Succeeded)
            {
                AddIdentityErrors(
                    createUserResult);

                return View(doctor);
            }

            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Doctor");

            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                AddIdentityErrors(
                    addRoleResult);

                return View(doctor);
            }

            doctor.UserId = user.Id;
            doctor.IsActive = true;

            _context.Doctors.Add(doctor);

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Ο ιατρός δημιουργήθηκε.");

            return RedirectToAction(
                nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor =
                await _context.Doctors.FindAsync(id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    [Bind("Id,FirstName,LastName,Specialty,Email,PhoneNumber")]
    Doctor input)
        {
            if (id != input.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(input);

            input.FirstName =
                input.FirstName.Trim();

            input.LastName =
                input.LastName.Trim();

            input.Specialty =
                input.Specialty.Trim();

            input.Email =
                input.Email.Trim();

            input.PhoneNumber =
                input.PhoneNumber.Trim();

            var doctor =
                await _context.Doctors
                    .FirstOrDefaultAsync(d =>
                        d.Id == id);

            if (doctor == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(
                    doctor.UserId))
            {
                ModelState.AddModelError(
                    "",
                    "Ο ιατρός δεν είναι συνδεδεμένος με λογαριασμό εισόδου.");

                return View(input);
            }

            var user =
                await _userManager.FindByIdAsync(
                    doctor.UserId);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Ο λογαριασμός σύνδεσης του ιατρού δεν βρέθηκε.");

                return View(input);
            }

            var emailChanged =
                !string.Equals(
                    doctor.Email,
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
                        nameof(Doctor.Email),
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

            doctor.FirstName =
                input.FirstName;

            doctor.LastName =
                input.LastName;

            doctor.Specialty =
                input.Specialty;

            doctor.Email =
                input.Email;

            doctor.PhoneNumber =
                input.PhoneNumber;

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Τα στοιχεία του ιατρού ενημερώθηκαν.");

            return RedirectToAction(
                nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor =
                await _context.Doctors
                    .FirstOrDefaultAsync(d =>
                        d.Id == id);

            if (doctor == null)
                return NotFound();

            ViewBag.FutureActiveAppointmentsCount =
                await _context.Appointments
                    .CountAsync(a =>
                        a.DoctorId == doctor.Id &&
                        a.AppointmentDate >= DateTime.Now &&
                        (
                            a.Status ==
                            AppointmentStatus.Pending ||
                            a.Status ==
                            AppointmentStatus.Approved
                        ));

            return View(doctor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(
    int id)
        {
            var doctor =
                await _context.Doctors
                    .FirstOrDefaultAsync(d =>
                        d.Id == id);

            if (doctor == null)
                return RedirectToAction(nameof(Index));

            if (!doctor.IsActive)
                return RedirectToAction(nameof(Index));

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            var now = DateTime.Now;

            var adminUserId =
                _userManager.GetUserId(User);

            doctor.IsActive = false;

            var futureAppointments =
                await _context.Appointments
                    .Where(a =>
                        a.DoctorId == doctor.Id &&
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
                    "Το ραντεβού ακυρώθηκε λόγω απενεργοποίησης του λογαριασμού του ιατρού.";

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
                        "Το ραντεβού ακυρώθηκε λόγω απενεργοποίησης του λογαριασμού του ιατρού.";
                }
            }

            var lockUserResult =
                await LockUserAccountAsync(
                    doctor.UserId);

            if (!lockUserResult.Succeeded)
            {
                await transaction.RollbackAsync();

                AddIdentityErrors(
                    lockUserResult);

                return View(
                    "Delete",
                    doctor);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            if (futureAppointments.Any())
            {
                TempData.FlashWarning(
                    $"Ο ιατρός απενεργοποιήθηκε και ακυρώθηκαν {futureAppointments.Count} μελλοντικά ραντεβού.");
            }
            else
            {
                TempData.FlashSuccess(
                    "Ο ιατρός απενεργοποιήθηκε.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var doctor =
                await _context.Doctors
                    .FirstOrDefaultAsync(d =>
                        d.Id == id);

            if (doctor == null)
                return NotFound();

            doctor.IsActive = true;

            var unlockUserResult =
                await UnlockUserAccountAsync(
                    doctor.UserId);

            if (!unlockUserResult.Succeeded)
            {
                AddIdentityErrors(
                    unlockUserResult);

                return RedirectToAction(
                    nameof(Index));
            }

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Ο γιατρός ενεργοποιήθηκε ξανά.");

            return RedirectToAction(nameof(Index));
        }

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
    }
}