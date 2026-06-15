namespace Reservation.API.DTOs.Reservation
{
    public class CreateReservationItemRequest
    {
        public int TimeSlotId { get; set; }
        public DateOnly Date { get; set; }
    }

    public class CreateReservationRequest
    {
        public List<CreateReservationItemRequest> Items { get; set; } = new();
    }
}
