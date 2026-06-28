using Reservation.Application.Features.Court.Commands;
using Reservation.Application.Validators.Court;
using Xunit;

namespace Reservation.Tests.CourtTests
{
    public class CreateCourtCommandValidatorTests
    {
        private readonly CreateCourtCommandValidator _validator = new();

        private CreateCourtCommand ValidCommand() => new("Teren 1", "Beograd", "Opis", 1500, true, 1);

        [Fact]
        public void Name_WhenEmpty_ReturnsValidationError()
        {
            var command = ValidCommand() with { Name = "" };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public void Location_WhenEmpty_ReturnsValidationError()
        {
            var command = ValidCommand() with { Location = "" };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Location");
        }

        [Fact]
        public void PricePerHour_WhenZeroOrLess_ReturnsValidationError()
        {
            var command = ValidCommand() with { PricePerHour = 0 };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "PricePerHour");
        }

        [Fact]
        public void SportId_WhenZeroOrLess_ReturnsValidationError()
        {
            var command = ValidCommand() with { SportId = 0 };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "SportId");
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var result = _validator.Validate(ValidCommand());
            Assert.True(result.IsValid);
        }
    }

    public class UpdateCourtCommandValidatorTests
    {
        private readonly UpdateCourtCommandValidator _validator = new();

        private UpdateCourtCommand ValidCommand() => new(1, "Teren 1", "Beograd", "Opis", 1500, true, 1);

        [Fact]
        public void Id_WhenZeroOrLess_ReturnsValidationError()
        {
            var command = ValidCommand() with { Id = 0 };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Id");
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var result = _validator.Validate(ValidCommand());
            Assert.True(result.IsValid);
        }
    }

    public class DeleteCourtCommandValidatorTests
    {
        private readonly DeleteCourtCommandValidator _validator = new();

        [Fact]
        public void Id_WhenZeroOrLess_ReturnsValidationError()
        {
            var result = _validator.Validate(new DeleteCourtCommand(0));
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var result = _validator.Validate(new DeleteCourtCommand(1));
            Assert.True(result.IsValid);
        }
    }
}