using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Court.Commands
{
    public record CreateCourtCommand(
        string Name,
        string Location,
        string Description,
        decimal PricePerHour,
        bool IsIndoor,
        int SportId
    ) : IRequest<Result<Domain.Models.Court>>;

    public class CreateCourtCommandHandler : IRequestHandler<CreateCourtCommand, Result<Domain.Models.Court>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly FluentValidation.IValidator<CreateCourtCommand> validator;

        public CreateCourtCommandHandler(IUnitOfWork uow, ICacheService cache, FluentValidation.IValidator<CreateCourtCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public async Task<Result<Domain.Models.Court>> Handle(CreateCourtCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<Domain.Models.Court>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var sport = uow.Sports.GetById(request.SportId);
            if (sport == null)
                return Result<Domain.Models.Court>.Fail($"Sport with id {request.SportId} not found.");

            var court = new Domain.Models.Court
            {
                Name = request.Name,
                Location = request.Location,
                Description = request.Description,
                PricePerHour = request.PricePerHour,
                IsIndoor = request.IsIndoor,
                SportId = request.SportId
            };

            uow.Courts.Add(court);
            uow.SaveChanges();

            await cache.RemoveByPatternAsync("sports*");
            await cache.RemoveByPatternAsync("sport_*");
            await cache.RemoveByPatternAsync("court_*");
            await cache.RemoveByPatternAsync("courts*");

            return Result<Domain.Models.Court>.Ok(court);
        }
    }
}