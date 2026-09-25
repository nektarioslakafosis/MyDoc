using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Extensions;
using MyDoc.Models;
using MyDoc.Services;

namespace MyDoc.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppointmentLifecycleService _appointmentLifecycleService;
        private readonly AppointmentNotificationService _appointmentNotificationService;
        private readonly SchedulingService _schedulingService;
        private readonly NotificationService _notificationService;
        private readonly AppointmentDocumentStorageService _documentStorageService;

        public DoctorDashboardController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            AppointmentLifecycleService appointmentLifecycleService,
            AppointmentNotificationService appointmentNotificationService,
            SchedulingService schedulingService,
            NotificationService notificationService,
            AppointmentDocumentStorageService documentStorageService)
        {
            _context = context;
            _userManager = userManager;
            _appointmentLifecycleService = appointmentLifecycleService;
            _appointmentNotificationService = appointmentNotificationService;
            _schedulingService = schedulingService;
            _notificationService = notificationService;
            _documentStorageService = documentStorageService;
        }

        private async Task<Doctor?> GetCurrentDoctorAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return null;

            return await _context.Doctors
                .FirstOrDefaultAsync(d =>
                    d.UserId == user.Id &&
                    d.IsActive);
        }

        public async Task<IActionResult> Index()
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointments =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a =>
                        a.DoctorId == doctor.Id)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();

            var appointmentIds =
                appointments
                    .Select(a => a.Id)
                    .ToList();

            var pendingRescheduleAppointmentIds =
                await _context.AppointmentRescheduleRequests
                    .Where(x =>
                        appointmentIds.Contains(
                            x.AppointmentId) &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending)
                    .Select(x => x.AppointmentId)
                    .ToListAsync();

            ViewBag.PendingRescheduleAppointmentIds =
                pendingRescheduleAppointmentIds
                    .ToHashSet();

            return View(appointments);
        }

        public async Task<IActionResult> Details(int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            ViewBag.PendingRescheduleRequest =
                await _context.AppointmentRescheduleRequests
                    .Where(x =>
                        x.AppointmentId ==
                        appointment.Id &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            var unreadPatientMessages =
                await _context.AppointmentMessages
                    .Where(x =>
                        x.AppointmentId ==
                        appointment.Id &&
                        x.SenderRole ==
                        "Patient" &&
                        x.ReadAt == null)
                    .ToListAsync();

            foreach (var message in unreadPatientMessages)
            {
                message.ReadAt = DateTime.Now;
            }

            if (unreadPatientMessages.Any())
                await _context.SaveChangesAsync();

            ViewBag.AppointmentMessages =
                await _context.AppointmentMessages
                    .Where(x =>
                        x.AppointmentId ==
                        appointment.Id)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();

            ViewBag.CanSendAppointmentMessage =
                CanSendAppointmentMessage(
                    appointment);

            ViewBag.AppointmentIntake =
                await _context.AppointmentIntakes
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id);

            ViewBag.AppointmentFollowUpPlan =
                await _context.AppointmentFollowUpPlans
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id);

            ViewBag.AppointmentDocuments =
                await _context.AppointmentDocuments
                    .Where(x =>
                        x.AppointmentId ==
                        appointment.Id)
                    .OrderByDescending(x =>
                        x.UploadedAt)
                    .ToListAsync();

            ViewBag.AppointmentPrivateNote =
                await _context.AppointmentPrivateNotes
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id &&
                        x.DoctorId ==
                        doctor.Id);

            return View(appointment);
        }

        public async Task<IActionResult> Edit(int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            if (appointment.Status ==
                    AppointmentStatus.Approved &&
                _appointmentLifecycleService
                    .HasAppointmentExpired(
                        appointment))
            {
                TempData.FlashWarning(
                    "Το ραντεβού έχει ολοκληρώσει την προγραμματισμένη διάρκειά του. Κλείστε το ως ολοκληρωμένο ή ως μη προσέλευση.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (appointment.AppointmentDate <=
                DateTime.Now)
            {
                TempData.FlashWarning(
                    "Δεν επιτρέπεται αλλαγή κατάστασης αφού το ραντεβού έχει ξεκινήσει.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (!CanDoctorChangeStatus(
                    appointment.Status))
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            return View(
                BuildDoctorEditAppointmentVM(
                    appointment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            DoctorEditAppointmentVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == vm.Id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            if (appointment.AppointmentDate <=
                DateTime.Now)
            {
                TempData.FlashWarning(
                    appointment.Status ==
                        AppointmentStatus.Approved &&
                    _appointmentLifecycleService
                        .HasAppointmentExpired(
                            appointment)
                        ? "Το ραντεβού έχει ολοκληρώσει την προγραμματισμένη διάρκειά του. Κλείστε το από τη σελίδα λεπτομερειών."
                        : "Δεν επιτρέπεται αλλαγή κατάστασης αφού το ραντεβού έχει ξεκινήσει.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (!IsAllowedDoctorStatusTransition(
                    appointment.Status,
                    vm.Status))
            {
                var editVm =
                    BuildDoctorEditAppointmentVM(
                        appointment);

                if (!editVm.AvailableStatuses.Any())
                {
                    return RedirectToAction(
                        nameof(Details),
                        new { id = appointment.Id });
                }

                editVm.Status = vm.Status;

                ModelState.AddModelError(
                    "",
                    "Δεν επιτρέπεται αυτή η αλλαγή κατάστασης.");

                return View(editVm);
            }

            if (!ModelState.IsValid)
            {
                var editVm =
                    BuildDoctorEditAppointmentVM(
                        appointment);

                editVm.Status =
                    vm.Status;

                editVm.CancellationReason =
                    vm.CancellationReason;

                return View(editVm);
            }

            if (vm.Status ==
                    AppointmentStatus.Cancelled &&
                string.IsNullOrWhiteSpace(
                    vm.CancellationReason))
            {
                var editVm =
                    BuildDoctorEditAppointmentVM(
                        appointment);

                editVm.Status =
                    vm.Status;

                editVm.CancellationReason =
                    vm.CancellationReason;

                ModelState.AddModelError(
                    nameof(vm.CancellationReason),
                    "Συμπληρώστε τον λόγο ακύρωσης.");

                return View(editVm);
            }

            appointment.Status =
                vm.Status;

            var cancellationReason =
                string.Empty;

            if (appointment.Status ==
                AppointmentStatus.Cancelled)
            {
                cancellationReason =
                    vm.CancellationReason!.Trim();

                appointment.CancellationReason =
                    cancellationReason;

                appointment.CancelledAt =
                    DateTime.Now;

                appointment.CancelledByRole =
                    "Doctor";

                var pendingRescheduleRequests =
                    await _context
                        .AppointmentRescheduleRequests
                        .Where(x =>
                            x.AppointmentId ==
                            appointment.Id &&
                            x.Status ==
                            AppointmentRescheduleRequestStatus.Pending)
                        .ToListAsync();

                foreach (var request
                         in pendingRescheduleRequests)
                {
                    request.Status =
                        AppointmentRescheduleRequestStatus.Rejected;

                    request.ReviewedAt =
                        DateTime.Now;

                    request.ReviewedByUserId =
                        doctor.UserId;

                    request.ReviewComment =
                        "Το ραντεβού ακυρώθηκε από τον ιατρό πριν εξεταστεί το αίτημα αλλαγής.";
                }
            }

            await _context.SaveChangesAsync();

            if (appointment.Patient != null)
            {
                if (appointment.Status ==
                    AppointmentStatus.Approved)
                {
                    await _appointmentNotificationService
                        .NotifyPatientForAppointmentApprovedAsync(
                            doctor,
                            appointment.Patient,
                            appointment,
                            Url.Action(
                                "Details",
                                "Appointments",
                                new { id = appointment.Id }),
                            GetLoginUrl());

                    TempData.FlashSuccess(
                        "Το ραντεβού εγκρίθηκε.");
                }
                else if (appointment.Status ==
                         AppointmentStatus.Cancelled)
                {
                    await _appointmentNotificationService
                        .NotifyPatientForDoctorCancellationAsync(
                            doctor,
                            appointment.Patient,
                            appointment,
                            cancellationReason,
                            Url.Action(
                                "Details",
                                "Appointments",
                                new { id = appointment.Id }),
                            GetLoginUrl());

                    TempData.FlashSuccess(
                        "Το ραντεβού ακυρώθηκε.");
                }
            }

            return RedirectToAction(
                nameof(Index));
        }

        public async Task<IActionResult> Schedule()
        {
            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var availabilities =
                await _context.DoctorAvailabilities
                    .Where(a =>
                        a.DoctorId == doctor.Id &&
                        a.IsActive)
                    .OrderBy(a => a.DayOfWeek)
                    .ThenBy(a => a.StartTime)
                    .ToListAsync();

            var now =
                DateTime.Now;

            var allUnavailablePeriods =
                await _context.DoctorUnavailablePeriods
                    .Where(x =>
                        x.DoctorId == doctor.Id)
                    .ToListAsync();

            var activeOrFutureUnavailablePeriods =
                allUnavailablePeriods
                    .Where(x =>
                        x.Date.Date
                            .Add(
                                x.EndTime.ToTimeSpan()) >
                        now)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.StartTime)
                    .ToList();

            var pastUnavailablePeriods =
                allUnavailablePeriods
                    .Where(x =>
                        x.Date.Date
                            .Add(
                                x.EndTime.ToTimeSpan()) <=
                        now)
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.StartTime)
                    .ToList();

            var vm = new DoctorScheduleVM
            {
                Availabilities =
                    availabilities,

                UnavailablePeriods =
                    activeOrFutureUnavailablePeriods,

                PastUnavailablePeriods =
                    pastUnavailablePeriods
            };

            return View(vm);
        }

        public IActionResult CreateAvailability()
        {
            return View(
                new DoctorAvailabilityVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAvailability(
            DoctorAvailabilityVM vm)
        {
            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            if (vm.StartTime >= vm.EndTime)
            {
                ModelState.AddModelError(
                    "",
                    "Η ώρα έναρξης πρέπει να είναι νωρίτερα από την ώρα λήξης.");
            }

            var hasOverlap =
                await _context.DoctorAvailabilities
                    .AnyAsync(a =>
                        a.DoctorId == doctor.Id &&
                        a.DayOfWeek ==
                        vm.DayOfWeek &&
                        a.IsActive &&
                        vm.StartTime < a.EndTime &&
                        vm.EndTime > a.StartTime);

            if (hasOverlap)
            {
                ModelState.AddModelError(
                    "",
                    "Υπάρχει ήδη επικαλυπτόμενο ωράριο για αυτή τη μέρα.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var availability =
                new DoctorAvailability
                {
                    DoctorId =
                        doctor.Id,

                    DayOfWeek =
                        vm.DayOfWeek,

                    StartTime =
                        vm.StartTime,

                    EndTime =
                        vm.EndTime,

                    SlotDurationMinutes =
                        30,

                    IsActive =
                        true
                };

            _context.DoctorAvailabilities
                .Add(availability);

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Η διαθεσιμότητα προστέθηκε.");

            return RedirectToAction(
                nameof(Schedule));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(
            int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var availability =
                await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id &&
                        a.IsActive);

            if (availability == null)
                return NotFound();

            var now =
                DateTime.Now;

            var futureActiveAppointments =
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

            var affectedAppointments =
                futureActiveAppointments
                    .Where(a =>
                        a.AppointmentDate.DayOfWeek ==
                        availability.DayOfWeek)
                    .Where(a =>
                    {
                        var appointmentStart =
                            TimeOnly.FromDateTime(
                                a.AppointmentDate);

                        var appointmentEnd =
                            appointmentStart
                                .AddMinutes(30);

                        return appointmentStart <
                               availability.EndTime &&
                               appointmentEnd >
                               availability.StartTime;
                    })
                    .ToList();

            if (affectedAppointments.Any())
            {
                TempData.FlashWarning(
                    $"Δεν μπορείτε να αφαιρέσετε αυτή τη διαθεσιμότητα, γιατί υπάρχουν {affectedAppointments.Count} μελλοντικά ενεργά ραντεβού σε αυτό το ωράριο. Αν δεν μπορείτε να είστε διαθέσιμος σε συγκεκριμένη ημερομηνία, χρησιμοποιήστε τη λειτουργία Νέα Απουσία.");

                return RedirectToAction(
                    nameof(Schedule));
            }

            availability.IsActive =
                false;

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Η διαθεσιμότητα αφαιρέθηκε από το ενεργό εβδομαδιαίο πρόγραμμα.");

            return RedirectToAction(
                nameof(Schedule));
        }

        public IActionResult CreateUnavailablePeriod()
        {
            return View(
                new DoctorUnavailablePeriodVM
                {
                    Date = DateTime.Today
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUnavailablePeriod(
            DoctorUnavailablePeriodVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var date =
                vm.Date.Date;

            var unavailableStart =
                date.Add(
                    vm.StartTime.ToTimeSpan());

            var unavailableEnd =
                date.Add(
                    vm.EndTime.ToTimeSpan());

            if (date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(vm.Date),
                    "Δεν μπορείτε να δηλώσετε απουσία σε προηγούμενη ημερομηνία.");
            }

            if (vm.StartTime >= vm.EndTime)
            {
                ModelState.AddModelError(
                    "",
                    "Η ώρα έναρξης πρέπει να είναι νωρίτερα από την ώρα λήξης.");
            }

            if (unavailableEnd <= DateTime.Now)
            {
                ModelState.AddModelError(
                    "",
                    "Δεν μπορείτε να δηλώσετε απουσία για ώρα που έχει ήδη περάσει.");
            }

            var hasOverlap =
                await _context.DoctorUnavailablePeriods
                    .AnyAsync(x =>
                        x.DoctorId == doctor.Id &&
                        x.Date == date &&
                        vm.StartTime < x.EndTime &&
                        vm.EndTime > x.StartTime);

            if (hasOverlap)
            {
                ModelState.AddModelError(
                    "",
                    "Υπάρχει ήδη δηλωμένη απουσία που επικαλύπτεται με αυτό το διάστημα.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var unavailablePeriod =
                new DoctorUnavailablePeriod
                {
                    DoctorId =
                        doctor.Id,

                    Date =
                        date,

                    StartTime =
                        vm.StartTime,

                    EndTime =
                        vm.EndTime,

                    Reason =
                        vm.Reason
                };

            _context.DoctorUnavailablePeriods
                .Add(unavailablePeriod);

            var dayStart =
                date;

            var dayEnd =
                date.AddDays(1);

            var candidateAppointments =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a =>
                        a.DoctorId == doctor.Id &&
                        a.AppointmentDate >= dayStart &&
                        a.AppointmentDate < dayEnd &&
                        (
                            a.Status ==
                            AppointmentStatus.Pending ||
                            a.Status ==
                            AppointmentStatus.Approved
                        ))
                    .ToListAsync();

            var affectedAppointments =
                candidateAppointments
                    .Where(a =>
                        a.AppointmentDate <
                        unavailableEnd &&
                        a.AppointmentDate
                            .AddMinutes(30) >
                        unavailableStart)
                    .ToList();

            var unavailableCancellationReason =
                string.IsNullOrWhiteSpace(vm.Reason)
                    ? "Αλλαγή διαθεσιμότητας ιατρού."
                    : vm.Reason.Trim();

            foreach (var appointment
                     in affectedAppointments)
            {
                appointment.Status =
                    AppointmentStatus.Cancelled;

                appointment.CancellationReason =
                    unavailableCancellationReason;

                appointment.CancelledAt =
                    DateTime.Now;

                appointment.CancelledByRole =
                    "Doctor";
            }

            var affectedAppointmentIds =
                affectedAppointments
                    .Select(a => a.Id)
                    .ToList();

            if (affectedAppointmentIds.Any())
            {
                var pendingRescheduleRequests =
                    await _context
                        .AppointmentRescheduleRequests
                        .Where(x =>
                            affectedAppointmentIds.Contains(
                                x.AppointmentId) &&
                            x.Status ==
                            AppointmentRescheduleRequestStatus.Pending)
                        .ToListAsync();

                foreach (var request
                         in pendingRescheduleRequests)
                {
                    request.Status =
                        AppointmentRescheduleRequestStatus.Rejected;

                    request.ReviewedAt =
                        DateTime.Now;

                    request.ReviewedByUserId =
                        doctor.UserId;

                    request.ReviewComment =
                        "Το ραντεβού ακυρώθηκε λόγω δηλωμένης απουσίας του ιατρού πριν εξεταστεί το αίτημα αλλαγής.";
                }
            }

            await _context.SaveChangesAsync();

            foreach (var appointment
                     in affectedAppointments)
            {
                if (appointment.Patient != null)
                {
                    await _appointmentNotificationService
                        .NotifyPatientForUnavailablePeriodCancellationAsync(
                            doctor,
                            appointment.Patient,
                            appointment,
                            unavailableCancellationReason,
                            Url.Action(
                                "Details",
                                "Appointments",
                                new { id = appointment.Id }),
                            GetLoginUrl());
                }
            }

            if (affectedAppointments.Any())
            {
                TempData.FlashWarning(
                    $"Η απουσία καταχωρήθηκε και ακυρώθηκαν {affectedAppointments.Count} ραντεβού.");
            }
            else
            {
                TempData.FlashSuccess(
                    "Η απουσία καταχωρήθηκε.");
            }

            return RedirectToAction(
                nameof(Schedule));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnavailablePeriod(
            int id)
        {
            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var unavailablePeriod =
                await _context.DoctorUnavailablePeriods
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.DoctorId == doctor.Id);

            if (unavailablePeriod == null)
                return NotFound();

            _context.DoctorUnavailablePeriods
                .Remove(unavailablePeriod);

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Η απουσία διαγράφηκε.");

            return RedirectToAction(
                nameof(Schedule));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRescheduleRequest(
            int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var request =
                await _context.AppointmentRescheduleRequests
                    .Include(x => x.Appointment)
                        .ThenInclude(a => a.Patient)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.Appointment.DoctorId ==
                        doctor.Id);

            if (request == null)
                return NotFound();

            var appointment =
                request.Appointment;

            
            if (request.Status !=
                AppointmentRescheduleRequestStatus.Pending)
            {
                TempData.FlashWarning(
                    "Το αίτημα αλλαγής δεν είναι πλέον ενεργό.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            
            if (_appointmentLifecycleService
                .HasRescheduleRequestExpired(
                    appointment,
                    request))
            {
                ExpireRescheduleRequest(
                    request,
                    "Το αίτημα έληξε επειδή έφτασε το χρονικό όριο αλλαγής.");

                await _context.SaveChangesAsync();

                if (appointment.Patient != null)
                {
                    await _appointmentNotificationService
                        .NotifyPatientForRescheduleRequestExpiredAsync(
                            doctor,
                            appointment.Patient,
                            appointment,
                            request,
                            Url.Action(
                                "Details",
                                "Appointments",
                                new { id = appointment.Id }),
                            GetLoginUrl());
                }

                TempData.FlashWarning(
                    "Το αίτημα αλλαγής έχει λήξει.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (appointment.Status !=
                AppointmentStatus.Approved)
            {
                ExpireRescheduleRequest(
                    request,
                    "Το αίτημα έληξε επειδή το αρχικό ραντεβού δεν μπορεί πλέον να αλλάξει.");

                await _context.SaveChangesAsync();

                TempData.FlashWarning(
                    "Το αίτημα αλλαγής δεν μπορεί πλέον να εγκριθεί.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var availableSlots =
                await _schedulingService
                    .GetAvailableSlotsAsync(
                        appointment.DoctorId,
                        request.RequestedAppointmentDate.Date);

            if (!availableSlots.Contains(
                    request.RequestedAppointmentDate
                        .TimeOfDay))
            {
                ExpireRescheduleRequest(
                    request,
                    "Το αίτημα έληξε επειδή η ζητούμενη ώρα δεν είναι πλέον διαθέσιμη.");

                await _context.SaveChangesAsync();

                if (appointment.Patient != null)
                {
                    await _appointmentNotificationService
                        .NotifyPatientForRescheduleRequestUnavailableAsync(
                            doctor,
                            appointment.Patient,
                            appointment,
                            request,
                            Url.Action(
                                "Details",
                                "Appointments",
                                new { id = appointment.Id }),
                            GetLoginUrl());
                }

                TempData.FlashWarning(
                    "Η ζητούμενη ώρα δεν είναι πλέον διαθέσιμη. Το ραντεβού δεν άλλαξε.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var appointmentId =
                appointment.Id;

            var existingReminderLogs =
                await _context.AppointmentReminderLogs
                    .Where(x =>
                        x.AppointmentId ==
                        appointment.Id)
                    .ToListAsync();

            appointment.AppointmentDate =
                request.RequestedAppointmentDate;

            request.Status =
                AppointmentRescheduleRequestStatus.Approved;

            request.ReviewedAt =
                DateTime.Now;

            request.ReviewedByUserId =
                doctor.UserId;

            request.ReviewComment =
                null;

            try
            {
                
                await _context.SaveChangesAsync();

                if (existingReminderLogs.Any())
                {
                    _context.AppointmentReminderLogs
                        .RemoveRange(
                            existingReminderLogs);

                    await _context.SaveChangesAsync();
                }

                if (appointment.Patient != null)
                {
                    await _appointmentNotificationService
                        .NotifyPatientForRescheduleRequestApprovedAsync(
                            doctor,
                            appointment.Patient,
                            appointment,
                            request,
                            Url.Action(
                                "Details",
                                "Appointments",
                                new { id = appointment.Id }),
                            GetLoginUrl());
                }

                TempData.FlashSuccess(
                    "Το αίτημα αλλαγής εγκρίθηκε και το ραντεβού ενημερώθηκε.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }
            catch (DbUpdateException ex)
                when (IsUniqueConstraintViolation(ex))
            {
                
                _context.ChangeTracker.Clear();

                var conflictedRequest =
                    await _context
                        .AppointmentRescheduleRequests
                        .Include(x => x.Appointment)
                            .ThenInclude(a => a.Patient)
                        .FirstOrDefaultAsync(x =>
                            x.Id == id);

                if (conflictedRequest != null &&
                    conflictedRequest.Status ==
                    AppointmentRescheduleRequestStatus.Pending)
                {
                    ExpireRescheduleRequest(
                        conflictedRequest,
                        "Το αίτημα έληξε επειδή η ζητούμενη ώρα έκλεισε από άλλο ραντεβού.");

                    await _context.SaveChangesAsync();

                    if (conflictedRequest.Appointment.Patient != null)
                    {
                        await _appointmentNotificationService
                            .NotifyPatientForRescheduleRequestUnavailableAsync(
                                doctor,
                                conflictedRequest.Appointment.Patient,
                                conflictedRequest.Appointment,
                                conflictedRequest,
                                Url.Action(
                                    "Details",
                                    "Appointments",
                                    new
                                    {
                                        id =
                                            conflictedRequest.AppointmentId
                                    }),
                                GetLoginUrl());
                    }
                }

                TempData.FlashWarning(
                    "Η ζητούμενη ώρα δεν είναι πλέον διαθέσιμη. Το ραντεβού δεν άλλαξε.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointmentId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRescheduleRequest(
            AppointmentRescheduleRejectVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var request =
                await _context.AppointmentRescheduleRequests
                    .Include(x => x.Appointment)
                        .ThenInclude(a => a.Patient)
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                        vm.RescheduleRequestId &&
                        x.Appointment.DoctorId ==
                        doctor.Id);

            if (request == null)
                return NotFound();

            var appointment =
                request.Appointment;

            
            if (request.Status !=
                AppointmentRescheduleRequestStatus.Pending)
            {
                TempData.FlashWarning(
                    "Το αίτημα αλλαγής δεν είναι πλέον ενεργό.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            
            if (_appointmentLifecycleService
                .HasRescheduleRequestExpired(
                    appointment,
                    request))
            {
                ExpireRescheduleRequest(
                    request,
                    "Το αίτημα έληξε επειδή έφτασε το χρονικό όριο αλλαγής.");

                await _context.SaveChangesAsync();

                if (appointment.Patient != null)
                {
                    await _appointmentNotificationService
                        .NotifyPatientForRescheduleRequestExpiredAsync(
                            doctor,
                            appointment.Patient,
                            appointment,
                            request,
                            Url.Action(
                                "Details",
                                "Appointments",
                                new { id = appointment.Id }),
                            GetLoginUrl());
                }

                TempData.FlashWarning(
                    "Το αίτημα αλλαγής έχει ήδη λήξει.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (string.IsNullOrWhiteSpace(
                    vm.RejectionReason))
            {
                TempData.FlashWarning(
                    "Συμπληρώστε τον λόγο απόρριψης.");

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = appointment.Id,
                        activeTab = "overview"
                    });
            }

            var rejectionReason =
                vm.RejectionReason.Trim();

            request.Status =
                AppointmentRescheduleRequestStatus.Rejected;

            request.ReviewedAt =
                DateTime.Now;

            request.ReviewedByUserId =
                doctor.UserId;

            request.ReviewComment =
                rejectionReason;

            await _context.SaveChangesAsync();

            if (appointment.Patient != null)
            {
                await _appointmentNotificationService
                    .NotifyPatientForRescheduleRequestRejectedAsync(
                        doctor,
                        appointment.Patient,
                        appointment,
                        request,
                        rejectionReason,
                        Url.Action(
                            "Details",
                            "Appointments",
                            new { id = appointment.Id }),
                        GetLoginUrl());
            }

            TempData.FlashSuccess(
                "Το αίτημα αλλαγής απορρίφθηκε.");

            return RedirectToAction(
                nameof(Details),
                new { id = appointment.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(
            int id,
            string? outcomeNote)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            if (appointment.Status !=
                AppointmentStatus.Approved)
            {
                TempData.FlashWarning(
                    "Μόνο εγκεκριμένα ραντεβού μπορούν να σημειωθούν ως ολοκληρωμένα.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (!_appointmentLifecycleService
                    .HasAppointmentExpired(
                        appointment))
            {
                TempData.FlashWarning(
                    "Το ραντεβού δεν έχει ολοκληρώσει ακόμα την προγραμματισμένη διάρκειά του.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var hasPendingRescheduleRequest =
                await _context
                    .AppointmentRescheduleRequests
                    .AnyAsync(x =>
                        x.AppointmentId ==
                        appointment.Id &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending);

            if (hasPendingRescheduleRequest)
            {
                TempData.FlashWarning(
                    "Υπάρχει αίτημα αλλαγής σε αναμονή. Εξετάστε πρώτα το αίτημα αλλαγής.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            appointment.Status =
                AppointmentStatus.Completed;

            appointment.ClosedAt =
                DateTime.Now;

            appointment.ClosedByRole =
                "Doctor";

            appointment.DoctorOutcomeNote =
                string.IsNullOrWhiteSpace(
                    outcomeNote)
                    ? null
                    : outcomeNote.Trim();

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Το ραντεβού σημειώθηκε ως ολοκληρωμένο.");

            return RedirectToAction(
                nameof(Details),
                new { id = appointment.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNoShow(
            int id,
            string? outcomeNote)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            if (appointment.Status !=
                AppointmentStatus.Approved)
            {
                TempData.FlashWarning(
                    "Μόνο εγκεκριμένα ραντεβού μπορούν να σημειωθούν ως μη προσέλευση.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (!_appointmentLifecycleService
                    .HasAppointmentExpired(
                        appointment))
            {
                TempData.FlashWarning(
                    "Το ραντεβού δεν έχει ολοκληρώσει ακόμα την προγραμματισμένη διάρκειά του.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var hasPendingRescheduleRequest =
                await _context
                    .AppointmentRescheduleRequests
                    .AnyAsync(x =>
                        x.AppointmentId ==
                        appointment.Id &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending);

            if (hasPendingRescheduleRequest)
            {
                TempData.FlashWarning(
                    "Υπάρχει αίτημα αλλαγής σε αναμονή. Εξετάστε πρώτα το αίτημα αλλαγής.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            appointment.Status =
                AppointmentStatus.NoShow;

            appointment.ClosedAt =
                DateTime.Now;

            appointment.ClosedByRole =
                "Doctor";

            appointment.DoctorOutcomeNote =
                string.IsNullOrWhiteSpace(
                    outcomeNote)
                    ? null
                    : outcomeNote.Trim();

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Το ραντεβού σημειώθηκε ως μη προσέλευση.");

            return RedirectToAction(
                nameof(Details),
                new { id = appointment.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            int id,
            string message)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            if (appointment.Patient == null)
            {
                const string error =
                    "Δεν βρέθηκε ασθενής για αυτό το ραντεβού.";

                if (IsAjaxRequest(Request))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = error
                    });
                }

                TempData.FlashWarning(error);

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = appointment.Id,
                        activeTab = "messages"
                    });
            }

            if (!CanSendAppointmentMessage(
                    appointment))
            {
                const string error =
                    "Τα μηνύματα για αυτό το ραντεβού είναι πλέον μόνο για προβολή.";

                if (IsAjaxRequest(Request))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = error
                    });
                }

                TempData.FlashWarning(error);

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = appointment.Id,
                        activeTab = "messages"
                    });
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                const string error =
                    "Το μήνυμα δεν μπορεί να είναι κενό.";

                if (IsAjaxRequest(Request))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = error
                    });
                }

                TempData.FlashWarning(error);

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = appointment.Id,
                        activeTab = "messages"
                    });
            }

            var trimmedMessage =
                message.Trim();

            if (trimmedMessage.Length > 1000)
            {
                const string error =
                    "Το μήνυμα δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.";

                if (IsAjaxRequest(Request))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = error
                    });
                }

                TempData.FlashWarning(error);

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = appointment.Id,
                        activeTab = "messages"
                    });
            }

            var appointmentMessage =
                new AppointmentMessage
                {
                    AppointmentId =
                        appointment.Id,

                    SenderUserId =
                        user.Id,

                    SenderRole =
                        "Doctor",

                    Message =
                        trimmedMessage,

                    CreatedAt =
                        DateTime.Now
                };

            _context.AppointmentMessages
                .Add(appointmentMessage);

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                appointment.Patient.UserId,
                "Νέο μήνυμα από τον ιατρό",
                $"Ο/Η ιατρός {doctor.FirstName} {doctor.LastName} έστειλε μήνυμα για το ραντεβού στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",
                Url.Action(
                    "Details",
                    "Appointments",
                    new { id = appointment.Id }));

            if (IsAjaxRequest(Request))
            {
                return Json(new
                {
                    success = true,
                    message = appointmentMessage.Message,
                    createdAt =
                        appointmentMessage.CreatedAt
                            .ToString("dd/MM/yyyy HH:mm")
                });
            }

            TempData.FlashSuccess(
                "Το μήνυμα στάλθηκε.");

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = appointment.Id,
                    activeTab = "messages"
                });
        }

        public async Task<IActionResult> FollowUpPlan(
            int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            if (appointment.Status !=
                AppointmentStatus.Completed)
            {
                TempData.FlashWarning(
                    "Μπορείτε να προσθέσετε οδηγίες μετά την επίσκεψη μόνο σε ολοκληρωμένο ραντεβού.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var followUpPlan =
                await _context
                    .AppointmentFollowUpPlans
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id);

            var vm =
                new AppointmentFollowUpPlanVM
                {
                    AppointmentId =
                        appointment.Id,

                    AppointmentDate =
                        appointment.AppointmentDate,

                    PatientName =
                        appointment.Patient != null
                            ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}"
                            : "-",

                    Instructions =
                        followUpPlan?.Instructions ??
                        string.Empty,

                    RecommendedTests =
                        followUpPlan?.RecommendedTests,

                    MedicationNotes =
                        followUpPlan?.MedicationNotes,

                    WarningSigns =
                        followUpPlan?.WarningSigns,

                    FollowUpRecommendation =
                        followUpPlan?
                            .FollowUpRecommendation,

                    SuggestedFollowUpDate =
                        followUpPlan?
                            .SuggestedFollowUpDate,

                    ExtraNotes =
                        followUpPlan?.ExtraNotes
                };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FollowUpPlan(
            AppointmentFollowUpPlanVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == vm.AppointmentId &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            vm.AppointmentDate =
                appointment.AppointmentDate;

            vm.PatientName =
                appointment.Patient != null
                    ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}"
                    : "-";

            if (appointment.Status !=
                AppointmentStatus.Completed)
            {
                TempData.FlashWarning(
                    "Μπορείτε να προσθέσετε οδηγίες μετά την επίσκεψη μόνο σε ολοκληρωμένο ραντεβού.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (vm.SuggestedFollowUpDate.HasValue &&
                vm.SuggestedFollowUpDate.Value.Date <=
                DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(vm.SuggestedFollowUpDate),
                    "Η προτεινόμενη ημερομηνία επανεξέτασης πρέπει να είναι μελλοντική.");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var followUpPlan =
                await _context
                    .AppointmentFollowUpPlans
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id);

            var isNew =
                followUpPlan == null;

            if (followUpPlan == null)
            {
                followUpPlan =
                    new AppointmentFollowUpPlan
                    {
                        AppointmentId =
                            appointment.Id,

                        CreatedAt =
                            DateTime.Now
                    };

                _context.AppointmentFollowUpPlans
                    .Add(followUpPlan);
            }
            else
            {
                followUpPlan.UpdatedAt =
                    DateTime.Now;
            }

            followUpPlan.Instructions =
                vm.Instructions.Trim();

            followUpPlan.RecommendedTests =
                NormalizeNullableText(
                    vm.RecommendedTests);

            followUpPlan.MedicationNotes =
                NormalizeNullableText(
                    vm.MedicationNotes);

            followUpPlan.WarningSigns =
                NormalizeNullableText(
                    vm.WarningSigns);

            followUpPlan.FollowUpRecommendation =
                NormalizeNullableText(
                    vm.FollowUpRecommendation);

            followUpPlan.SuggestedFollowUpDate =
                vm.SuggestedFollowUpDate;

            followUpPlan.ExtraNotes =
                NormalizeNullableText(
                    vm.ExtraNotes);

            await _context.SaveChangesAsync();

            if (appointment.Patient != null)
            {
                await _notificationService.CreateAsync(
                    appointment.Patient.UserId,

                    isNew
                        ? "Νέες οδηγίες μετά το ραντεβού"
                        : "Ενημερώθηκαν οι οδηγίες μετά το ραντεβού",

                    $"Ο/Η ιατρός {doctor.FirstName} {doctor.LastName} {(isNew ? "πρόσθεσε" : "ενημέρωσε")} οδηγίες για το ολοκληρωμένο ραντεβού στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",

                    Url.Action(
                        "Details",
                        "Appointments",
                        new { id = appointment.Id }));
            }

            TempData.FlashSuccess(
                isNew
                    ? "Οι οδηγίες μετά το ραντεβού αποθηκεύτηκαν."
                    : "Οι οδηγίες μετά το ραντεβού ενημερώθηκαν.");

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = appointment.Id,
                    activeTab = "followup"
                });
        }

        public async Task<IActionResult> DownloadDocument(
            int id)
        {
            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var document =
                await _context.AppointmentDocuments
                    .Include(x => x.Appointment)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.Appointment.DoctorId ==
                        doctor.Id);

            if (document == null)
                return NotFound();

            var filePath =
                _documentStorageService
                    .GetFilePath(
                        document.StoredFileName);

            if (!System.IO.File.Exists(filePath))
            {
                TempData.FlashDanger(
                    "Το αρχείο δεν βρέθηκε.");

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id =
                            document.AppointmentId
                    });
            }

            return PhysicalFile(
                filePath,
                document.ContentType,
                document.OriginalFileName);
        }

        public async Task<IActionResult> PrivateNote(
            int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            var privateNote =
                await _context.AppointmentPrivateNotes
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id &&
                        x.DoctorId ==
                        doctor.Id);

            var vm =
                new AppointmentPrivateNoteVM
                {
                    AppointmentId =
                        appointment.Id,

                    AppointmentDate =
                        appointment.AppointmentDate,

                    AppointmentStatus =
                        appointment.Status,

                    PatientName =
                        appointment.Patient != null
                            ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}"
                            : "-",

                    Notes =
                        privateNote?.Notes ??
                        string.Empty
                };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrivateNote(
            AppointmentPrivateNoteVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var doctor =
                await GetCurrentDoctorAsync();

            if (doctor == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == vm.AppointmentId &&
                        a.DoctorId == doctor.Id);

            if (appointment == null)
                return NotFound();

            vm.AppointmentDate =
                appointment.AppointmentDate;

            vm.AppointmentStatus =
                appointment.Status;

            vm.PatientName =
                appointment.Patient != null
                    ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}"
                    : "-";

            if (!ModelState.IsValid)
                return View(vm);

            var privateNote =
                await _context.AppointmentPrivateNotes
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id &&
                        x.DoctorId ==
                        doctor.Id);

            var isNew =
                privateNote == null;

            if (privateNote == null)
            {
                privateNote =
                    new AppointmentPrivateNote
                    {
                        AppointmentId =
                            appointment.Id,

                        DoctorId =
                            doctor.Id,

                        CreatedAt =
                            DateTime.Now
                    };

                _context.AppointmentPrivateNotes
                    .Add(privateNote);
            }
            else
            {
                privateNote.UpdatedAt =
                    DateTime.Now;
            }

            privateNote.Notes =
                vm.Notes.Trim();

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                isNew
                    ? "Η προσωπική σημείωση αποθηκεύτηκε."
                    : "Η προσωπική σημείωση ενημερώθηκε.");

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = appointment.Id,
                    activeTab = "notes"
                });
        }

        // PRIVATE METHODS

        private DoctorEditAppointmentVM
            BuildDoctorEditAppointmentVM(
                Appointment appointment)
        {
            var defaultStatus =
                appointment.Status switch
                {
                    AppointmentStatus.Pending =>
                        AppointmentStatus.Approved,

                    AppointmentStatus.Approved =>
                        AppointmentStatus.Cancelled,

                    _ =>
                        appointment.Status
                };

            return new DoctorEditAppointmentVM
            {
                Id =
                    appointment.Id,

                CurrentStatus =
                    appointment.Status,

                Status =
                    defaultStatus,

                AvailableStatuses =
                    GetAllowedStatusOptions(
                        appointment.Status)
            };
        }

        private List<SelectListItem>
            GetAllowedStatusOptions(
                AppointmentStatus currentStatus)
        {
            var allowedStatuses =
                new List<AppointmentStatus>();

            if (currentStatus ==
                AppointmentStatus.Pending)
            {
                allowedStatuses.Add(
                    AppointmentStatus.Approved);

                allowedStatuses.Add(
                    AppointmentStatus.Cancelled);
            }
            else if (currentStatus ==
                     AppointmentStatus.Approved)
            {
                allowedStatuses.Add(
                    AppointmentStatus.Cancelled);
            }

            return allowedStatuses
                .Select(status =>
                    new SelectListItem
                    {
                        Value =
                            ((int)status).ToString(),

                        Text =
                            GetStatusText(status)
                    })
                .ToList();
        }

        private bool CanDoctorChangeStatus(
            AppointmentStatus status)
        {
            return status ==
                       AppointmentStatus.Pending ||
                   status ==
                       AppointmentStatus.Approved;
        }

        private bool IsAllowedDoctorStatusTransition(
            AppointmentStatus currentStatus,
            AppointmentStatus requestedStatus)
        {
            if (currentStatus ==
                AppointmentStatus.Pending)
            {
                return requestedStatus ==
                           AppointmentStatus.Approved ||
                       requestedStatus ==
                           AppointmentStatus.Cancelled;
            }

            if (currentStatus ==
                AppointmentStatus.Approved)
            {
                return requestedStatus ==
                       AppointmentStatus.Cancelled;
            }

            return false;
        }

        private string GetStatusText(
            AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Pending =>
                    "Σε αναμονή",

                AppointmentStatus.Approved =>
                    "Εγκεκριμένο",

                AppointmentStatus.Cancelled =>
                    "Ακυρωμένο",

                AppointmentStatus.Expired =>
                    "Έληξε",

                AppointmentStatus.Completed =>
                    "Ολοκληρωμένο",

                AppointmentStatus.NoShow =>
                    "Μη προσέλευση",

                _ =>
                    status.ToString()
            };
        }

        private string? GetLoginUrl()
        {
            return Url.Page(
                "/Account/Login",
                pageHandler: null,
                values: new { area = "Identity" },
                protocol: Request.Scheme);
        }

        private static bool IsUniqueConstraintViolation(
            DbUpdateException ex)
        {
            return ex.InnerException
                       is Microsoft.Data.SqlClient
                           .SqlException sqlException &&
                   (
                       sqlException.Number == 2601 ||
                       sqlException.Number == 2627
                   );
        }

        private static bool CanSendAppointmentMessage(
            Appointment appointment)
        {
            return appointment.Status ==
                       AppointmentStatus.Pending ||
                   appointment.Status ==
                       AppointmentStatus.Approved;
        }

        private static string? NormalizeNullableText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool IsAjaxRequest(
            HttpRequest request)
        {
            return request.Headers["X-Requested-With"] ==
                   "XMLHttpRequest";
        }

        private static void ExpireRescheduleRequest(
            AppointmentRescheduleRequest request,
            string reason)
        {
            request.Status =
                AppointmentRescheduleRequestStatus.Expired;

            request.ReviewedAt =
                DateTime.Now;

            request.ReviewedByUserId =
                null;

            request.ReviewComment =
                reason;
        }

    }
}