using Reservation.API.DTOs.TimeSlot;
using Reservation.API.Validators.TimeSlot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Tests.TimeSlotTests
{
    public class CreateTimeSlotRequestValidatorTests
    {
        private readonly CreateTimeSlotRequestValidator _validator = new();

        private CreateTimeSlotRequest ValidRequest() => new()
        {
            CourtId = 1,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            Price = 1500,
            IsAvailable = true
        };

        [Fact]
        public void CourtId_WhenZero_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.CourtId = 0;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "CourtId");
        }

        [Fact]
        public void CourtId_WhenNegative_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.CourtId = -1;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "CourtId");
        }

        [Fact]
        public void Date_WhenDefault_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Date = default;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Date");
        }

        [Fact]
        public void Date_WhenInThePast_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Date");
        }

        [Fact]
        public void Date_WhenToday_PassesValidation()
        {
            var request = ValidRequest();
            request.Date = DateOnly.FromDateTime(DateTime.Today);

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Date");
        }

        [Fact]
        public void Date_WhenInTheFuture_PassesValidation()
        {
            var request = ValidRequest();
            request.Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Date");
        }

        [Fact]
        public void StartTime_WhenMinutesNotZero_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.StartTime = new TimeOnly(9, 30);

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "StartTime");
        }

        [Fact]
        public void StartTime_WhenOnFullHour_PassesValidation()
        {
            var request = ValidRequest();
            request.StartTime = new TimeOnly(10, 0);

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "StartTime");
        }

        [Fact]
        public void EndTime_WhenMinutesNotZero_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.EndTime = new TimeOnly(11, 45);

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EndTime");
        }

        [Fact]
        public void EndTime_WhenBeforeStartTime_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.StartTime = new TimeOnly(11, 0);
            request.EndTime = new TimeOnly(9, 0);

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EndTime");
        }

        [Fact]
        public void EndTime_WhenEqualToStartTime_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.StartTime = new TimeOnly(9, 0);
            request.EndTime = new TimeOnly(9, 0);

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EndTime");
        }

        [Fact]
        public void EndTime_WhenMoreThan8HoursAfterStart_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.StartTime = new TimeOnly(8, 0);
            request.EndTime = new TimeOnly(17, 0); 

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EndTime");
        }

        [Fact]
        public void EndTime_WhenExactly8HoursAfterStart_PassesValidation()
        {
            var request = ValidRequest();
            request.StartTime = new TimeOnly(8, 0);
            request.EndTime = new TimeOnly(16, 0); 

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "EndTime");
        }

        [Fact]
        public void Price_WhenZero_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Price = 0;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Price");
        }

        [Fact]
        public void Price_WhenNegative_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Price = -100;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Price");
        }

        [Fact]
        public void Price_WhenExceedsMaximum_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Price = 100001;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Price");
        }

        [Fact]
        public void Price_WhenAtMaximum_PassesValidation()
        {
            var request = ValidRequest();
            request.Price = 100000;

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Price");
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
