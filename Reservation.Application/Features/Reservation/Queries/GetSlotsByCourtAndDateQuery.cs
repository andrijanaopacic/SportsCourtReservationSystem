using MediatR;
using Reservation.Application.Common;
using Reservation.Domain.Repositories;
using DomainModels = Reservation.Domain.Models;

namespace Reservation.Application.Features.Reservation.Queries
{
    public record GetSlotsByCourtAndDateQuery(int CourtId, DateOnly Date) : IRequest<Result<List<DomainModels.TimeSlot>>>;

    public class GetSlotsByCourtAndDateQueryHandler : IRequestHandler<GetSlotsByCourtAndDateQuery, Result<List<DomainModels.TimeSlot>>>
    {
        private readonly IUnitOfWork uow;

        public GetSlotsByCourtAndDateQueryHandler(IUnitOfWork uow)
        {
            this.uow = uow;
        }

        public Task<Result<List<DomainModels.TimeSlot>>> Handle(GetSlotsByCourtAndDateQuery request, CancellationToken cancellationToken)
        {
            var slots = uow.TimeSlots.GetAll()
                .Where(s => s.CourtId == request.CourtId && s.Date == request.Date)
                .ToList();

            return Task.FromResult(Result<List<DomainModels.TimeSlot>>.Ok(slots));
        }
    }
}