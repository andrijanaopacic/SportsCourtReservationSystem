using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Validators.TimeSlot;
using Xunit;

namespace Reservation.Tests.TimeSlotTests
{
    public class CreateTimeSlotCommandValidatorTests
    {
        private readonly CreateTimeSlotCommandValidator _validator = new();

        private CreateTimeSlotCommand ValidCommand() =>
            new(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(10, 0), 1);

        [Fact]
        public void CourtId_WhenZeroOrLess_ReturnsValidationError()
        {
            var command = ValidCommand() with { CourtId = 0 };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "CourtId");
        }

        [Fact]
        public void Date_WhenInThePast_ReturnsValidationError()
        {
            var command = ValidCommand() with { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Date");
        }

        [Fact]
        public void StartTime_WhenNotOnFullHour_ReturnsValidationError()
        {
            var command = ValidCommand() with { StartTime = new TimeOnly(9, 30) };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "StartTime");
        }

        [Fact]
        public void EndTime_WhenBeforeStartTime_ReturnsValidationError()
        {
            var command = ValidCommand() with { StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(9, 0) };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EndTime");
        }

        [Fact]
        public void EndTime_WhenSlotExceeds8Hours_ReturnsValidationError()
        {
            var command = ValidCommand() with { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) };
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EndTime");
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var result = _validator.Validate(ValidCommand());
            Assert.True(result.IsValid);
        }
    }

    public class UpdateTimeSlotCommandValidatorTests
    {
        private readonly UpdateTimeSlotCommandValidator _validator = new();

        private UpdateTimeSlotCommand ValidCommand() =>
            new(1, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(10, 0), 1, true);

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

    public class DeleteTimeSlotCommandValidatorTests
    {
        private readonly DeleteTimeSlotCommandValidator _validator = new();

        [Fact]
        public void Id_WhenZeroOrLess_ReturnsValidationError()
        {
            var result = _validator.Validate(new DeleteTimeSlotCommand(0));
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var result = _validator.Validate(new DeleteTimeSlotCommand(1));
            Assert.True(result.IsValid);
        }
    }
}