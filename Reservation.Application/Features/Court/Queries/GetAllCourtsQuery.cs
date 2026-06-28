using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Court.Queries
{
    public record GetAllCourtsQuery(string? Name, bool? IsIndoor, int? SportId) : IRequest<Result<List<CourtDetailsResult>>>;

    public class GetAllCourtsQueryHandler : IRequestHandler<GetAllCourtsQuery, Result<List<CourtDetailsResult>>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetAllCourtsQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<List<CourtDetailsResult>>> Handle(GetAllCourtsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"courts_{request.Name}_{request.IsIndoor}_{request.SportId}";
            var cached = await cache.GetAsync<List<CourtDetailsResult>>(cacheKey);
            if (cached != null)
                return Result<List<CourtDetailsResult>>.Ok(cached);

            var courts = uow.Courts.GetAllWithSport()
                .Where(c => string.IsNullOrWhiteSpace(request.Name) || c.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase))
                .Where(c => request.IsIndoor == null || c.IsIndoor == request.IsIndoor)
                .Where(c => request.SportId == null || request.SportId == 0 || c.SportId == request.SportId)
                .Select(c => new CourtDetailsResult(
                    c.CourtId, c.Name, c.Location, c.Description,
                    c.PricePerHour, c.IsIndoor, c.SportId, c.Sport.Name
                ))
                .ToList();

            await cache.SetAsync(cacheKey, courts);

            return Result<List<CourtDetailsResult>>.Ok(courts);
        }
    }
}