using Reservation.API.DTOs.Court;

namespace Reservation.API.DTOs.Sport
{
    public class SportDetailsDto
    {
        public int SportId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public List<CourtDto> Courts { get; set; } = new();
    }
}
