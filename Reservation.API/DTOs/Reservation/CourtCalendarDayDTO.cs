namespace Reservation.API.DTOs.Reservation
{
    public class CourtCalendarDayDTO
    {
        public DateOnly Date { get; set; }
        public int ReservationCount { get; set; }

    }
}
