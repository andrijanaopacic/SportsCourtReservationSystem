using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Court.Commands
{
    public record DeleteCourtCommand(int Id) : IRequest<Result<bool>>;

    public class DeleteCourtCommandHandler : IRequestHandler<DeleteCourtCommand, Result<bool>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly FluentValidation.IValidator<DeleteCourtCommand> validator;

        public DeleteCourtCommandHandler(IUnitOfWork uow, ICacheService cache, FluentValidation.IValidator<DeleteCourtCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public async Task<Result<bool>> Handle(DeleteCourtCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<bool>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var court = uow.Courts.GetById(request.Id);
            if (court == null)
                return Result<bool>.Fail($"Court with id {request.Id} not found.");

            uow.Courts.Remove(court);
            uow.SaveChanges();

            await cache.RemoveByPatternAsync("sports*");
            await cache.RemoveByPatternAsync("sport_*");
            await cache.RemoveByPatternAsync("court_*");
            await cache.RemoveByPatternAsync("courts*");

            return Result<bool>.Ok(true);
        }
    }
}