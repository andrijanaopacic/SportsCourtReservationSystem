namespace Reservation.API.DTOs.Court
{
    public class CourtDto
    {
        public int CourtId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public bool IsIndoor { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; } = string.Empty;
    }
}
