using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Sport.Queries
{
    public record GetAllSportsQuery(string? Name) : IRequest<Result<List<Domain.Models.Sport>>>;

    public class GetAllSportsQueryHandler : IRequestHandler<GetAllSportsQuery, Result<List<Domain.Models.Sport>>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        public GetAllSportsQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<List<Domain.Models.Sport>>> Handle(GetAllSportsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"sports_{request.Name}";
            var cached = await cache.GetAsync<List<Domain.Models.Sport>>(cacheKey);
            if (cached != null)
                return Result<List<Domain.Models.Sport>>.Ok(cached);

            var sports = uow.Sports.SearchByName(request.Name).ToList();

            await cache.SetAsync(cacheKey, sports);

            return Result<List<Domain.Models.Sport>>.Ok(sports);
        }
    }
}