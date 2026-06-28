using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Validators.Reservation;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class CreateReservationCommandValidatorTests
    {
        private readonly CreateReservationCommandValidator _validator = new();

        [Fact]
        public void Items_WhenEmpty_ReturnsValidationError()
        {
            var command = new CreateReservationCommand("user1", new List<ReservationItemInput>());
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Items_WhenMoreThanTen_ReturnsValidationError()
        {
            var items = Enumerable.Range(1, 11).Select(i => new ReservationItemInput(i)).ToList();
            var command = new CreateReservationCommand("user1", items);
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Items_WhenContainsDuplicates_ReturnsValidationError()
        {
            var command = new CreateReservationCommand("user1", new List<ReservationItemInput> { new(1), new(1) });
            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidCommand_PassesValidation()
        {
            var command = new CreateReservationCommand("user1", new List<ReservationItemInput> { new(1), new(2) });
            var result = _validator.Validate(command);

            Assert.True(result.IsValid);
        }
    }
}