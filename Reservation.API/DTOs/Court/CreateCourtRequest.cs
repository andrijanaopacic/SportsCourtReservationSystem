namespace Reservation.API.DTOs.Court
{
    public class CreateCourtRequest
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public decimal PricePerHour { get; set; }
        public bool IsIndoor { get; set; }
        public int SportId { get; set; } 
    }
}
