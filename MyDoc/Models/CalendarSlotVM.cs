namespace MyDoc.Models
{
    public class CalendarSlotVM
    {
        public DateTime Date { get; set; }

        public TimeSpan Time { get; set; }

        public bool IsBooked { get; set; }

        public bool IsUnavailable { get; set; }

        public bool IsAvailable => !IsBooked && !IsUnavailable;

        public DateTime StartDateTime => Date.Date.Add(Time);
    }
}
