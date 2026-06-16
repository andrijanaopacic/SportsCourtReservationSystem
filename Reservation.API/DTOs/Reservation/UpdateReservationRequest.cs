namespace Reservation.API.DTOs.Reservation
{
    public class UpdateReservationRequest
    {
        public string Status { get; set; } = string.Empty;
        public List<CreateReservationItemRequest> Items { get; set; } = new();
    }
}