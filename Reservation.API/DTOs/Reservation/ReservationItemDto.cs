namespace Reservation.API.DTOs.Reservation
{
    public class ReservationItemDto
    {
        public int RowNumber { get; set; }
        public decimal Price { get; set; }
        public DateOnly Date { get; set; }
        public int TimeSlotId { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string CourtName { get; set; } = string.Empty;
    }
}
