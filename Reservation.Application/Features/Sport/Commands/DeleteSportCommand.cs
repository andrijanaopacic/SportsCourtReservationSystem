using FluentValidation;
using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Sport.Commands
{
    public record DeleteSportCommand(int Id) : IRequest<Result<bool>>;

    public class DeleteSportCommandHandler : IRequestHandler<DeleteSportCommand, Result<bool>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly IValidator<DeleteSportCommand> validator;

        public DeleteSportCommandHandler(IUnitOfWork uow, ICacheService cache, IValidator<DeleteSportCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public async Task<Result<bool>> Handle(DeleteSportCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<bool>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var sport = uow.Sports.GetByIdWithCourts(request.Id);
            if (sport == null)
                return Result<bool>.Fail($"Sport with id {request.Id} not found.");

            if (sport.Courts.Any())
                return Result<bool>.Fail("Cannot delete sport because it has courts assigned to it. Please delete the courts first.");

            uow.Sports.Remove(sport);
            uow.SaveChanges();

            await cache.RemoveAsync($"sport_{request.Id}");
            await cache.RemoveByPatternAsync("sports*");
            await cache.RemoveByPatternAsync("sport_*");
            await cache.RemoveByPatternAsync("court_*");
            await cache.RemoveByPatternAsync("courts*");

            return Result<bool>.Ok(true);
        }
    }
}