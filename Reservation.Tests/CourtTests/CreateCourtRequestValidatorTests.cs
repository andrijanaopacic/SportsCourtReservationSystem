using Reservation.API.DTOs.Court;
using Reservation.API.Validators.Court;
using Xunit;

namespace Reservation.Tests
{
    public class CreateCourtRequestValidatorTests
    {
        private readonly CreateCourtRequestValidator _validator = new();

        private CreateCourtRequest ValidRequest() => new()
        {
            Name = "Teren 1",
            Location = "Beograd",
            Description = "Opis terena",
            PricePerHour = 1500,
            IsIndoor = true,
            SportId = 1
        };

        [Fact]
        public void Name_WhenEmpty_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Name = "";

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public void Location_WhenEmpty_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Location = "";

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Location");
        }

        [Fact]
        public void PricePerHour_WhenZeroOrLess_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.PricePerHour = 0;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "PricePerHour");
        }

        [Fact]
        public void SportId_WhenZeroOrLess_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.SportId = 0;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "SportId");
        }

        [Fact]
        public void Description_WhenExceedsMaxLength_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Description = new string('a', 501);

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Description");
        }

        [Fact]
        public void Description_WhenEmpty_PassesValidation()
        {
            var request = ValidRequest();
            request.Description = "";

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Description");
        }

        [Fact]
        public void ValidRequest_PassesValidation()
        {
            var request = ValidRequest();
            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }
    }
}