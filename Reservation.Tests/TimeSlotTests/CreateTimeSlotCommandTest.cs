using FluentValidation;
using FluentValidation.Results;
using Moq;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.TimeSlotTests
{
    public class CreateTimeSlotCommandTests
    {
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateTimeSlotCommand>> _validatorMock;
        private readonly CreateTimeSlotCommandHandler _handler;

        public CreateTimeSlotCommandTests()
        {
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _courtsRepoMock = new Mock<ICourtRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<CreateTimeSlotCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateTimeSlotCommand>()))
                .Returns(new ValidationResult());

            _handler = new CreateTimeSlotCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCourtDoesNotExist_ReturnsFailResult()
        {
            var command = new CreateTimeSlotCommand(
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(10, 0), 99);
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _slotsRepoMock.Verify(r => r.Add(It.IsAny<Domain.Models.TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenOverlappingSlotExists_ReturnsFailResult()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1", PricePerHour = 1000 };
            var existingSlots = new List<Domain.Models.TimeSlot>
            {
                new() { TimeSlotId = 1, CourtId = 1, Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                        StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
            };
            var command = new CreateTimeSlotCommand(
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(10, 0), new TimeOnly(12, 0), 1);

            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _slotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(existingSlots);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _slotsRepoMock.Verify(r => r.Add(It.IsAny<Domain.Models.TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_AddsSlotAndCallsSaveChanges()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1", PricePerHour = 1000 };
            var command = new CreateTimeSlotCommand(
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0), 1);

            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _slotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(new List<Domain.Models.TimeSlot>());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2000, result.Value!.TotalPrice);
            _slotsRepoMock.Verify(r => r.Add(It.IsAny<Domain.Models.TimeSlot>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}