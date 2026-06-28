using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Sport.Queries
{
    public record CourtSummary(int CourtId, string Name, string Location, decimal PricePerHour, bool IsIndoor, int SportId, string SportName);

    public record SportDetailsResult(int SportId, string Name, int MaxPlayers, int TotalCourtsCount, List<CourtSummary> Courts);

    public record GetSportByIdQuery(int Id) : IRequest<Result<SportDetailsResult>>;

    public class GetSportByIdQueryHandler : IRequestHandler<GetSportByIdQuery, Result<SportDetailsResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetSportByIdQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<SportDetailsResult>> Handle(GetSportByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"sport_{request.Id}";
            var cached = await cache.GetAsync<SportDetailsResult>(cacheKey);
            if (cached != null)
                return Result<SportDetailsResult>.Ok(cached);

            var sport = uow.Sports.GetByIdWithCourts(request.Id);
            if (sport == null)
                return Result<SportDetailsResult>.Fail($"Sport with id {request.Id} not found.");

            var result = new SportDetailsResult(
                sport.SportId,
                sport.Name,
                sport.MaxPlayers,
                sport.Courts.Count,
                sport.Courts.Select(c => new CourtSummary(
                    c.CourtId, c.Name, c.Location, c.PricePerHour, c.IsIndoor, c.SportId, sport.Name
                )).ToList()
            );

            await cache.SetAsync(cacheKey, result);

            return Result<SportDetailsResult>.Ok(result);
        }
    }
}