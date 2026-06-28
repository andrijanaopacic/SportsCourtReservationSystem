using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.TimeSlot.Queries
{
    public record GetTimeSlotsByCourtQuery(int CourtId) : IRequest<Result<List<TimeSlotResult>>>;

    public class GetTimeSlotsByCourtQueryHandler : IRequestHandler<GetTimeSlotsByCourtQuery, Result<List<TimeSlotResult>>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetTimeSlotsByCourtQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<List<TimeSlotResult>>> Handle(GetTimeSlotsByCourtQuery request, CancellationToken cancellationToken)
        {
            var court = uow.Courts.GetById(request.CourtId);
            if (court == null)
                return Result<List<TimeSlotResult>>.Fail($"Court with ID {request.CourtId} was not found.");

            var cacheKey = $"timeslots:court:{request.CourtId}";
            var cached = await cache.GetAsync<List<TimeSlotResult>>(cacheKey);
            if (cached != null)
                return Result<List<TimeSlotResult>>.Ok(cached);

            var slots = uow.TimeSlots.GetByCourt(request.CourtId)
                .Select(t => new TimeSlotResult(
                    t.TimeSlotId, t.Date, t.StartTime, t.EndTime, t.Duration,
                    t.Price, t.TotalPrice, t.IsAvailable, t.CourtId, court.Name))
                .ToList();

            await cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(10));

            return Result<List<TimeSlotResult>>.Ok(slots);
        }
    }
}