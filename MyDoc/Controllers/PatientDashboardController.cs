using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Models;
using MyDoc.Services;

[Authorize(Roles = "Patient")]
public class PatientDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppointmentLifecycleService _appointmentLifecycleService;

    public PatientDashboardController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        AppointmentLifecycleService appointmentLifecycleService)
    {
        _context = context;
        _userManager = userManager;
        _appointmentLifecycleService = appointmentLifecycleService;
    }

    public async Task<IActionResult> Index()
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

        var now =
            DateTime.Now;

        var appointments =
            await _context.Appointments
                .Where(a =>
                    a.PatientId == patient.Id)
                .Include(a => a.Doctor)
                .OrderBy(a =>
                    a.AppointmentDate)
                .ToListAsync();

        var appointmentIds =
            appointments
                .Select(a => a.Id)
                .ToList();

        var appointmentIdsWithPreInfo =
            await _context.AppointmentIntakes
                .Where(x =>
                    appointmentIds.Contains(
                        x.AppointmentId))
                .Select(x =>
                    x.AppointmentId)
                .ToListAsync();

        var appointmentIdsWithPendingReschedule =
            await _context.AppointmentRescheduleRequests
                .Where(x =>
                    appointmentIds.Contains(
                        x.AppointmentId) &&
                    x.Status ==
                    AppointmentRescheduleRequestStatus.Pending)
                .Select(x =>
                    x.AppointmentId)
                .ToListAsync();

        var appointmentIdsWithFollowUp =
            await _context.AppointmentFollowUpPlans
                .Where(x =>
                    appointmentIds.Contains(
                        x.AppointmentId))
                .Select(x =>
                    x.AppointmentId)
                .ToListAsync();

        var preInfoSet =
            appointmentIdsWithPreInfo
                .ToHashSet();

        var pendingRescheduleSet =
            appointmentIdsWithPendingReschedule
                .ToHashSet();

        var followUpSet =
            appointmentIdsWithFollowUp
                .ToHashSet();

        var items =
            appointments
                .Select(appointment =>
                {
                    var isFutureActive =
                        appointment.AppointmentDate >
                        now &&
                        (
                            appointment.Status ==
                            AppointmentStatus.Pending ||

                            appointment.Status ==
                            AppointmentStatus.Approved
                        );

                    var hasPreAppointmentInfo =
                        preInfoSet.Contains(
                            appointment.Id);

                    return new PatientAppointmentItemVM
                    {
                        Id =
                            appointment.Id,

                        DoctorId =
                            appointment.DoctorId,

                        AppointmentDate =
                            appointment.AppointmentDate,

                        Status =
                            appointment.Status,

                        DoctorName =
                            appointment.Doctor == null
                                ? "-"
                                : $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",

                        Specialty =
                            appointment.Doctor?
                                .Specialty ?? "-",

                        Description =
                            appointment.Description,

                        HasPreAppointmentInfo =
                            hasPreAppointmentInfo,

                        NeedsPreAppointmentInfo =
                            isFutureActive &&
                            !hasPreAppointmentInfo,

                        HasPendingRescheduleRequest =
                            pendingRescheduleSet.Contains(
                                appointment.Id),

                        HasFollowUpPlan =
                            followUpSet.Contains(
                                appointment.Id),

                        
                        CanReschedule =
                            _appointmentLifecycleService
                                .CanPatientRescheduleAppointment(
                                    appointment),

                        
                        CanCancel =
                            isFutureActive
                    };
                })
                .ToList();

        var vm =
            new PatientDashboardVM
            {
                UpcomingAppointments =
                    items
                        .Where(x =>
                            x.AppointmentDate >
                            now &&
                            (
                                x.Status ==
                                AppointmentStatus.Pending ||

                                x.Status ==
                                AppointmentStatus.Approved
                            ))
                        .OrderBy(x =>
                            x.AppointmentDate)
                        .ToList(),

                HistoryAppointments =
                    items
                        .Where(x =>
                            !(
                                x.AppointmentDate >
                                now &&
                                (
                                    x.Status ==
                                    AppointmentStatus.Pending ||

                                    x.Status ==
                                    AppointmentStatus.Approved
                                )
                            ))
                        .OrderByDescending(x =>
                            x.AppointmentDate)
                        .ToList()
            };

        return View(vm);
    }
}