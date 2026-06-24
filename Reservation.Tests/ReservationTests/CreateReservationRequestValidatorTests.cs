using Reservation.API.DTOs.Reservation;
using Reservation.API.Validators.Reservation;

namespace Reservation.Tests.ReservationTests
{
    public class CreateReservationRequestValidatorTests
    {
        private readonly CreateReservationRequestValidator _validator = new();

        private static CreateReservationRequest ValidRequest() => new()
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Items = new List<CreateReservationItemRequest>
            {
                new CreateReservationItemRequest { TimeSlotId = 1 }
            }
        };

        // ─── Items prazan ─────────────────────────────────────────────────

        [Fact]
        public void Items_WhenEmpty_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Items = new List<CreateReservationItemRequest>();

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Items");
        }

        [Fact]
        public void Items_WhenNull_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Items = null!;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Items");
        }

        // ─── Items previše ────────────────────────────────────────────────

        [Fact]
        public void Items_When11Items_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Items = Enumerable.Range(1, 11)
                .Select(i => new CreateReservationItemRequest { TimeSlotId = i })
                .ToList();

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Items");
        }

        [Fact]
        public void Items_WhenExactly10Items_PassesValidation()
        {
            var request = ValidRequest();
            request.Items = Enumerable.Range(1, 10)
                .Select(i => new CreateReservationItemRequest { TimeSlotId = i })
                .ToList();

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Items");
        }

        // ─── Duplikati ────────────────────────────────────────────────────

        [Fact]
        public void Items_WhenDuplicateTimeSlotId_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Items = new List<CreateReservationItemRequest>
            {
                new CreateReservationItemRequest { TimeSlotId = 5 },
                new CreateReservationItemRequest { TimeSlotId = 5 }
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Items");
        }

        [Fact]
        public void Items_WhenNoDuplicates_PassesValidation()
        {
            var request = ValidRequest();
            request.Items = new List<CreateReservationItemRequest>
            {
                new CreateReservationItemRequest { TimeSlotId = 1 },
                new CreateReservationItemRequest { TimeSlotId = 2 },
                new CreateReservationItemRequest { TimeSlotId = 3 }
            };

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Items");
        }

        // ─── Item TimeSlotId ──────────────────────────────────────────────

        [Fact]
        public void Item_TimeSlotId_WhenZero_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Items = new List<CreateReservationItemRequest>
            {
                new CreateReservationItemRequest { TimeSlotId = 0 }
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName.Contains("TimeSlotId"));
        }

        [Fact]
        public void Item_TimeSlotId_WhenNegative_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Items = new List<CreateReservationItemRequest>
            {
                new CreateReservationItemRequest { TimeSlotId = -3 }
            };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName.Contains("TimeSlotId"));
        }

        [Fact]
        public void Item_TimeSlotId_WhenPositive_PassesValidation()
        {
            var request = ValidRequest();
            request.Items = new List<CreateReservationItemRequest>
            {
                new CreateReservationItemRequest { TimeSlotId = 100 }
            };

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName.Contains("TimeSlotId"));
        }

        // ─── Valid request ────────────────────────────────────────────────

        [Fact]
        public void ValidRequest_PassesValidation()
        {
            var request = ValidRequest();
            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidRequest_WithMultipleDistinctSlots_PassesValidation()
        {
            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 },
                    new CreateReservationItemRequest { TimeSlotId = 2 },
                    new CreateReservationItemRequest { TimeSlotId = 3 }
                }
            };

            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }
    }
}
