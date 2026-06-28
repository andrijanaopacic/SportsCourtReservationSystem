using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.TimeSlot.Queries
{
    public record GetTimeSlotByIdQuery(int Id) : IRequest<Result<TimeSlotResult>>;

    public class GetTimeSlotByIdQueryHandler : IRequestHandler<GetTimeSlotByIdQuery, Result<TimeSlotResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetTimeSlotByIdQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<TimeSlotResult>> Handle(GetTimeSlotByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"timeslots:id:{request.Id}";
            var cached = await cache.GetAsync<TimeSlotResult>(cacheKey);
            if (cached != null)
                return Result<TimeSlotResult>.Ok(cached);

            var slot = uow.TimeSlots.GetByIdWithCourt(request.Id);
            if (slot == null)
                return Result<TimeSlotResult>.Fail($"Time slot with ID {request.Id} was not found.");

            var result = new TimeSlotResult(
                slot.TimeSlotId, slot.Date, slot.StartTime, slot.EndTime, slot.Duration,
                slot.Price, slot.TotalPrice, slot.IsAvailable, slot.CourtId, slot.Court?.Name ?? string.Empty);

            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

            return Result<TimeSlotResult>.Ok(result);
        }
    }
}