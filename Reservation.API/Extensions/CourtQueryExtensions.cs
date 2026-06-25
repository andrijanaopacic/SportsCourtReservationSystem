using Reservation.Domain.Models;

namespace Reservation.API.Extensions
{
    public static class CourtQueryExtensions
    {
        public static IEnumerable<Court> FilterByName(
            this IEnumerable<Court> courts, string? name) =>
            string.IsNullOrWhiteSpace(name) ? courts :
            courts.Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        public static IEnumerable<Court> FilterByIndoor(
            this IEnumerable<Court> courts, bool? isIndoor) =>
            isIndoor == null ? courts :
            courts.Where(c => c.IsIndoor == isIndoor);

        public static IEnumerable<Court> FilterBySport(
            this IEnumerable<Court> courts, int? sportId) =>
            sportId == null || sportId == 0 ? courts :
            courts.Where(c => c.SportId == sportId);
    }
}
