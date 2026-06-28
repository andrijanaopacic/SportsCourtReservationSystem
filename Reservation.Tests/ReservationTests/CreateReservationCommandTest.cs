using FluentValidation;
using FluentValidation.Results;
using Moq;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class CreateReservationCommandTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateReservationCommand>> _validatorMock;
        private readonly CreateReservationCommandHandler _handler;

        public CreateReservationCommandTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<CreateReservationCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateReservationCommand>()))
                .Returns(new ValidationResult());

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());

            _handler = new CreateReservationCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenValidationFails_ReturnsFailResult()
        {
            var command = new CreateReservationCommand("user1", new List<ReservationItemInput>());
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Items", "Rezervacija mora imati barem jednu stavku.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateReservationCommand>())).Returns(failedValidation);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _reservationsRepoMock.Verify(r => r.Add(It.IsAny<ReservationEntity>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSlotAlreadyReserved_ReturnsFailResult()
        {
            var existingReservation = new ReservationEntity
            {
                Status = ReservationStatus.UPCOMING,
                ReservationItems = new List<ReservationItem> { new() { TimeSlotId = 1 } }
            };
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { existingReservation });

            var command = new CreateReservationCommand("user1", new List<ReservationItemInput> { new(1) });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _reservationsRepoMock.Verify(r => r.Add(It.IsAny<ReservationEntity>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSlotDoesNotExist_ReturnsFailResult()
        {
            var command = new CreateReservationCommand("user1", new List<ReservationItemInput> { new(99) });
            _slotsRepoMock.Setup(r => r.GetById(99)).Returns((TimeSlot?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenSlotNotAvailable_ReturnsFailResult()
        {
            var slot = new TimeSlot
            {
                TimeSlotId = 1,
                IsAvailable = false,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
            };
            var command = new CreateReservationCommand("user1", new List<ReservationItemInput> { new(1) });
            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenValid_CreatesReservationAndCallsSaveChanges()
        {
            var slot = new TimeSlot
            {
                TimeSlotId = 1,
                IsAvailable = true,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TotalPrice = 1000,
                Court = new Court { CourtId = 1, Name = "Teren 1" }
            };
            var command = new CreateReservationCommand("user1", new List<ReservationItemInput> { new(1) });
            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1000, result.Value!.TotalPrice);
            _reservationsRepoMock.Verify(r => r.Add(It.IsAny<ReservationEntity>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}