namespace Reservation.API.DTOs.TimeSlot
{
    public class CreateTimeSlotRequest
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int CourtId { get; set; }
    }
}
