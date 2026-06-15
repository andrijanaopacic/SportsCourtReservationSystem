using Reservation.Domain.Models;

namespace Reservation.API.Extensions
{
    public static class TimeSlotQueryExtensions
    {
        public static IEnumerable<TimeSlot> FilterByAvailability(
            this IEnumerable<TimeSlot> slots, bool? isAvailable) =>
            isAvailable == null ? slots :
            slots.Where(s => s.IsAvailable == isAvailable);

        public static IEnumerable<TimeSlot> FilterByMinPrice(
            this IEnumerable<TimeSlot> slots, decimal? minPrice) =>
            minPrice == null ? slots :
            slots.Where(s => s.Price >= minPrice);

        public static IEnumerable<TimeSlot> FilterByMaxPrice(
            this IEnumerable<TimeSlot> slots, decimal? maxPrice) =>
            maxPrice == null ? slots :
            slots.Where(s => s.Price <= maxPrice);
    }
}
