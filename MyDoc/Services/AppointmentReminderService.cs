using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Models;

namespace MyDoc.Services
{
    public class AppointmentReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly AppointmentNotificationService _appointmentNotificationService;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(
            ApplicationDbContext context,
            AppointmentNotificationService appointmentNotificationService,
            ILogger<AppointmentReminderService> logger)
        {
            _context = context;
            _appointmentNotificationService = appointmentNotificationService;
            _logger = logger;
        }

        public async Task SendDueRemindersAsync()
        {
            var now = DateTime.Now;

            await SendReminderTypeAsync(
                AppointmentReminderType.TwentyFourHours,
                now.AddHours(2),
                now.AddHours(24),
                now);

            await SendReminderTypeAsync(
                AppointmentReminderType.TwoHours,
                now,
                now.AddHours(2),
                now);
        }

        private async Task SendReminderTypeAsync(
            AppointmentReminderType reminderType,
            DateTime windowStartExclusive,
            DateTime windowEndInclusive,
            DateTime now)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a =>
                    a.Status == AppointmentStatus.Approved &&
                    a.PatientId != null &&
                    a.AppointmentDate > windowStartExclusive &&
                    a.AppointmentDate <= windowEndInclusive &&
                    !_context.AppointmentReminderLogs.Any(log =>
                        log.AppointmentId == a.Id &&
                        log.ReminderType == reminderType))
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            foreach (var appointment in appointments)
            {
                if (appointment.Patient == null || appointment.Doctor == null)
                    continue;

                try
                {
                    await _appointmentNotificationService.NotifyPatientForAppointmentReminderAsync(
                        appointment.Doctor,
                        appointment.Patient,
                        appointment,
                        reminderType,
                        $"/Appointments/Details/{appointment.Id}",
                        null);

                    _context.AppointmentReminderLogs.Add(new AppointmentReminderLog
                    {
                        AppointmentId = appointment.Id,
                        ReminderType = reminderType,
                        SentAt = now
                    });

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send {ReminderType} reminder for appointment {AppointmentId}",
                        reminderType,
                        appointment.Id);
                }
            }
        }
    }
}
