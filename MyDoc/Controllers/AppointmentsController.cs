using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using MyDoc.Data;
using MyDoc.Extensions;
using MyDoc.Models;
using MyDoc.Services;

namespace MyDoc.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SchedulingService _schedulingService;
        private readonly AppointmentLifecycleService _appointmentLifecycleService;
        private readonly AppointmentNotificationService _appointmentNotificationService;
        private readonly NotificationService _notificationService;
        private readonly AppointmentDocumentStorageService _documentStorageService;

        public AppointmentsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SchedulingService schedulingService,
            AppointmentLifecycleService appointmentLifecycleService,
            AppointmentNotificationService appointmentNotificationService,
            NotificationService notificationService,
            AppointmentDocumentStorageService documentStorageService)
        {
            _context = context;
            _userManager = userManager;
            _schedulingService = schedulingService;
            _appointmentLifecycleService = appointmentLifecycleService;
            _appointmentNotificationService = appointmentNotificationService;
            _notificationService = notificationService;
            _documentStorageService = documentStorageService;
        }

        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> Index()
        {
            await _appointmentLifecycleService.ExpirePastAppointmentsAsync();

            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin"))
            {
                var adminAppointments =
                    await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .OrderByDescending(a =>
                            a.AppointmentDate)
                        .ToListAsync();

                return View(
                    "AdminIndex",
                    adminAppointments);
            }

            if (user == null)
                return Unauthorized();

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.IsActive);

            if (patient == null)
                return Forbid();

            var appointments = await _context.Appointments
                .Where(a => a.PatientId == patient.Id)
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .ToListAsync();

            return View(appointments);
        }

        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            await _appointmentLifecycleService.ExpirePastAppointmentsAsync();

            if (id == null)
                return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
                return NotFound();

            if (User.IsInRole("Admin"))
            {
                return View(
                    "AdminDetails",
                    appointment);
            }

            if (User.IsInRole("Patient"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                    return Unauthorized();

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

                if (patient == null ||
                    appointment.PatientId != patient.Id)
                {
                    return Forbid();
                }
            }

            ViewBag.PendingRescheduleRequest =
                await _context.AppointmentRescheduleRequests
                    .Where(x =>
                        x.AppointmentId == appointment.Id &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            ViewBag.AppointmentIntake =
                await _context.AppointmentIntakes
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId == appointment.Id);

            ViewBag.AppointmentFollowUpPlan =
                await _context.AppointmentFollowUpPlans
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId == appointment.Id);

            var appointmentDocuments =
                new List<AppointmentDocument>();

            var canUploadAppointmentDocument = false;

            if (User.IsInRole("Patient"))
            {
                appointmentDocuments =
                    await _context.AppointmentDocuments
                        .Where(x =>
                            x.AppointmentId == appointment.Id)
                        .OrderByDescending(x => x.UploadedAt)
                        .ToListAsync();

                canUploadAppointmentDocument =
                    CanUploadAppointmentDocument(appointment);
            }

            ViewBag.AppointmentDocuments =
                appointmentDocuments;

            ViewBag.CanUploadAppointmentDocument =
                canUploadAppointmentDocument;

            ViewBag.CanEditAppointmentIntake =
                User.IsInRole("Patient") &&
                CanEditAppointmentIntake(appointment);

            var appointmentMessages =
                new List<AppointmentMessage>();

            var canSendAppointmentMessage = false;

            if (User.IsInRole("Patient"))
            {
                var unreadDoctorMessages =
                    await _context.AppointmentMessages
                        .Where(x =>
                            x.AppointmentId == appointment.Id &&
                            x.SenderRole == "Doctor" &&
                            x.ReadAt == null)
                        .ToListAsync();

                foreach (var message in unreadDoctorMessages)
                {
                    message.ReadAt = DateTime.Now;
                }

                if (unreadDoctorMessages.Any())
                    await _context.SaveChangesAsync();

                appointmentMessages =
                    await _context.AppointmentMessages
                        .Where(x =>
                            x.AppointmentId == appointment.Id)
                        .OrderBy(x => x.CreatedAt)
                        .ToListAsync();

                canSendAppointmentMessage =
                    CanSendAppointmentMessage(appointment);
            }

            ViewBag.AppointmentMessages =
                appointmentMessages;

            ViewBag.CanSendAppointmentMessage =
                canSendAppointmentMessage;

            return View(appointment);
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Create(
            int? doctorId,
            DateTime? month)
        {
            var calendarMonth =
                NormalizeCalendarMonth(month);

            var vm = new AppointmentCreateVM
            {
                DoctorId = doctorId ?? 0,
                CalendarMonth = calendarMonth
            };

            vm.Doctors =
                await GetDoctorsSelectListAsync();

            if (vm.DoctorId > 0)
            {
                var doctorIsActive =
                    await _context.Doctors.AnyAsync(d =>
                        d.Id == vm.DoctorId &&
                        d.IsActive);

                if (!doctorIsActive)
                {
                    ModelState.AddModelError(
                        nameof(vm.DoctorId),
                        "Ο επιλεγμένος γιατρός δεν είναι διαθέσιμος.");

                    vm.DoctorId = 0;
                }
                else
                {
                    var start =
                        new DateTime(
                            vm.CalendarMonth.Year,
                            vm.CalendarMonth.Month,
                            1);

                    var end =
                        start.AddMonths(1).AddDays(-1);

                    vm.CalendarSlots =
                        await _schedulingService
                            .GetCalendarSlotsAsync(
                                vm.DoctorId,
                                start,
                                end);
                }
            }

            return View(vm);
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Reschedule(
            int id,
            DateTime? month)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            if (!_appointmentLifecycleService
                    .CanPatientRescheduleAppointment(
                        appointment))
            {
                TempData.FlashWarning(
                    "Η αλλαγή ώρας επιτρέπεται μόνο μέχρι 24 ώρες πριν από το ραντεβού.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var hasPendingRequest =
                await _context.AppointmentRescheduleRequests
                    .AnyAsync(x =>
                        x.AppointmentId == appointment.Id &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending);

            if (appointment.Status ==
                    AppointmentStatus.Approved &&
                hasPendingRequest)
            {
                TempData.FlashWarning(
                    "Υπάρχει ήδη αίτημα αλλαγής σε αναμονή για αυτό το ραντεβού.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var calendarMonth =
                NormalizeCalendarMonth(month);

            var start =
                new DateTime(
                    calendarMonth.Year,
                    calendarMonth.Month,
                    1);

            var end =
                start.AddMonths(1)
                    .AddDays(-1);

            var calendarSlots =
                await _schedulingService
                    .GetCalendarSlotsAsync(
                        appointment.DoctorId,
                        start,
                        end);

            calendarSlots =
                FilterRescheduleSlots(
                    calendarSlots);

            var vm = new AppointmentRescheduleVM
            {
                AppointmentId =
                    appointment.Id,

                DoctorId =
                    appointment.DoctorId,

                DoctorName =
                    $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",

                Specialty =
                    appointment.Doctor.Specialty,

                CurrentStatus =
                    appointment.Status,

                CurrentAppointmentDate =
                    appointment.AppointmentDate,

                CalendarMonth =
                    calendarMonth,

                CalendarSlots =
                    calendarSlots
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Reschedule(
            AppointmentRescheduleVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == vm.AppointmentId &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            vm.DoctorId =
                appointment.DoctorId;

            vm.DoctorName =
                $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}";

            vm.Specialty =
                appointment.Doctor.Specialty;

            vm.CurrentStatus =
                appointment.Status;

            vm.CurrentAppointmentDate =
                appointment.AppointmentDate;

            vm.CalendarMonth =
                NormalizeCalendarMonth(
                    vm.CalendarMonth);

            
            if (!_appointmentLifecycleService
                    .CanPatientRescheduleAppointment(
                        appointment))
            {
                TempData.FlashWarning(
                    "Η αλλαγή ώρας επιτρέπεται μόνο μέχρι 24 ώρες πριν από το ραντεβού.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var existingPendingRequest =
                await _context.AppointmentRescheduleRequests
                    .AnyAsync(x =>
                        x.AppointmentId == appointment.Id &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending);

            if (appointment.Status ==
                    AppointmentStatus.Approved &&
                existingPendingRequest)
            {
                TempData.FlashWarning(
                    "Υπάρχει ήδη αίτημα αλλαγής σε αναμονή για αυτό το ραντεβού.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            DateTime selectedDateTime =
                default;

            if (string.IsNullOrWhiteSpace(
                    vm.SelectedSlot))
            {
                ModelState.AddModelError(
                    nameof(vm.SelectedSlot),
                    "Επιλέξτε νέα διαθέσιμη ώρα.");
            }
            else if (!DateTime.TryParse(
                         vm.SelectedSlot,
                         out selectedDateTime))
            {
                ModelState.AddModelError(
                    nameof(vm.SelectedSlot),
                    "Η επιλεγμένη ώρα δεν είναι έγκυρη.");
            }
            else if (!_appointmentLifecycleService
                         .IsRequestedRescheduleDateAllowed(
                             selectedDateTime))
            {
                ModelState.AddModelError(
                    nameof(vm.SelectedSlot),
                    "Η νέα ώρα πρέπει να απέχει περισσότερο από 24 ώρες.");
            }
            else if (selectedDateTime ==
                     appointment.AppointmentDate)
            {
                ModelState.AddModelError(
                    nameof(vm.SelectedSlot),
                    "Επιλέξτε διαφορετική ώρα από το τρέχον ραντεβού.");
            }

            if (ModelState.IsValid)
            {
                var availableSlots =
                    await _schedulingService
                        .GetAvailableSlotsAsync(
                            appointment.DoctorId,
                            selectedDateTime.Date);

                if (!availableSlots.Contains(
                        selectedDateTime.TimeOfDay))
                {
                    ModelState.AddModelError(
                        nameof(vm.SelectedSlot),
                        "Η επιλεγμένη ώρα δεν είναι πλέον διαθέσιμη.");
                }
                else
                {
                    var previousAppointmentDate =
                        appointment.AppointmentDate;

                    var reason =
                        vm.Reason.Trim();

                    
                    if (appointment.Status ==
                        AppointmentStatus.Pending)
                    {
                        appointment.AppointmentDate =
                            selectedDateTime;

                        var rescheduleRequest =
                            new AppointmentRescheduleRequest
                            {
                                AppointmentId =
                                    appointment.Id,

                                PreviousAppointmentDate =
                                    previousAppointmentDate,

                                RequestedAppointmentDate =
                                    selectedDateTime,

                                Reason =
                                    reason,

                                Status =
                                    AppointmentRescheduleRequestStatus.Approved,

                                RequestedByRole =
                                    "Patient",

                                CreatedAt =
                                    DateTime.Now,

                                ReviewedAt =
                                    DateTime.Now,

                                ReviewedByUserId =
                                    user.Id
                            };

                        _context
                            .AppointmentRescheduleRequests
                            .Add(rescheduleRequest);

                        try
                        {
                            await _context.SaveChangesAsync();

                            await _appointmentNotificationService
                                .NotifyDoctorForPatientRescheduledPendingAppointmentAsync(
                                    appointment.Doctor,
                                    patient,
                                    appointment,
                                    previousAppointmentDate,
                                    reason,
                                    Url.Action(
                                        "Details",
                                        "DoctorDashboard",
                                        new
                                        {
                                            id =
                                                appointment.Id
                                        }),
                                    GetLoginUrl());

                            TempData.FlashSuccess(
                                "Το ραντεβού άλλαξε επιτυχώς και ο ιατρός ενημερώθηκε.");

                            return RedirectToAction(
                                nameof(Details),
                                new
                                {
                                    id =
                                        appointment.Id
                                });
                        }
                        catch (DbUpdateException ex)
                            when (
                                IsUniqueConstraintViolation(
                                    ex))
                        {
                            appointment.AppointmentDate =
                                previousAppointmentDate;

                            _context.Entry(
                                    appointment)
                                .State =
                                EntityState.Unchanged;

                            _context.Entry(
                                    rescheduleRequest)
                                .State =
                                EntityState.Detached;

                            ModelState.AddModelError(
                                nameof(vm.SelectedSlot),
                                "Η επιλεγμένη ώρα μόλις κλείστηκε από άλλον ασθενή. Επιλέξτε άλλη διαθέσιμη ώρα.");
                        }
                    }

                    
                    else if (appointment.Status ==
                             AppointmentStatus.Approved)
                    {
                        var rescheduleRequest =
                            new AppointmentRescheduleRequest
                            {
                                AppointmentId =
                                    appointment.Id,

                                PreviousAppointmentDate =
                                    previousAppointmentDate,

                                RequestedAppointmentDate =
                                    selectedDateTime,

                                Reason =
                                    reason,

                                Status =
                                    AppointmentRescheduleRequestStatus.Pending,

                                RequestedByRole =
                                    "Patient",

                                CreatedAt =
                                    DateTime.Now
                            };

                        _context
                            .AppointmentRescheduleRequests
                            .Add(rescheduleRequest);

                        try
                        {
                            await _context.SaveChangesAsync();

                            await _appointmentNotificationService
                                .NotifyDoctorForPatientRescheduleRequestAsync(
                                    appointment.Doctor,
                                    patient,
                                    appointment,
                                    rescheduleRequest,
                                    Url.Action(
                                        "Details",
                                        "DoctorDashboard",
                                        new
                                        {
                                            id =
                                                appointment.Id
                                        }),
                                    GetLoginUrl());

                            TempData.FlashSuccess(
                                "Το αίτημα αλλαγής στάλθηκε στον ιατρό.");

                            return RedirectToAction(
                                nameof(Details),
                                new
                                {
                                    id =
                                        appointment.Id
                                });
                        }
                        catch (DbUpdateException ex)
                            when (
                                IsUniqueConstraintViolation(
                                    ex))
                        {
                            _context.Entry(
                                    rescheduleRequest)
                                .State =
                                EntityState.Detached;

                            TempData.FlashWarning(
                                "Υπάρχει ήδη αίτημα αλλαγής σε αναμονή για αυτό το ραντεβού.");

                            return RedirectToAction(
                                nameof(Details),
                                new
                                {
                                    id =
                                        appointment.Id
                                });
                        }
                    }
                }
            }

            var start =
                new DateTime(
                    vm.CalendarMonth.Year,
                    vm.CalendarMonth.Month,
                    1);

            var end =
                start.AddMonths(1)
                    .AddDays(-1);

            var calendarSlots =
                await _schedulingService
                    .GetCalendarSlotsAsync(
                        appointment.DoctorId,
                        start,
                        end);

            vm.CalendarSlots =
                FilterRescheduleSlots(
                    calendarSlots);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Create(
            AppointmentCreateVM vm)
        {
            vm.Doctors =
                await GetDoctorsSelectListAsync();

            vm.CalendarMonth =
                NormalizeCalendarMonth(vm.CalendarMonth);

            if (vm.DoctorId <= 0)
            {
                ModelState.AddModelError(
                    nameof(vm.DoctorId),
                    "Επιλέξτε γιατρό.");
            }
            else
            {
                var doctorIsActive =
                    await _context.Doctors.AnyAsync(d =>
                        d.Id == vm.DoctorId &&
                        d.IsActive);

                if (!doctorIsActive)
                {
                    ModelState.AddModelError(
                        nameof(vm.DoctorId),
                        "Ο επιλεγμένος γιατρός δεν είναι διαθέσιμος.");
                }
            }

            if (string.IsNullOrWhiteSpace(vm.SelectedSlot))
            {
                ModelState.AddModelError(
                    nameof(vm.SelectedSlot),
                    "Επιλέξτε διαθέσιμη ώρα.");
            }

            if (string.IsNullOrWhiteSpace(vm.Description))
            {
                ModelState.AddModelError(
                    nameof(vm.Description),
                    "Συμπληρώστε περιγραφή.");
            }

            DateTime selectedDateTime = default;

            if (!DateTime.TryParse(
                    vm.SelectedSlot,
                    out selectedDateTime))
            {
                ModelState.AddModelError(
                    nameof(vm.SelectedSlot),
                    "Η επιλεγμένη ώρα δεν είναι έγκυρη.");
            }
            else if (
                selectedDateTime <
                DateTime.Now.AddHours(24))
            {
                ModelState.AddModelError(
                    nameof(vm.SelectedSlot),
                    "Το ραντεβού πρέπει να κλείνεται τουλάχιστον 24 ώρες νωρίτερα.");
            }

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
            {
                ModelState.AddModelError(
                    "",
                    "Το προφίλ ασθενή δεν είναι ολοκληρωμένο.");
            }

            if (ModelState.IsValid)
            {
                var availableSlots =
                    await _schedulingService
                        .GetAvailableSlotsAsync(
                            vm.DoctorId,
                            selectedDateTime.Date);

                if (!availableSlots.Contains(
                        selectedDateTime.TimeOfDay))
                {
                    ModelState.AddModelError(
                        nameof(vm.SelectedSlot),
                        "Η επιλεγμένη ώρα δεν είναι πλέον διαθέσιμη.");
                }
                else
                {
                    var appointment =
                        new Appointment
                        {
                            DoctorId =
                                vm.DoctorId,

                            PatientId =
                                patient!.Id,

                            AppointmentDate =
                                selectedDateTime,

                            Description =
                                vm.Description,

                            Status =
                                AppointmentStatus.Pending
                        };

                    _context.Appointments.Add(appointment);

                    try
                    {
                        await _context.SaveChangesAsync();

                        var doctor =
                            await _context.Doctors
                                .FirstOrDefaultAsync(d =>
                                    d.Id ==
                                    appointment.DoctorId);

                        if (doctor != null)
                        {
                            await _appointmentNotificationService
                                .NotifyDoctorForNewAppointmentAsync(
                                    doctor,
                                    patient!,
                                    appointment,
                                    Url.Action(
                                        "Details",
                                        "DoctorDashboard",
                                        new { id = appointment.Id }),
                                    GetLoginUrl());
                        }

                        TempData.FlashSuccess(
                            "Το αίτημα ραντεβού καταχωρήθηκε.");

                        return RedirectToAction(
                            "Index",
                            "PatientDashboard");
                    }
                    catch (DbUpdateException ex)
                        when (IsUniqueConstraintViolation(ex))
                    {
                        _context.Entry(appointment).State =
                            EntityState.Detached;

                        ModelState.AddModelError(
                            nameof(vm.SelectedSlot),
                            "Η επιλεγμένη ώρα μόλις κλείστηκε από άλλον ασθενή. Επιλέξτε άλλη διαθέσιμη ώρα.");
                    }
                }
            }

            if (vm.DoctorId > 0)
            {
                var start =
                    new DateTime(
                        vm.CalendarMonth.Year,
                        vm.CalendarMonth.Month,
                        1);

                var end =
                    start.AddMonths(1).AddDays(-1);

                vm.CalendarSlots =
                    await _schedulingService
                        .GetCalendarSlotsAsync(
                            vm.DoctorId,
                            start,
                            end);
            }

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int? id)
        {
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(
            int id,
            Appointment input)
        {
            return Forbid();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int? id)
        {
            return Forbid();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            return Forbid();
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            if (appointment.AppointmentDate <= DateTime.Now ||
                (
                    appointment.Status != AppointmentStatus.Pending &&
                    appointment.Status != AppointmentStatus.Approved
                ))
            {
                TempData.FlashWarning(
                    "Αυτό το ραντεβού δεν μπορεί πλέον να ακυρωθεί.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var vm = new AppointmentCancelVM
            {
                Id =
                    appointment.Id,

                AppointmentDate =
                    appointment.AppointmentDate,

                DoctorName =
                    $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",

                Specialty =
                    appointment.Doctor.Specialty
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Cancel(
            AppointmentCancelVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == vm.Id &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            vm.AppointmentDate =
                appointment.AppointmentDate;

            vm.DoctorName =
                $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}";

            vm.Specialty =
                appointment.Doctor.Specialty;

            if (appointment.AppointmentDate <= DateTime.Now)
            {
                TempData.FlashWarning(
                    "Δεν μπορείτε να ακυρώσετε ραντεβού που έχει ήδη ξεκινήσει ή έχει περάσει.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (appointment.Status != AppointmentStatus.Pending &&
                appointment.Status != AppointmentStatus.Approved)
            {
                TempData.FlashWarning(
                    "Αυτό το ραντεβού δεν μπορεί πλέον να ακυρωθεί.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            if (!ModelState.IsValid)
                return View(vm);

            var cancellationReason =
                vm.CancellationReason.Trim();

            appointment.Status =
                AppointmentStatus.Cancelled;

            appointment.CancellationReason =
                cancellationReason;

            appointment.CancelledAt =
                DateTime.Now;

            appointment.CancelledByRole =
                "Patient";

            var pendingRescheduleRequests =
                await _context.AppointmentRescheduleRequests
                    .Where(x =>
                        x.AppointmentId == appointment.Id &&
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending)
                    .ToListAsync();

            foreach (var request in pendingRescheduleRequests)
            {
                request.Status =
                    AppointmentRescheduleRequestStatus.Rejected;

                request.ReviewedAt =
                    DateTime.Now;

                request.ReviewedByUserId =
                    user.Id;

                request.ReviewComment =
                    "Το ραντεβού ακυρώθηκε πριν εξεταστεί το αίτημα αλλαγής.";
            }

            await _context.SaveChangesAsync();

            if (appointment.Doctor != null)
            {
                await _appointmentNotificationService
                    .NotifyDoctorForPatientCancellationAsync(
                        appointment.Doctor,
                        patient,
                        appointment,
                        cancellationReason,
                        Url.Action(
                            "Details",
                            "DoctorDashboard",
                            new { id = appointment.Id }),
                        GetLoginUrl());
            }

            TempData.FlashSuccess(
                "Το ραντεβού ακυρώθηκε επιτυχώς.");

            return RedirectToAction(
                "Index",
                "PatientDashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> SendMessage(
            int id,
            string message)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var isAjax =
                IsAjaxRequest(Request);

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            if (!CanSendAppointmentMessage(appointment))
            {
                const string error =
                    "Τα μηνύματα για αυτό το ραντεβού είναι πλέον μόνο για προβολή.";

                if (isAjax)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = error
                    });
                }

                TempData.FlashWarning(error);

                return RedirectToDetailsTab(
                    appointment.Id,
                    "messages");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                const string error =
                    "Το μήνυμα δεν μπορεί να είναι κενό.";

                if (isAjax)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = error
                    });
                }

                TempData.FlashWarning(error);

                return RedirectToDetailsTab(
                    appointment.Id,
                    "messages");
            }

            var trimmedMessage =
                message.Trim();

            if (trimmedMessage.Length > 1000)
            {
                const string error =
                    "Το μήνυμα δεν μπορεί να ξεπερνά τους 1000 χαρακτήρες.";

                if (isAjax)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = error
                    });
                }

                TempData.FlashWarning(error);

                return RedirectToDetailsTab(
                    appointment.Id,
                    "messages");
            }

            var appointmentMessage =
                new AppointmentMessage
                {
                    AppointmentId =
                        appointment.Id,

                    SenderUserId =
                        user.Id,

                    SenderRole =
                        "Patient",

                    Message =
                        trimmedMessage,

                    CreatedAt =
                        DateTime.Now
                };

            _context.AppointmentMessages
                .Add(appointmentMessage);

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(
                    appointment.Doctor.UserId))
            {
                await _notificationService.CreateAsync(
                    appointment.Doctor.UserId,
                    "Νέο μήνυμα ραντεβού",
                    $"Ο/Η {patient.FirstName} {patient.LastName} έστειλε μήνυμα για το ραντεβού στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",
                    Url.Action(
                        "Details",
                        "DoctorDashboard",
                        new { id = appointment.Id }));
            }

            if (isAjax)
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

            return RedirectToDetailsTab(
                appointment.Id,
                "messages");
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Intake(int id)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            if (!CanEditAppointmentIntake(appointment))
            {
                TempData.FlashInfo(
                    "Οι πληροφορίες για αυτό το ραντεβού είναι πλέον μόνο για προβολή.");

                return RedirectToAction(
                    nameof(Details),
                    new { id = appointment.Id });
            }

            var intake =
                await _context.AppointmentIntakes
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id);

            var vm = new AppointmentIntakeVM
            {
                AppointmentId =
                    appointment.Id,

                AppointmentDate =
                    appointment.AppointmentDate,

                DoctorName =
                    $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",

                Specialty =
                    appointment.Doctor.Specialty,

                MainConcern =
                    intake?.MainConcern ??
                    appointment.Description,

                Symptoms =
                    intake?.Symptoms,

                StartedWhen =
                    intake?.StartedWhen,

                CurrentMedications =
                    intake?.CurrentMedications,

                Allergies =
                    intake?.Allergies,

                PreviousRelevantExams =
                    intake?.PreviousRelevantExams,

                ExtraNotes =
                    intake?.ExtraNotes
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Intake(
            AppointmentIntakeVM vm)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == vm.AppointmentId &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            vm.AppointmentDate =
                appointment.AppointmentDate;

            vm.DoctorName =
                $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}";

            vm.Specialty =
                appointment.Doctor.Specialty;

            if (!CanEditAppointmentIntake(appointment))
            {
                TempData.FlashInfo(
                    "Οι πληροφορίες για αυτό το ραντεβού είναι πλέον μόνο για προβολή.");

                return RedirectToDetailsTab(
                    appointment.Id,
                    "preparation");
            }

            if (!ModelState.IsValid)
                return View(vm);

            var intake =
                await _context.AppointmentIntakes
                    .FirstOrDefaultAsync(x =>
                        x.AppointmentId ==
                        appointment.Id);

            var isNew =
                intake == null;

            if (intake == null)
            {
                intake =
                    new AppointmentIntake
                    {
                        AppointmentId =
                            appointment.Id,

                        SubmittedAt =
                            DateTime.Now
                    };

                _context.AppointmentIntakes
                    .Add(intake);
            }
            else
            {
                intake.UpdatedAt =
                    DateTime.Now;
            }

            intake.MainConcern =
                vm.MainConcern.Trim();

            intake.Symptoms =
                NormalizeNullableText(vm.Symptoms);

            intake.StartedWhen =
                NormalizeNullableText(vm.StartedWhen);

            intake.CurrentMedications =
                NormalizeNullableText(
                    vm.CurrentMedications);

            intake.Allergies =
                NormalizeNullableText(vm.Allergies);

            intake.PreviousRelevantExams =
                NormalizeNullableText(
                    vm.PreviousRelevantExams);

            intake.ExtraNotes =
                NormalizeNullableText(vm.ExtraNotes);

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(
                    appointment.Doctor.UserId))
            {
                await _notificationService.CreateAsync(
                    appointment.Doctor.UserId,
                    isNew
                        ? "Συμπληρώθηκαν πληροφορίες επίσκεψης"
                        : "Ενημερώθηκαν πληροφορίες επίσκεψης",

                    $"Ο/Η {patient.FirstName} {patient.LastName} {(isNew ? "συμπλήρωσε" : "ενημέρωσε")} τις πληροφορίες πριν το ραντεβού στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",

                    Url.Action(
                        "Details",
                        "DoctorDashboard",
                        new { id = appointment.Id }));
            }

            TempData.FlashSuccess(
                isNew
                    ? "Οι πληροφορίες πριν την επίσκεψη αποθηκεύτηκαν."
                    : "Οι πληροφορίες πριν την επίσκεψη ενημερώθηκαν.");

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = appointment.Id,
                    activeTab = "preparation"
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> UploadDocument(
            int id,
            IFormFile documentFile)
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var appointment =
                await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.PatientId == patient.Id);

            if (appointment == null)
                return NotFound();

            if (!CanUploadAppointmentDocument(appointment))
            {
                TempData.FlashWarning(
                    "Δεν μπορείτε πλέον να ανεβάσετε αρχεία για αυτό το ραντεβού.");

                return RedirectToDetailsTab(
                    appointment.Id,
                    "documents");
            }

            var validationError =
                _documentStorageService.Validate(
                    documentFile);

            if (validationError != null)
            {
                TempData.FlashWarning(
                    validationError);

                return RedirectToDetailsTab(
                    appointment.Id,
                    "documents");
            }

            var savedFile =
                await _documentStorageService
                    .SaveAsync(documentFile);

            var appointmentDocument =
                new AppointmentDocument
                {
                    AppointmentId =
                        appointment.Id,

                    UploadedByUserId =
                        user.Id,

                    UploadedByRole =
                        "Patient",

                    OriginalFileName =
                        savedFile.OriginalFileName,

                    StoredFileName =
                        savedFile.StoredFileName,

                    ContentType =
                        savedFile.ContentType,

                    FileSize =
                        savedFile.FileSize,

                    UploadedAt =
                        DateTime.Now
                };

            _context.AppointmentDocuments
                .Add(appointmentDocument);

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(
                    appointment.Doctor.UserId))
            {
                await _notificationService.CreateAsync(
                    appointment.Doctor.UserId,
                    "Νέο αρχείο ραντεβού",
                    $"Ο/Η {patient.FirstName} {patient.LastName} ανέβασε αρχείο για το ραντεβού στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",
                    Url.Action(
                        "Details",
                        "DoctorDashboard",
                        new { id = appointment.Id }));
            }

            TempData.FlashSuccess(
                "Το αρχείο ανέβηκε επιτυχώς.");

            return RedirectToDetailsTab(
                appointment.Id,
                "documents");
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var document =
                await _context.AppointmentDocuments
                    .Include(x => x.Appointment)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.Appointment.PatientId ==
                        patient.Id);

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

                return RedirectToDetailsTab(
                    document.AppointmentId,
                    "documents");
            }

            return PhysicalFile(
                filePath,
                document.ContentType,
                document.OriginalFileName);
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ViewDocument(int id)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var document =
                await _context.AppointmentDocuments
                    .Include(x => x.Appointment)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.Appointment.PatientId ==
                        patient.Id);

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

                return RedirectToDetailsTab(
                    document.AppointmentId,
                    "documents");
            }

            return PhysicalFile(
                filePath,
                document.ContentType,
                enableRangeProcessing: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.UserId == user.Id &&
                        p.IsActive);

            if (patient == null)
                return Forbid();

            var document =
                await _context.AppointmentDocuments
                    .Include(x => x.Appointment)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.Appointment.PatientId ==
                        patient.Id &&
                        x.UploadedByUserId ==
                        user.Id);

            if (document == null)
                return NotFound();

            var appointmentId =
                document.AppointmentId;

            var filePath =
                _documentStorageService
                    .GetFilePath(
                        document.StoredFileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.AppointmentDocuments
                .Remove(document);

            await _context.SaveChangesAsync();

            TempData.FlashSuccess(
                "Το αρχείο διαγράφηκε.");

            return RedirectToDetailsTab(
                appointmentId,
                "documents");
        }

        // PRIVATE METHODS

        private static bool IsUniqueConstraintViolation(
            DbUpdateException ex)
        {
            return ex.InnerException
                       is SqlException sqlException &&
                   (
                       sqlException.Number == 2601 ||
                       sqlException.Number == 2627
                   );
        }

        private string? GetLoginUrl()
        {
            return Url.Page(
                "/Account/Login",
                pageHandler: null,
                values: new { area = "Identity" },
                protocol: Request.Scheme);
        }

        private static bool CanSendAppointmentMessage(
            Appointment appointment)
        {
            return appointment.Status ==
                       AppointmentStatus.Pending ||
                   appointment.Status ==
                       AppointmentStatus.Approved;
        }

        private static bool CanEditAppointmentIntake(
            Appointment appointment)
        {
            return appointment.Status ==
                       AppointmentStatus.Approved &&
                   appointment.AppointmentDate >
                       DateTime.Now;
        }

        private static bool CanUploadAppointmentDocument(
            Appointment appointment)
        {
            return appointment.AppointmentDate >
                       DateTime.Now &&
                   (
                       appointment.Status ==
                           AppointmentStatus.Pending ||
                       appointment.Status ==
                           AppointmentStatus.Approved
                   );
        }

        private DateTime NormalizeCalendarMonth(
            DateTime? month)
        {
            var currentMonth =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1);

            if (!month.HasValue)
                return currentMonth;

            var requestedMonth =
                new DateTime(
                    month.Value.Year,
                    month.Value.Month,
                    1);

            if (requestedMonth < currentMonth)
                return currentMonth;

            return requestedMonth;
        }

        private async Task<List<SelectListItem>>
            GetDoctorsSelectListAsync()
        {
            return await _context.Doctors
                .Where(d => d.IsActive)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .Select(d =>
                    new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text =
                            d.FirstName +
                            " " +
                            d.LastName +
                            " (" +
                            d.Specialty +
                            ")"
                    })
                .ToListAsync();
        }

        private static string? NormalizeNullableText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private IActionResult RedirectToDetailsTab(
            int appointmentId,
            string activeTab)
        {
            activeTab = activeTab switch
            {
                "preparationPanel" => "preparation",
                "documentsPanel" => "documents",
                "messagesPanel" => "messages",
                "followupPanel" => "followup",
                "overviewPanel" => "overview",
                _ => activeTab
            };

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = appointmentId,
                    activeTab
                });
        }

        private static bool IsAjaxRequest(
            HttpRequest request)
        {
            return request.Headers["X-Requested-With"] ==
                   "XMLHttpRequest";
        }


        private List<CalendarSlotVM> FilterRescheduleSlots(
            IEnumerable<CalendarSlotVM> slots)
        {
            return slots
                .Where(slot =>
                    _appointmentLifecycleService
                        .IsRequestedRescheduleDateAllowed(
                            slot.Date.Date.Add(
                                slot.Time)))
                .OrderBy(slot => slot.Date)
                .ThenBy(slot => slot.Time)
                .ToList();
        }


    }
}