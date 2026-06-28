using Reservation.Application.Features.Sport.Commands;
using Reservation.Application.Validators.Sport;
using Xunit;

namespace Reservation.Tests.SportTests
{
    public class DeleteSportCommandValidatorTests
    {
        private readonly DeleteSportCommandValidator _validator = new();

        [Fact]
        public void Id_WhenZeroOrLess_ReturnsValidationError()
        {
            var command = new DeleteSportCommand(0);
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var command = new DeleteSportCommand(1);
            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }
    }
}