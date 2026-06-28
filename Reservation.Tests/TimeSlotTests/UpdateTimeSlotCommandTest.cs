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
    public class UpdateTimeSlotCommandTests
    {
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<UpdateTimeSlotCommand>> _validatorMock;
        private readonly UpdateTimeSlotCommandHandler _handler;

        public UpdateTimeSlotCommandTests()
        {
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _courtsRepoMock = new Mock<ICourtRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<UpdateTimeSlotCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<UpdateTimeSlotCommand>()))
                .Returns(new ValidationResult());

            _handler = new UpdateTimeSlotCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSlotDoesNotExist_ReturnsFailResult()
        {
            var command = new UpdateTimeSlotCommand(
                99, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(10, 0), 1, true);
            _slotsRepoMock.Setup(r => r.GetById(99)).Returns((Domain.Models.TimeSlot?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenCourtDoesNotExist_ReturnsFailResult()
        {
            var slot = new Domain.Models.TimeSlot { TimeSlotId = 1, CourtId = 1 };
            var command = new UpdateTimeSlotCommand(
                1, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(10, 0), 99, true);
            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenOverlapsWithAnotherSlot_ReturnsFailResult()
        {
            var slot = new Domain.Models.TimeSlot { TimeSlotId = 1, CourtId = 1 };
            var court = new Court { CourtId = 1, Name = "Teren 1", PricePerHour = 1000 };
            var otherSlot = new Domain.Models.TimeSlot
            {
                TimeSlotId = 2,
                CourtId = 1,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0)
            };
            var command = new UpdateTimeSlotCommand(
                1, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(10, 0), new TimeOnly(12, 0), 1, true);

            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _slotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(new List<Domain.Models.TimeSlot> { otherSlot });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenValid_UpdatesSlotAndCallsSaveChanges()
        {
            var slot = new Domain.Models.TimeSlot { TimeSlotId = 1, CourtId = 1 };
            var court = new Court { CourtId = 1, Name = "Teren 1", PricePerHour = 1000 };
            var command = new UpdateTimeSlotCommand(
                1, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0), 1, true);

            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _slotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(new List<Domain.Models.TimeSlot>());

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2000, slot.TotalPrice);
            _slotsRepoMock.Verify(r => r.Update(slot), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}