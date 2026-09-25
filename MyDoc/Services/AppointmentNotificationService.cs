using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;
using MyDoc.Models;

namespace MyDoc.Services
{
    public class AppointmentNotificationService
    {
        private readonly NotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AppointmentNotificationService> _logger;

        public AppointmentNotificationService(
            NotificationService notificationService,
            IEmailSender emailSender,
            ILogger<AppointmentNotificationService> logger)
        {
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task NotifyDoctorForNewAppointmentAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title = "Νέο αίτημα ραντεβού";

            var message =
                $"Ο/Η {patient.FirstName} {patient.LastName} ζήτησε νέο ραντεβού για {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.";

            if (!string.IsNullOrWhiteSpace(doctor.UserId))
            {
                await _notificationService.CreateAsync(
                    doctor.UserId,
                    title,
                    message,
                    notificationLinkUrl);
            }

            await SendEmailSafeAsync(
                doctor.Email,
                "Νέο αίτημα ραντεβού - MyDoc",
                BuildEmailBody(
                    "Νέο αίτημα ραντεβού",
                    message,
                    appointment,
                    null,
                    emailLinkUrl));
        }

        public async Task NotifyDoctorForPatientCancellationAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            string cancellationReason,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title = "Ακύρωση ραντεβού";

            var message =
                $"Ο/Η {patient.FirstName} {patient.LastName} ακύρωσε το ραντεβού στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}. Λόγος: {cancellationReason}";

            if (!string.IsNullOrWhiteSpace(doctor.UserId))
            {
                await _notificationService.CreateAsync(
                    doctor.UserId,
                    title,
                    message,
                    notificationLinkUrl);
            }

            await SendEmailSafeAsync(
                doctor.Email,
                "Ακύρωση ραντεβού - MyDoc",
                BuildEmailBody(
                    "Ακύρωση ραντεβού",
                    $"Ο/Η {patient.FirstName} {patient.LastName} ακύρωσε το ραντεβού.",
                    appointment,
                    cancellationReason,
                    emailLinkUrl));
        }

        public async Task NotifyPatientForAppointmentApprovedAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Το ραντεβού εγκρίθηκε";

            var message =
                $"Το ραντεβού σας με τον/την ιατρό {doctor.FirstName} {doctor.LastName} στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm} εγκρίθηκε.";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Το ραντεβού σας εγκρίθηκε - MyDoc",
                BuildEmailBody(
                    "Το ραντεβού σας εγκρίθηκε",
                    message,
                    appointment,
                    null,
                    emailLinkUrl));
        }

        public async Task NotifyPatientForDoctorCancellationAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            string cancellationReason,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Το ραντεβού ακυρώθηκε";

            var message =
                $"Το ραντεβού σας με τον/την ιατρό {doctor.FirstName} {doctor.LastName} στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm} ακυρώθηκε. Λόγος: {cancellationReason}";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Το ραντεβού σας ακυρώθηκε - MyDoc",
                BuildEmailBody(
                    "Το ραντεβού σας ακυρώθηκε",
                    $"Το ραντεβού σας με τον/την ιατρό {doctor.FirstName} {doctor.LastName} ακυρώθηκε.",
                    appointment,
                    cancellationReason,
                    emailLinkUrl));
        }

        public async Task NotifyPatientForUnavailablePeriodCancellationAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            string cancellationReason,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Το ραντεβού ακυρώθηκε";

            var message =
                $"Το ραντεβού σας με τον/την ιατρό {doctor.FirstName} {doctor.LastName} στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm} ακυρώθηκε λόγω αλλαγής διαθεσιμότητας. Λόγος: {cancellationReason}";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Το ραντεβού σας ακυρώθηκε - MyDoc",
                BuildEmailBody(
                    "Το ραντεβού σας ακυρώθηκε",
                    $"Το ραντεβού σας με τον/την ιατρό {doctor.FirstName} {doctor.LastName} ακυρώθηκε λόγω αλλαγής διαθεσιμότητας.",
                    appointment,
                    cancellationReason,
                    emailLinkUrl));
        }

        public async Task NotifyDoctorForPatientRescheduledPendingAppointmentAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            DateTime previousAppointmentDate,
            string reason,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Αλλαγή αιτήματος ραντεβού";

            var message =
                $"Ο/Η {patient.FirstName} {patient.LastName} άλλαξε το ζητούμενο ραντεβού από {previousAppointmentDate:dd/MM/yyyy HH:mm} σε {appointment.AppointmentDate:dd/MM/yyyy HH:mm}. Λόγος: {reason}";

            if (!string.IsNullOrWhiteSpace(doctor.UserId))
            {
                await _notificationService.CreateAsync(
                    doctor.UserId,
                    title,
                    message,
                    notificationLinkUrl);
            }

            await SendEmailSafeAsync(
                doctor.Email,
                "Αλλαγή αιτήματος ραντεβού - MyDoc",
                BuildEmailBody(
                    "Αλλαγή αιτήματος ραντεβού",
                    $"Ο/Η {patient.FirstName} {patient.LastName} άλλαξε το ζητούμενο ραντεβού από {previousAppointmentDate:dd/MM/yyyy HH:mm} σε {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",
                    appointment,
                    reason,
                    emailLinkUrl));
        }

        public async Task NotifyDoctorForPatientRescheduleRequestAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            AppointmentRescheduleRequest request,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Νέο αίτημα αλλαγής ραντεβού";

            var message =
                $"Ο/Η {patient.FirstName} {patient.LastName} ζήτησε αλλαγή ραντεβού από {request.PreviousAppointmentDate:dd/MM/yyyy HH:mm} σε {request.RequestedAppointmentDate:dd/MM/yyyy HH:mm}. Λόγος: {request.Reason}";

            if (!string.IsNullOrWhiteSpace(doctor.UserId))
            {
                await _notificationService.CreateAsync(
                    doctor.UserId,
                    title,
                    message,
                    notificationLinkUrl);
            }

            await SendEmailSafeAsync(
                doctor.Email,
                "Νέο αίτημα αλλαγής ραντεβού - MyDoc",
                BuildEmailBody(
                    "Νέο αίτημα αλλαγής ραντεβού",
                    $"Ο/Η {patient.FirstName} {patient.LastName} ζήτησε αλλαγή ραντεβού από {request.PreviousAppointmentDate:dd/MM/yyyy HH:mm} σε {request.RequestedAppointmentDate:dd/MM/yyyy HH:mm}.",
                    appointment,
                    request.Reason,
                    emailLinkUrl));
        }

        public async Task NotifyPatientForRescheduleRequestApprovedAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            AppointmentRescheduleRequest request,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Το αίτημα αλλαγής εγκρίθηκε";

            var message =
                $"Το αίτημα αλλαγής ραντεβού εγκρίθηκε. Το ραντεβού σας με τον/την ιατρό {doctor.FirstName} {doctor.LastName} μεταφέρθηκε στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Το αίτημα αλλαγής ραντεβού εγκρίθηκε - MyDoc",
                BuildEmailBody(
                    "Το αίτημα αλλαγής ραντεβού εγκρίθηκε",
                    message,
                    appointment,
                    null,
                    emailLinkUrl));
        }

        public async Task NotifyPatientForRescheduleRequestRejectedAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            AppointmentRescheduleRequest request,
            string rejectionReason,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Το αίτημα αλλαγής απορρίφθηκε";

            var message =
                $"Το αίτημα αλλαγής ραντεβού για τις {request.RequestedAppointmentDate:dd/MM/yyyy HH:mm} απορρίφθηκε. Λόγος: {rejectionReason}";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Το αίτημα αλλαγής ραντεβού απορρίφθηκε - MyDoc",
                BuildEmailBody(
                    "Το αίτημα αλλαγής ραντεβού απορρίφθηκε",
                    $"Το αίτημα αλλαγής ραντεβού με τον/την ιατρό {doctor.FirstName} {doctor.LastName} απορρίφθηκε.",
                    appointment,
                    rejectionReason,
                    emailLinkUrl));
        }

        
        public async Task NotifyPatientForRescheduleRequestExpiredAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            AppointmentRescheduleRequest request,
            string patientMessage,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            const string title =
                "Ενημέρωση αιτήματος αλλαγής";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                patientMessage,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Ενημέρωση αιτήματος αλλαγής ραντεβού - MyDoc",
                BuildEmailBody(
                    title,
                    patientMessage,
                    appointment,
                    null,
                    emailLinkUrl));
        }

        public async Task NotifyPatientForAppointmentReminderAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            AppointmentReminderType reminderType,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                reminderType ==
                AppointmentReminderType.TwentyFourHours
                    ? "Υπενθύμιση ραντεβού"
                    : "Το ραντεβού σας είναι σύντομα";

            var message =
                reminderType ==
                AppointmentReminderType.TwentyFourHours
                    ? $"Υπενθύμιση: έχετε ραντεβού με τον/την ιατρό {doctor.FirstName} {doctor.LastName} στις {appointment.AppointmentDate:dd/MM/yyyy HH:mm}."
                    : $"Υπενθύμιση: το ραντεβού σας με τον/την ιατρό {doctor.FirstName} {doctor.LastName} είναι σήμερα στις {appointment.AppointmentDate:HH:mm}.";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                $"{title} - MyDoc",
                BuildEmailBody(
                    title,
                    message,
                    appointment,
                    null,
                    emailLinkUrl));
        }

        private async Task SendEmailSafeAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            try
            {
                await _emailSender.SendEmailAsync(
                    email,
                    subject,
                    htmlMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send appointment notification email to {Email}",
                    email);
            }
        }

        private static string BuildEmailBody(
            string heading,
            string message,
            Appointment appointment,
            string? reason,
            string? actionUrl)
        {
            var safeHeading =
                Encode(heading);

            var safeMessage =
                Encode(message);

            var safeReason =
                Encode(reason);

            var safeActionUrl =
                Encode(actionUrl);

            var reasonBlock =
                string.IsNullOrWhiteSpace(reason)
                    ? ""
                    : $"""
                      <p style="margin: 16px 0;">
                          <strong>Λόγος:</strong><br />
                          {safeReason}
                      </p>
                      """;

            var actionBlock =
                string.IsNullOrWhiteSpace(actionUrl)
                    ? ""
                    : $"""
                      <p style="margin: 24px 0;">
                          <a href="{safeActionUrl}"
                             style="background:#0ea5e9;color:#ffffff;padding:12px 18px;border-radius:8px;text-decoration:none;font-weight:600;">
                              Σύνδεση στο MyDoc
                          </a>
                      </p>
                      """;

            return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;color:#1f2d3d;line-height:1.6;">
                <h2 style="color:#123c84;">{safeHeading}</h2>

                <p>{safeMessage}</p>

                <p>
                    <strong>Ημερομηνία ραντεβού:</strong><br />
                    {appointment.AppointmentDate:dd/MM/yyyy HH:mm}
                </p>

                {reasonBlock}

                {actionBlock}

                <p style="margin-top:28px;color:#607086;font-size:14px;">
                    MyDoc
                </p>
            </div>
            """;
        }

        private static string Encode(
            string? value)
        {
            return HtmlEncoder.Default.Encode(
                value ?? string.Empty);
        }


        public async Task NotifyPatientForRescheduleRequestExpiredAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            AppointmentRescheduleRequest request,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Το αίτημα αλλαγής έληξε";

            var message =
                $"Το αίτημα αλλαγής για τις " +
                $"{request.RequestedAppointmentDate:dd/MM/yyyy HH:mm} έληξε. " +
                $"Δεν έγινε αλλαγή και το ραντεβού σας παραμένει προγραμματισμένο για " +
                $"{appointment.AppointmentDate:dd/MM/yyyy HH:mm}.";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Το αίτημα αλλαγής ραντεβού έληξε - MyDoc",
                BuildEmailBody(
                    title,
                    message,
                    appointment,
                    null,
                    emailLinkUrl));
        }

        public async Task NotifyPatientForRescheduleRequestUnavailableAsync(
            Doctor doctor,
            Patient patient,
            Appointment appointment,
            AppointmentRescheduleRequest request,
            string? notificationLinkUrl,
            string? emailLinkUrl)
        {
            var title =
                "Η νέα ώρα δεν είναι διαθέσιμη";

            var message =
                $"Η ώρα {request.RequestedAppointmentDate:dd/MM/yyyy HH:mm} " +
                $"δεν είναι πλέον διαθέσιμη. " +
                $"Το αίτημα αλλαγής έληξε και το ραντεβού σας παραμένει προγραμματισμένο για " +
                $"{appointment.AppointmentDate:dd/MM/yyyy HH:mm}.";

            await _notificationService.CreateAsync(
                patient.UserId,
                title,
                message,
                notificationLinkUrl);

            await SendEmailSafeAsync(
                patient.Email,
                "Η νέα ώρα δεν είναι διαθέσιμη - MyDoc",
                BuildEmailBody(
                    title,
                    message,
                    appointment,
                    null,
                    emailLinkUrl));
        }

    }
}