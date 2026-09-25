using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Models;

namespace MyDoc.Services
{
    public class AppointmentLifecycleService
    {
        private readonly ApplicationDbContext _context;
        private readonly AppointmentNotificationService _appointmentNotificationService;

        private const int AppointmentDurationMinutes = 30;
        private const int RescheduleCutoffHours = 24;

        public AppointmentLifecycleService(
            ApplicationDbContext context,
            AppointmentNotificationService appointmentNotificationService)
        {
            _context = context;
            _appointmentNotificationService = appointmentNotificationService;
        }

        public async Task ExpirePastAppointmentsAsync()
        {
            var now = DateTime.Now;

            var appointmentsToExpire = await _context.Appointments
                .Where(a =>
                    a.AppointmentDate <= now &&
                    a.Status == AppointmentStatus.Pending)
                .ToListAsync();

            foreach (var appointment in appointmentsToExpire)
            {
                appointment.Status = AppointmentStatus.Expired;
            }

            
            var pendingRescheduleRequests =
                await _context.AppointmentRescheduleRequests
                    .Include(x => x.Appointment)
                        .ThenInclude(a => a.Patient)
                    .Include(x => x.Appointment)
                        .ThenInclude(a => a.Doctor)
                    .Where(x =>
                        x.Status ==
                        AppointmentRescheduleRequestStatus.Pending)
                    .ToListAsync();

            var expiredRequestsToNotify =
                new List<AppointmentRescheduleRequest>();

            foreach (var request in pendingRescheduleRequests)
            {
                var appointment = request.Appointment;

                
                if (appointment.Status != AppointmentStatus.Approved)
                {
                    ExpireRescheduleRequest(
                        request,
                        now,
                        "Το αίτημα έληξε επειδή το ραντεβού δεν είναι πλέον διαθέσιμο για αλλαγή.");

                    continue;
                }

                var deadline =
                    GetRescheduleRequestDeadline(
                        appointment,
                        request.RequestedAppointmentDate);

                if (now >= deadline)
                {
                    ExpireRescheduleRequest(
                        request,
                        now,
                        "Το αίτημα έληξε επειδή έφτασε το χρονικό όριο αλλαγής.");

                    if (appointment.Patient != null &&
                        appointment.Doctor != null)
                    {
                        expiredRequestsToNotify.Add(request);
                    }
                }
            }

            if (appointmentsToExpire.Any() ||
                pendingRescheduleRequests.Any(x =>
                    x.Status ==
                    AppointmentRescheduleRequestStatus.Expired))
            {
                await _context.SaveChangesAsync();
            }

            
            foreach (var request in expiredRequestsToNotify)
            {
                var appointment = request.Appointment;

                if (appointment.Patient == null ||
                    appointment.Doctor == null)
                {
                    continue;
                }

                await _appointmentNotificationService
                    .NotifyPatientForRescheduleRequestExpiredAsync(
                        appointment.Doctor,
                        appointment.Patient,
                        appointment,
                        request,
                        $"/Appointments/Details/{appointment.Id}",
                        null);
            }
        }

        public bool HasAppointmentExpired(
            Appointment appointment)
        {
            return appointment.AppointmentDate
                .AddMinutes(AppointmentDurationMinutes) <=
                DateTime.Now;
        }

        public bool CanPatientRescheduleAppointment(
            Appointment appointment)
        {
            var now = DateTime.Now;

            if (appointment.Status != AppointmentStatus.Pending &&
                appointment.Status != AppointmentStatus.Approved)
            {
                return false;
            }

            
            return appointment.AppointmentDate >
                   now.AddHours(RescheduleCutoffHours);
        }

        public bool IsRequestedRescheduleDateAllowed(
            DateTime requestedAppointmentDate)
        {
            
            return requestedAppointmentDate >
                   DateTime.Now.AddHours(
                       RescheduleCutoffHours);
        }

        public DateTime GetRescheduleRequestDeadline(
            Appointment appointment,
            DateTime requestedAppointmentDate)
        {
            
            var earliestAppointmentDate =
                appointment.AppointmentDate <=
                requestedAppointmentDate
                    ? appointment.AppointmentDate
                    : requestedAppointmentDate;

            return earliestAppointmentDate
                .AddHours(-RescheduleCutoffHours);
        }

        public bool HasRescheduleRequestExpired(
            Appointment appointment,
            AppointmentRescheduleRequest request)
        {
            if (request.Status !=
                AppointmentRescheduleRequestStatus.Pending)
            {
                return true;
            }

            if (appointment.Status !=
                AppointmentStatus.Approved)
            {
                return true;
            }

            var deadline =
                GetRescheduleRequestDeadline(
                    appointment,
                    request.RequestedAppointmentDate);

            return DateTime.Now >= deadline;
        }

        private static void ExpireRescheduleRequest(
            AppointmentRescheduleRequest request,
            DateTime expiredAt,
            string reason)
        {
            request.Status =
                AppointmentRescheduleRequestStatus.Expired;

            request.ReviewedAt =
                expiredAt;

            request.ReviewedByUserId =
                null;

            request.ReviewComment =
                reason;
        }
    }
}