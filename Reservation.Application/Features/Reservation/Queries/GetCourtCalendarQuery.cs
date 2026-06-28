using MediatR;
using Reservation.Application.Common;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Queries
{
    public record CourtCalendarDayResult(DateOnly Date, int ReservationCount);

    public record GetCourtCalendarQuery(int CourtId, int Year, int Month) : IRequest<Result<List<CourtCalendarDayResult>>>;

    public class GetCourtCalendarQueryHandler : IRequestHandler<GetCourtCalendarQuery, Result<List<CourtCalendarDayResult>>>
    {
        private readonly IUnitOfWork uow;

        public GetCourtCalendarQueryHandler(IUnitOfWork uow)
        {
            this.uow = uow;
        }

        public Task<Result<List<CourtCalendarDayResult>>> Handle(GetCourtCalendarQuery request, CancellationToken cancellationToken)
        {
            var startDate = new DateOnly(request.Year, request.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var reservations = uow.Reservations.GetAll()
                .Where(r => r.Status != ReservationStatus.CANCELLED)
                .Where(r => r.ReservationItems.Any(i =>
                    i.TimeSlot.CourtId == request.CourtId &&
                    i.TimeSlot.Date >= startDate &&
                    i.TimeSlot.Date <= endDate))
                .ToList();

            var grouped = reservations
                .SelectMany(r => r.ReservationItems)
                .Where(i =>
                    i.TimeSlot.CourtId == request.CourtId &&
                    i.TimeSlot.Date >= startDate &&
                    i.TimeSlot.Date <= endDate)
                .GroupBy(i => i.TimeSlot.Date)
                .Select(g => new CourtCalendarDayResult(g.Key, g.Count()))
                .ToList();

            return Task.FromResult(Result<List<CourtCalendarDayResult>>.Ok(grouped));
        }
    }
}