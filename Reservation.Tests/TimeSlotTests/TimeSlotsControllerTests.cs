using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Reservation.API.Controllers;
using Reservation.API.DTOs.TimeSlot;
using Reservation.API.Extensions;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Tests.TimeSlotTests
{
    public class TimeSlotsControllerTests
    {
        private readonly Mock<ITimeSlotRepository> _timeSlotsRepoMock;
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateTimeSlotRequest>> _validatorMock;
        private readonly TimeSlotsController _controller;

        public TimeSlotsControllerTests()
        {
            _timeSlotsRepoMock = new Mock<ITimeSlotRepository>();
            _courtsRepoMock = new Mock<ICourtRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_timeSlotsRepoMock.Object);
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<TimeSlotDto>(It.IsAny<string>())).ReturnsAsync((TimeSlotDto?)null);
            _cacheMock.Setup(c => c.GetAsync<List<TimeSlotDto>>(It.IsAny<string>())).ReturnsAsync((List<TimeSlotDto>?)null);

            _validatorMock = new Mock<IValidator<CreateTimeSlotRequest>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateTimeSlotRequest>())).Returns(new ValidationResult());

            _controller = new TimeSlotsController(_uowMock.Object, _validatorMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            _timeSlotsRepoMock.Setup(r => r.GetByIdWithCourt(42)).Returns((TimeSlot?)null);

            var result = await _controller.GetById(42);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsOkWithTimeSlotDto()
        {
            var court = new Court { CourtId = 1, Name = "Tenis Court" };
            var slot = new TimeSlot
            {
                TimeSlotId = 1,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                Duration = TimeSpan.FromHours(2),
                Price = 1500,
                TotalPrice = 3000,
                IsAvailable = true,
                CourtId = 1,
                Court = court
            };
            _timeSlotsRepoMock.Setup(r => r.GetByIdWithCourt(1)).Returns(slot);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<TimeSlotDto>(ok.Value);
            Assert.Equal(new DateOnly(2026, 7, 1), dto.Date);
            Assert.Equal(new TimeOnly(9, 0), dto.StartTime);
            Assert.Equal(new TimeOnly(11, 0), dto.EndTime);
            Assert.Equal(1500, dto.Price);
            Assert.Equal(3000, dto.TotalPrice);
            Assert.Equal("Tenis Court", dto.CourtName);
        }

        [Fact]
        public async Task GetByCourt_WhenCourtNotFound_ReturnsNotFound()
        {
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _controller.GetByCourt(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetByCourt_WhenFound_ReturnsOkWithSlots()
        {
            var court = new Court { CourtId = 1, Name = "Padel Arena" };
            var slots = new List<TimeSlot>
            {
                new() { TimeSlotId = 1, CourtId = 1, Court = court, Date = new DateOnly(2026, 7, 1), StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0), Price = 1000, TotalPrice = 2000 },
                new() { TimeSlotId = 2, CourtId = 1, Court = court, Date = new DateOnly(2026, 7, 1), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0), Price = 1000, TotalPrice = 2000 },
            };
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _timeSlotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(slots);

            var result = await _controller.GetByCourt(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<TimeSlotDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task Create_WhenValidationFails_ReturnsBadRequest()
        {
            var request = new CreateTimeSlotRequest { CourtId = 0 };
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("CourtId", "CourtId must be greater than 0.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateTimeSlotRequest>())).Returns(failedValidation);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            _timeSlotsRepoMock.Verify(r => r.Add(It.IsAny<TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenCourtNotFound_ReturnsNotFound()
        {
            var request = new CreateTimeSlotRequest
            {
                CourtId = 99,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                Price = 1500
            };
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _controller.Create(request);

            Assert.IsType<NotFoundObjectResult>(result);
            _timeSlotsRepoMock.Verify(r => r.Add(It.IsAny<TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenOverlappingSlotExists_ReturnsBadRequest()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var request = new CreateTimeSlotRequest
            {
                CourtId = 1,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                Price = 1500
            };
            var existingSlots = new List<TimeSlot>
            {
                new() { TimeSlotId = 1, CourtId = 1, Date = new DateOnly(2026, 7, 1), StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(10, 0) }
            };
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _timeSlotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(existingSlots);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            _timeSlotsRepoMock.Verify(r => r.Add(It.IsAny<TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenValid_AddsSlotAndCallsSaveChanges()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var request = new CreateTimeSlotRequest
            {
                CourtId = 1,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                Price = 1500
            };
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _timeSlotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(new List<TimeSlot>());

            var result = await _controller.Create(request);

            _timeSlotsRepoMock.Verify(r => r.Add(It.IsAny<TimeSlot>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_WhenValid_TotalPriceIsCalculatedCorrectly()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var request = new CreateTimeSlotRequest
            {
                CourtId = 1,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),  
                Price = 1500
            };
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _timeSlotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(new List<TimeSlot>());

            TimeSlot? addedSlot = null;
            _timeSlotsRepoMock.Setup(r => r.Add(It.IsAny<TimeSlot>()))
                .Callback<TimeSlot>(s => addedSlot = s);

            await _controller.Create(request);

            Assert.NotNull(addedSlot);
            Assert.Equal(4500, addedSlot!.TotalPrice); 
        }

        [Fact]
        public async Task Update_WhenValidationFails_ReturnsBadRequest()
        {
            var request = new CreateTimeSlotRequest { CourtId = 0 };
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("CourtId", "CourtId must be greater than 0.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateTimeSlotRequest>())).Returns(failedValidation);

            var result = await _controller.Update(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
            _timeSlotsRepoMock.Verify(r => r.Update(It.IsAny<TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenSlotNotFound_ReturnsNotFound()
        {
            var request = new CreateTimeSlotRequest
            {
                CourtId = 1,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                Price = 1500
            };
            _timeSlotsRepoMock.Setup(r => r.GetById(99)).Returns((TimeSlot?)null);

            var result = await _controller.Update(99, request);

            Assert.IsType<NotFoundObjectResult>(result);
            _timeSlotsRepoMock.Verify(r => r.Update(It.IsAny<TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Update_WhenValid_UpdatesSlotAndCallsSaveChanges()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var existingSlot = new TimeSlot
            {
                TimeSlotId = 1,
                CourtId = 1,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0),
                Price = 1000,
                TotalPrice = 2000
            };
            var request = new CreateTimeSlotRequest
            {
                CourtId = 1,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                Price = 1500
            };
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(existingSlot);
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _timeSlotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(new List<TimeSlot> { existingSlot });

            var result = await _controller.Update(1, request);

            _timeSlotsRepoMock.Verify(r => r.Update(It.IsAny<TimeSlot>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_WhenNotFound_ReturnsNotFound()
        {
            _timeSlotsRepoMock.Setup(r => r.GetById(99)).Returns((TimeSlot?)null);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
            _timeSlotsRepoMock.Verify(r => r.Remove(It.IsAny<TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenFound_CallsRemoveAndSaveChanges()
        {
            var slot = new TimeSlot { TimeSlotId = 1, CourtId = 1 };
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            _timeSlotsRepoMock.Verify(r => r.Remove(slot), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }

        private List<TimeSlot> SampleSlots() => new()
        {
            new TimeSlot { TimeSlotId = 1, Price = 500,  TotalPrice = 1000, IsAvailable = true  },
            new TimeSlot { TimeSlotId = 2, Price = 1500, TotalPrice = 3000, IsAvailable = true  },
            new TimeSlot { TimeSlotId = 3, Price = 2500, TotalPrice = 5000, IsAvailable = false },
        };

        [Fact]
        public void FilterByAvailability_WhenNull_ReturnsAllSlots()
        {
            var result = SampleSlots().FilterByAvailability(null);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void FilterByAvailability_WhenTrue_ReturnsOnlyAvailableSlots()
        {
            var result = SampleSlots().FilterByAvailability(true);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void FilterByAvailability_WhenFalse_ReturnsOnlyUnavailableSlots()
        {
            var result = SampleSlots().FilterByAvailability(false);
            Assert.Single(result);
        }

        [Fact]
        public void FilterByMinPrice_WhenNull_ReturnsAllSlots()
        {
            var result = SampleSlots().FilterByMinPrice(null);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void FilterByMinPrice_WhenSet_ReturnsOnlySlotsAboveMin()
        {
            var result = SampleSlots().FilterByMinPrice(1500);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void FilterByMaxPrice_WhenNull_ReturnsAllSlots()
        {
            var result = SampleSlots().FilterByMaxPrice(null);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void FilterByMaxPrice_WhenSet_ReturnsOnlySlotsBelowMax()
        {
            var result = SampleSlots().FilterByMaxPrice(1500);
            Assert.Equal(2, result.Count());
        }
    }
}
