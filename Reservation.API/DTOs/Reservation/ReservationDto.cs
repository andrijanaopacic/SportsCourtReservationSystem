namespace Reservation.API.DTOs.Reservation
{
    public class ReservationDto
    {
        public int ReservationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public DateOnly Date {  get; set; }
        public List<ReservationItemDto> Items { get; set; } = new();
    }
}
