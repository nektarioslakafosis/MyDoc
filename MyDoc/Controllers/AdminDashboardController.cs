using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Models;
using MyDoc.Services;

namespace MyDoc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AppointmentLifecycleService _appointmentLifecycleService;

        public AdminDashboardController(
            ApplicationDbContext context,
            AppointmentLifecycleService appointmentLifecycleService)
        {
            _context = context;
            _appointmentLifecycleService = appointmentLifecycleService;
        }

        public async Task<IActionResult> Index()
        {
            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var now = DateTime.Now;

            var vm = new AdminDashboardVM
            {
                TotalDoctors =
                    await _context.Doctors.CountAsync(),

                ActiveDoctors =
                    await _context.Doctors
                        .CountAsync(d => d.IsActive),

                InactiveDoctors =
                    await _context.Doctors
                        .CountAsync(d => !d.IsActive),

                TotalPatients =
                    await _context.Patients.CountAsync(),

                ActivePatients =
                    await _context.Patients
                        .CountAsync(p => p.IsActive),

                InactivePatients =
                    await _context.Patients
                        .CountAsync(p => !p.IsActive),

                TotalAppointments =
                    await _context.Appointments.CountAsync(),

                PendingAppointments =
                    await _context.Appointments
                        .CountAsync(a =>
                            a.Status ==
                            AppointmentStatus.Pending),

                ApprovedAppointments =
                    await _context.Appointments
                        .CountAsync(a =>
                            a.Status ==
                            AppointmentStatus.Approved),

                CancelledAppointments =
                    await _context.Appointments
                        .CountAsync(a =>
                            a.Status ==
                            AppointmentStatus.Cancelled),

                ExpiredAppointments =
                    await _context.Appointments
                        .CountAsync(a =>
                            a.Status ==
                            AppointmentStatus.Expired),

                CompletedAppointments =
                    await _context.Appointments
                        .CountAsync(a =>
                            a.Status ==
                            AppointmentStatus.Completed),

                NoShowAppointments =
                    await _context.Appointments
                        .CountAsync(a =>
                            a.Status ==
                            AppointmentStatus.NoShow),

                TodayAppointments =
                    await _context.Appointments
                        .CountAsync(a =>
                            a.AppointmentDate >= today &&
                            a.AppointmentDate < tomorrow &&
                            (
                                a.Status ==
                                AppointmentStatus.Pending ||
                                a.Status ==
                                AppointmentStatus.Approved
                            )),

                UpcomingAppointments =
                    await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .Where(a =>
                            a.AppointmentDate >= now &&
                            (
                                a.Status ==
                                AppointmentStatus.Pending ||
                                a.Status ==
                                AppointmentStatus.Approved
                            ))
                        .OrderBy(a => a.AppointmentDate)
                        .Take(7)
                        .ToListAsync()
            };

            return View(vm);
        }
    }
}