using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Court.Commands
{
    public record UpdateCourtCommand(
        int Id,
        string Name,
        string Location,
        string Description,
        decimal PricePerHour,
        bool IsIndoor,
        int SportId
    ) : IRequest<Result<Domain.Models.Court>>;

    public class UpdateCourtCommandHandler : IRequestHandler<UpdateCourtCommand, Result<Domain.Models.Court>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly FluentValidation.IValidator<UpdateCourtCommand> validator;

        public UpdateCourtCommandHandler(IUnitOfWork uow, ICacheService cache, FluentValidation.IValidator<UpdateCourtCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public async Task<Result<Domain.Models.Court>> Handle(UpdateCourtCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<Domain.Models.Court>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var court = uow.Courts.GetById(request.Id);
            if (court == null)
                return Result<Domain.Models.Court>.Fail($"Court with id {request.Id} not found.");

            var sport = uow.Sports.GetById(request.SportId);
            if (sport == null)
                return Result<Domain.Models.Court>.Fail($"Sport with id {request.SportId} not found.");

            court.Name = request.Name;
            court.Location = request.Location;
            court.Description = request.Description;
            court.PricePerHour = request.PricePerHour;
            court.IsIndoor = request.IsIndoor;
            court.SportId = request.SportId;

            uow.Courts.Update(court);
            uow.SaveChanges();

            await cache.RemoveByPatternAsync("sports*");
            await cache.RemoveByPatternAsync("sport_*");
            await cache.RemoveByPatternAsync("court_*");
            await cache.RemoveByPatternAsync("courts*");

            return Result<Domain.Models.Court>.Ok(court);
        }
    }
}