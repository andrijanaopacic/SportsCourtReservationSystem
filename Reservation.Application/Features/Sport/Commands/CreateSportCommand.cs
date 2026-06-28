using FluentValidation;
using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Sport.Commands
{
    public record CreateSportCommand(string Name, int MaxPlayers) : IRequest<Result<Domain.Models.Sport>>;

    public class CreateSportCommandHandler : IRequestHandler<CreateSportCommand, Result<Domain.Models.Sport>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly IValidator<CreateSportCommand> validator;

        public CreateSportCommandHandler(IUnitOfWork uow, ICacheService cache, IValidator<CreateSportCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public async Task<Result<Domain.Models.Sport>> Handle(CreateSportCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<Domain.Models.Sport>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var existing = uow.Sports.GetByName(request.Name);
            if (existing != null)
                return Result<Domain.Models.Sport>.Fail("Sport with this name already exists.");

            var sport = new Domain.Models.Sport
            {
                Name = request.Name,
                MaxPlayers = request.MaxPlayers
            };

            uow.Sports.Add(sport);
            uow.SaveChanges();

            await cache.RemoveByPatternAsync("sports*");
            await cache.RemoveByPatternAsync("sport_*");
            await cache.RemoveByPatternAsync("court_*");
            await cache.RemoveByPatternAsync("courts*");

            return Result<Domain.Models.Sport>.Ok(sport);
        }
    }
}