namespace Reservation.API.DTOs.TimeSlot
{
    public class TimeSlotDto
    {
        public int TimeSlotId { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
    }
}
