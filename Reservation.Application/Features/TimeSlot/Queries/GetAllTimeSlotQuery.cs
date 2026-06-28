using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.TimeSlot.Queries
{
    public record GetAllTimeSlotsQuery(bool? IsAvailable, decimal? MinPrice, decimal? MaxPrice) : IRequest<Result<List<TimeSlotResult>>>;

    public class GetAllTimeSlotsQueryHandler : IRequestHandler<GetAllTimeSlotsQuery, Result<List<TimeSlotResult>>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetAllTimeSlotsQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<List<TimeSlotResult>>> Handle(GetAllTimeSlotsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"timeslots:all:{request.IsAvailable}:{request.MinPrice}:{request.MaxPrice}";
            var cached = await cache.GetAsync<List<TimeSlotResult>>(cacheKey);
            if (cached != null)
                return Result<List<TimeSlotResult>>.Ok(cached);

            var slots = uow.TimeSlots.GetAll()
                .Where(t => request.IsAvailable == null || t.IsAvailable == request.IsAvailable)
                .Where(t => request.MinPrice == null || t.Price >= request.MinPrice)
                .Where(t => request.MaxPrice == null || t.Price <= request.MaxPrice)
                .Select(t => new TimeSlotResult(
                    t.TimeSlotId, t.Date, t.StartTime, t.EndTime, t.Duration,
                    t.Price, t.TotalPrice, t.IsAvailable, t.CourtId, t.Court?.Name ?? string.Empty))
                .ToList();

            await cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(10));

            return Result<List<TimeSlotResult>>.Ok(slots);
        }
    }
}