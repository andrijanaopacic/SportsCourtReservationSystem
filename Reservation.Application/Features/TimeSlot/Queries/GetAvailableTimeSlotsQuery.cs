using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.TimeSlot.Queries
{
    public record GetAvailableTimeSlotsQuery() : IRequest<Result<List<TimeSlotResult>>>;

    public class GetAvailableTimeSlotsQueryHandler : IRequestHandler<GetAvailableTimeSlotsQuery, Result<List<TimeSlotResult>>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetAvailableTimeSlotsQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<List<TimeSlotResult>>> Handle(GetAvailableTimeSlotsQuery request, CancellationToken cancellationToken)
        {
            const string cacheKey = "timeslots:available";
            var cached = await cache.GetAsync<List<TimeSlotResult>>(cacheKey);
            if (cached != null)
                return Result<List<TimeSlotResult>>.Ok(cached);

            var slots = uow.TimeSlots.GetAll()
                .Where(t => t.IsAvailable)
                .Select(t => new TimeSlotResult(
                    t.TimeSlotId, t.Date, t.StartTime, t.EndTime, t.Duration,
                    t.Price, t.TotalPrice, t.IsAvailable, t.CourtId, t.Court?.Name ?? string.Empty))
                .ToList();

            await cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(10));

            return Result<List<TimeSlotResult>>.Ok(slots);
        }
    }
}