namespace Reservation.API.DTOs.TimeSlot
{
    public class TimeSlotDto
    {
        public int TimeSlotId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsAvailable { get; set; }
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
    }
}
