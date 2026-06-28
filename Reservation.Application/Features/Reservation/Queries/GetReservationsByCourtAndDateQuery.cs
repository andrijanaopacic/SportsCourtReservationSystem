using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Queries
{
    public record GetReservationsByCourtAndDateQuery(int CourtId, DateOnly Date) : IRequest<Result<List<ReservationResult>>>;

    public class GetReservationsByCourtAndDateQueryHandler : IRequestHandler<GetReservationsByCourtAndDateQuery, Result<List<ReservationResult>>>
    {
        private readonly IUnitOfWork uow;

        public GetReservationsByCourtAndDateQueryHandler(IUnitOfWork uow)
        {
            this.uow = uow;
        }

        public Task<Result<List<ReservationResult>>> Handle(GetReservationsByCourtAndDateQuery request, CancellationToken cancellationToken)
        {
            var reservations = uow.Reservations.GetAll()
                .Where(r => r.Status != ReservationStatus.CANCELLED)
                .Where(r => r.ReservationItems.Any(i => i.TimeSlot.CourtId == request.CourtId && i.TimeSlot.Date == request.Date))
                .ToList();

            var result = reservations.Select(CreateReservationCommandHandler.MapToResult).ToList();

            return Task.FromResult(Result<List<ReservationResult>>.Ok(result));
        }
    }
}