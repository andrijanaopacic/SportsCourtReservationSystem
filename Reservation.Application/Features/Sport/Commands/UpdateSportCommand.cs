using FluentValidation;
using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Sport.Commands
{
    public record UpdateSportCommand(int Id, string Name, int MaxPlayers) : IRequest<Result<Domain.Models.Sport>>;

    public class UpdateSportCommandHandler : IRequestHandler<UpdateSportCommand, Result<Domain.Models.Sport>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly IValidator<UpdateSportCommand> validator;

        public UpdateSportCommandHandler(IUnitOfWork uow, ICacheService cache, IValidator<UpdateSportCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public async Task<Result<Domain.Models.Sport>> Handle(UpdateSportCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<Domain.Models.Sport>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var sport = uow.Sports.GetById(request.Id);
            if (sport == null)
                return Result<Domain.Models.Sport>.Fail($"Sport with id {request.Id} not found.");

            sport.Name = request.Name;
            sport.MaxPlayers = request.MaxPlayers;

            uow.Sports.Update(sport);
            uow.SaveChanges();

            await cache.RemoveAsync($"sport_{request.Id}");
            await cache.RemoveByPatternAsync("sports*");
            await cache.RemoveByPatternAsync("sport_*");
            await cache.RemoveByPatternAsync("court_*");
            await cache.RemoveByPatternAsync("courts*");

            return Result<Domain.Models.Sport>.Ok(sport);
        }
    }
}