using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Extensions;
using MyDoc.Models;
using MyDoc.Services;

namespace MyDoc.Controllers
{
    public class HomeController : Controller
    {
        private const int HomeDoctorPageSize = 4;

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SchedulingService _schedulingService;
        private readonly AppointmentLifecycleService _appointmentLifecycleService;
        private readonly IEmailSender _emailSender;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SchedulingService schedulingService,
            AppointmentLifecycleService appointmentLifecycleService,
            IEmailSender emailSender)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _schedulingService = schedulingService;
            _appointmentLifecycleService = appointmentLifecycleService;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? specialty)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Patient"))
                {
                    return RedirectToAction(
                        "Index",
                        "PatientDashboard");
                }

                if (User.IsInRole("Doctor"))
                {
                    return RedirectToAction(
                        "Index",
                        "DoctorDashboard");
                }

                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "AdminDashboard");
                }
            }

            await _appointmentLifecycleService
                .ExpirePastAppointmentsAsync();

            var vm = new HomeIndexVM
            {
                Search = search,
                Specialty = specialty
            };

            vm.Specialties =
                await _context.Doctors
                    .Where(d => d.IsActive)
                    .Select(d => d.Specialty)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

            var doctorsQuery =
                _context.Doctors
                    .Where(d => d.IsActive)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                doctorsQuery =
                    doctorsQuery.Where(d =>
                        d.FirstName.Contains(term) ||
                        d.LastName.Contains(term) ||
                        d.Specialty.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(specialty))
            {
                doctorsQuery =
                    doctorsQuery.Where(d =>
                        d.Specialty == specialty);
            }

            var doctors =
                await doctorsQuery
                    .OrderBy(d => d.LastName)
                    .ThenBy(d => d.FirstName)
                    .Take(HomeDoctorPageSize)
                    .ToListAsync();

            foreach (var doctor in doctors)
            {
                vm.Doctors.Add(
                    new HomeDoctorPreviewVM
                    {
                        Id = doctor.Id,

                        FullName =
                            $"{doctor.FirstName} {doctor.LastName}",

                        Specialty =
                            doctor.Specialty,

                        NextAvailableSlot =
                            await GetNextAvailableSlotAsync(
                                doctor.Id,
                                14)
                    });
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> SearchDoctors(
            string? search,
            string? specialty,
            int page = 1)
        {
            if (page < 1)
                page = 1;

            var doctorsQuery =
                _context.Doctors
                    .Where(d => d.IsActive)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term =
                    search.Trim();

                doctorsQuery =
                    doctorsQuery.Where(d =>
                        d.FirstName.Contains(term) ||
                        d.LastName.Contains(term) ||
                        d.Specialty.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(specialty))
            {
                doctorsQuery =
                    doctorsQuery.Where(d =>
                        d.Specialty == specialty);
            }

            var hasFilters =
                !string.IsNullOrWhiteSpace(search) ||
                !string.IsNullOrWhiteSpace(specialty);

            var totalResults =
                await doctorsQuery.CountAsync();

            var totalPages =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        totalResults /
                        (double)HomeDoctorPageSize));

            if (page > totalPages)
                page = totalPages;

            var doctors =
                await doctorsQuery
                    .OrderBy(d => d.LastName)
                    .ThenBy(d => d.FirstName)
                    .Skip(
                        (page - 1) *
                        HomeDoctorPageSize)
                    .Take(HomeDoctorPageSize)
                    .ToListAsync();

            var results =
                new List<object>();

            foreach (var doctor in doctors)
            {
                var nextAvailableSlot =
                    await GetNextAvailableSlotAsync(
                        doctor.Id,
                        14);

                results.Add(new
                {
                    id = doctor.Id,

                    fullName =
                        $"{doctor.FirstName} {doctor.LastName}",

                    specialty =
                        doctor.Specialty,

                    nextAvailableSlot =
                        nextAvailableSlot?
                            .ToString(
                                "dd/MM/yyyy HH:mm")
                });
            }

            return Json(new
            {
                doctors = results,
                page,
                totalPages,
                totalResults,

                hasPrevious =
                    hasFilters &&
                    page > 1,

                hasNext =
                    hasFilters &&
                    page < totalPages
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var vm =
                new ContactFormVM();

            if (User.Identity?.IsAuthenticated == true)
            {
                var user =
                    await _userManager
                        .GetUserAsync(User);

                if (user != null)
                {
                    vm.Email =
                        user.Email ??
                        string.Empty;

                    if (User.IsInRole("Patient"))
                    {
                        var patient =
                            await _context.Patients
                                .FirstOrDefaultAsync(p =>
                                    p.UserId ==
                                    user.Id);

                        if (patient != null)
                        {
                            vm.FullName =
                                $"{patient.FirstName} {patient.LastName}"
                                    .Trim();
                        }
                    }
                    else if (User.IsInRole("Doctor"))
                    {
                        var doctor =
                            await _context.Doctors
                                .FirstOrDefaultAsync(d =>
                                    d.UserId ==
                                    user.Id);

                        if (doctor != null)
                        {
                            vm.FullName =
                                $"{doctor.FirstName} {doctor.LastName}"
                                    .Trim();
                        }
                    }
                    else if (User.IsInRole("Admin"))
                    {
                        vm.FullName =
                            user.Email ??
                            string.Empty;
                    }
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(
            ContactFormVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            const string supportEmail =
                "support@mydoc.com";

            var safeName =
                WebUtility.HtmlEncode(
                    vm.FullName.Trim());

            var safeEmail =
                WebUtility.HtmlEncode(
                    vm.Email.Trim());

            var safeSubject =
                WebUtility.HtmlEncode(
                    vm.Subject.Trim());

            var safeMessage =
                WebUtility.HtmlEncode(
                        vm.Message.Trim())
                    .Replace(
                        "\n",
                        "<br />");

            var emailSubject =
                $"MyDoc Support Request - {vm.Subject.Trim()}";

            var emailBody = $@"
                <h2>Νέο μήνυμα επικοινωνίας από το MyDoc</h2>

                <p><strong>Όνομα:</strong> {safeName}</p>
                <p><strong>Email:</strong> {safeEmail}</p>
                <p><strong>Θέμα:</strong> {safeSubject}</p>

                <hr />

                <p><strong>Μήνυμα:</strong></p>
                <p>{safeMessage}</p>
            ";

            try
            {
                await _emailSender.SendEmailAsync(
                    supportEmail,
                    emailSubject,
                    emailBody);

                TempData.FlashSuccess(
                    "Το μήνυμά σας στάλθηκε επιτυχώς. Θα επικοινωνήσουμε μαζί σας σύντομα.");

                return RedirectToAction(
                    nameof(Contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Contact form email failed.");

                ModelState.AddModelError(
                    "",
                    "Δεν ήταν δυνατή η αποστολή του μηνύματος. Δοκιμάστε ξανά αργότερα.");

                return View(vm);
            }
        }

        private async Task<DateTime?>
            GetNextAvailableSlotAsync(
                int doctorId,
                int daysToCheck)
        {
            for (var i = 0;
                 i <= daysToCheck;
                 i++)
            {
                var date =
                    DateTime.Today
                        .AddDays(i);

                var slots =
                    await _schedulingService
                        .GetAvailableSlotsAsync(
                            doctorId,
                            date);

                var firstSlot =
                    slots.FirstOrDefault();

                if (firstSlot != default)
                    return date.Add(firstSlot);
            }

            return null;
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}