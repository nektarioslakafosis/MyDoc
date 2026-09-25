using Microsoft.EntityFrameworkCore;
using MyDoc.Data;
using MyDoc.Models;

namespace MyDoc.Services
{
    public class SchedulingService
    {
        private readonly ApplicationDbContext _context;
        private const int SlotDurationMinutes = 30;
        private const int MinimumBookingLeadHours = 24;

        public SchedulingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TimeSpan>> GetAvailableSlotsAsync(int doctorId, DateTime date)
        {
            if (date.Date < DateTime.Today)
                return new List<TimeSpan>();

            var allSlots = await GenerateSlotsForDateAsync(doctorId, date);
            var bookedSlots = await GetBookedSlotsAsync(doctorId, date);
            var unavailablePeriods = await GetUnavailablePeriodsForDateAsync(doctorId, date);

            var earliestAllowedDateTime =
                DateTime.Now.AddHours(MinimumBookingLeadHours);

            return allSlots
                .Where(slot => !bookedSlots.Contains(slot))
                .Where(slot => !IsSlotInsideUnavailablePeriod(slot, unavailablePeriods))
                .Where(slot =>
                    date.Date.Add(slot) >=
                    earliestAllowedDateTime)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public async Task<List<TimeSpan>> GetBookedSlotsAsync(int doctorId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var bookedAppointments = await _context.Appointments
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.AppointmentDate >= dayStart &&
                    x.AppointmentDate < dayEnd &&
                    (x.Status == AppointmentStatus.Pending ||
                     x.Status == AppointmentStatus.Approved))
                .ToListAsync();

            return bookedAppointments
                .Select(x => x.AppointmentDate.TimeOfDay)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public async Task<List<CalendarSlotVM>> GetCalendarSlotsAsync(
            int doctorId,
            DateTime startDate,
            DateTime endDate)
        {
            var result = new List<CalendarSlotVM>();

            var today = DateTime.Today;

            var earliestAllowedDateTime =
                DateTime.Now.AddHours(MinimumBookingLeadHours);

            var currentDate = startDate.Date < today
                ? today
                : startDate.Date;

            var finalDate = endDate.Date;

            if (finalDate < today)
                return result;

            while (currentDate <= finalDate)
            {
                var allSlots = await GenerateSlotsForDateAsync(doctorId, currentDate);
                var bookedSlots = await GetBookedSlotsAsync(doctorId, currentDate);
                var unavailablePeriods = await GetUnavailablePeriodsForDateAsync(doctorId, currentDate);

                foreach (var slot in allSlots)
                {
                    var slotDateTime = currentDate.Add(slot);

                    if (slotDateTime < earliestAllowedDateTime)
                        continue;

                    result.Add(new CalendarSlotVM
                    {
                        Date = currentDate,
                        Time = slot,
                        IsBooked = bookedSlots.Contains(slot),
                        IsUnavailable = IsSlotInsideUnavailablePeriod(slot, unavailablePeriods)
                    });
                }

                currentDate = currentDate.AddDays(1);
            }

            return result
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Time)
                .ToList();
        }

        private async Task<List<DoctorUnavailablePeriod>> GetUnavailablePeriodsForDateAsync(
            int doctorId,
            DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            return await _context.DoctorUnavailablePeriods
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.Date >= dayStart &&
                    x.Date < dayEnd)
                .ToListAsync();
        }

        private bool IsSlotInsideUnavailablePeriod(
            TimeSpan slot,
            List<DoctorUnavailablePeriod> unavailablePeriods)
        {
            var slotStart = TimeOnly.FromTimeSpan(slot);
            var slotEnd = slotStart.AddMinutes(SlotDurationMinutes);

            return unavailablePeriods.Any(period =>
                slotStart < period.EndTime &&
                slotEnd > period.StartTime);
        }

        private async Task<List<TimeSpan>> GenerateSlotsForDateAsync(int doctorId, DateTime date)
        {
            var dayOfWeek = date.DayOfWeek;

            var availabilities = await _context.DoctorAvailabilities
                .Where(a =>
                    a.DoctorId == doctorId &&
                    a.DayOfWeek == dayOfWeek &&
                    a.IsActive)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            var slots = new List<TimeSpan>();

            foreach (var availability in availabilities)
            {
                if (availability.SlotDurationMinutes <= 0)
                    continue;

                var current = availability.StartTime;

                while (current.AddMinutes(availability.SlotDurationMinutes) <= availability.EndTime)
                {
                    slots.Add(current.ToTimeSpan());
                    current = current.AddMinutes(availability.SlotDurationMinutes);
                }
            }

            return slots
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
    }
}