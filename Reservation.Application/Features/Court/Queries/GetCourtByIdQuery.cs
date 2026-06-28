using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Court.Queries
{
    public record CourtDetailsResult(
        int CourtId, string Name, string Location, string Description,
        decimal PricePerHour, bool IsIndoor, int SportId, string SportName
    );

    public record GetCourtByIdQuery(int Id) : IRequest<Result<CourtDetailsResult>>;

    public class GetCourtByIdQueryHandler : IRequestHandler<GetCourtByIdQuery, Result<CourtDetailsResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetCourtByIdQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<CourtDetailsResult>> Handle(GetCourtByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"court_{request.Id}";
            var cached = await cache.GetAsync<CourtDetailsResult>(cacheKey);
            if (cached != null)
                return Result<CourtDetailsResult>.Ok(cached);

            var court = uow.Courts.GetByIdWithSport(request.Id);
            if (court == null)
                return Result<CourtDetailsResult>.Fail($"Court with id {request.Id} not found.");

            var result = new CourtDetailsResult(
                court.CourtId, court.Name, court.Location, court.Description,
                court.PricePerHour, court.IsIndoor, court.SportId, court.Sport.Name
            );

            await cache.SetAsync(cacheKey, result);

            return Result<CourtDetailsResult>.Ok(result);
        }
    }
}