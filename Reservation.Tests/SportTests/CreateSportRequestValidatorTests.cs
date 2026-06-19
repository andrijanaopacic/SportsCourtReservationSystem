using Reservation.API.DTOs.Sport;
using Reservation.API.Validators.Sport;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Tests.SportTests
{
    public class CreateSportRequestValidatorTests
    {
        private readonly CreateSportRequestValidator _validator = new();

        [Fact]
        public void Name_WhenEmpty_ReturnsValidationError()
        {
            var request = new CreateSportRequest { Name = "", MaxPlayers = 4 };
            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public void Name_WhenExceedsMaxLength_ReturnsValidationError()
        {
            var request = new CreateSportRequest { Name = new string('a', 51), MaxPlayers = 4 };
            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public void MaxPlayers_WhenZero_ReturnsValidationError()
        {
            var request = new CreateSportRequest { Name = "Tenis", MaxPlayers = 0 };
            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "MaxPlayers");
        }

        [Fact]
        public void MaxPlayers_WhenNegative_ReturnsValidationError()
        {
            var request = new CreateSportRequest { Name = "Tenis", MaxPlayers = -1 };
            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "MaxPlayers");
        }


        [Fact]
        public void ValidRequest_PassesValidation()
        {
            var request = new CreateSportRequest { Name = "Tenis", MaxPlayers = 4 };
            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }

    }
}
