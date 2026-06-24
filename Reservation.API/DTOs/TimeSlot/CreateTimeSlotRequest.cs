namespace Reservation.API.DTOs.TimeSlot
{
    public class CreateTimeSlotRequest
    {
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int CourtId { get; set; }
    }
}
