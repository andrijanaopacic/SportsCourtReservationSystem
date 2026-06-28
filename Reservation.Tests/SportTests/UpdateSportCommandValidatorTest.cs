using Reservation.Application.Features.Sport.Commands;
using Reservation.Application.Validators.Sport;
using Xunit;

namespace Reservation.Tests.SportTests
{
    public class UpdateSportCommandValidatorTests
    {
        private readonly UpdateSportCommandValidator _validator = new();

        [Fact]
        public void Id_WhenZeroOrLess_ReturnsValidationError()
        {
            var command = new UpdateSportCommand(0, "Tenis", 4);
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Id");
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var command = new UpdateSportCommand(1, "Tenis", 4);
            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }
    }
}