namespace Reservation.Domain.Models
{
    public class ReservationItem
    {
        public int RowNumber { get; set; }
        public decimal Price { get; set; }
        public DateOnly Date { get; set;  }

        public int ReservationId { get; set; }
        public ReservationEntity Reservation { get; set; } = null!;

        public int TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; } = null!;

    }

}
