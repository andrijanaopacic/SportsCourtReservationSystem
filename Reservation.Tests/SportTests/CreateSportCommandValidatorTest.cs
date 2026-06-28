using Reservation.Application.Features.Sport.Commands;
using Reservation.Application.Validators.Sport;
using Xunit;

namespace Reservation.Tests.SportTests
{
    public class CreateSportCommandValidatorTests
    {
        private readonly CreateSportCommandValidator _validator = new();

        [Fact]
        public void Name_WhenEmpty_ReturnsValidationError()
        {
            var command = new CreateSportCommand("", 4);
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public void MaxPlayers_WhenZeroOrLess_ReturnsValidationError()
        {
            var command = new CreateSportCommand("Tenis", 0);
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "MaxPlayers");
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var command = new CreateSportCommand("Tenis", 4);
            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }
    }
}