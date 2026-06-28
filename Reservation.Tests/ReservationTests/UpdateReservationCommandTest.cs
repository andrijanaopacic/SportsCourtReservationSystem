using Moq;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class UpdateReservationCommandTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly UpdateReservationCommandHandler _handler;

        public UpdateReservationCommandTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());

            _handler = new UpdateReservationCommandHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenReservationDoesNotExist_ReturnsFailResult()
        {
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(99)).Returns((ReservationEntity?)null);

            var result = await _handler.Handle(
                new UpdateReservationCommand(99, new List<ReservationItemInput>()), CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenReservationIsCancelled_ReturnsFailResult()
        {
            var reservation = new ReservationEntity { ReservationId = 1, Status = ReservationStatus.CANCELLED };
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(reservation);

            var result = await _handler.Handle(
                new UpdateReservationCommand(1, new List<ReservationItemInput>()), CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenAllItemsRemoved_DeletesReservationAndReturnsNullValue()
        {
            var existingSlot = new TimeSlot { TimeSlotId = 1, IsAvailable = false };
            var reservation = new ReservationEntity
            {
                ReservationId = 1,
                Status = ReservationStatus.UPCOMING,
                ApplicationUserId = "user1",
                ReservationItems = new List<ReservationItem> { new() { TimeSlotId = 1, TimeSlot = existingSlot, RowNumber = 1 } }
            };
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(reservation);
            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(existingSlot);

            var result = await _handler.Handle(
                new UpdateReservationCommand(1, new List<ReservationItemInput>()), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
            Assert.True(existingSlot.IsAvailable);
            _reservationsRepoMock.Verify(r => r.Remove(reservation), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenAddingNewConflictingSlot_ReturnsFailResult()
        {
            var existingSlot = new TimeSlot { TimeSlotId = 1, IsAvailable = false };
            var reservation = new ReservationEntity
            {
                ReservationId = 1,
                Status = ReservationStatus.UPCOMING,
                ApplicationUserId = "user1",
                ReservationItems = new List<ReservationItem> { new() { TimeSlotId = 1, TimeSlot = existingSlot, RowNumber = 1 } }
            };
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(reservation);

            var conflictingReservation = new ReservationEntity
            {
                ReservationId = 2,
                Status = ReservationStatus.UPCOMING,
                ReservationItems = new List<ReservationItem> { new() { TimeSlotId = 2 } }
            };
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { reservation, conflictingReservation });

            var result = await _handler.Handle(
                new UpdateReservationCommand(1, new List<ReservationItemInput> { new(1), new(2) }), CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenValid_UpdatesReservationAndRecalculatesTotalPrice()
        {
            var existingSlot = new TimeSlot { TimeSlotId = 1, IsAvailable = false, TotalPrice = 1000 };
            var newSlot = new TimeSlot
            {
                TimeSlotId = 2,
                IsAvailable = true,
                TotalPrice = 1500,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Court = new Court { CourtId = 1, Name = "Teren 1" }
            };
            var reservation = new ReservationEntity
            {
                ReservationId = 1,
                Status = ReservationStatus.UPCOMING,
                ApplicationUserId = "user1",
                ReservationItems = new List<ReservationItem> { new() { TimeSlotId = 1, TimeSlot = existingSlot, RowNumber = 1, Price = 1000 } }
            };
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(reservation);
            _slotsRepoMock.Setup(r => r.GetById(2)).Returns(newSlot);
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { reservation });

            var result = await _handler.Handle(
                new UpdateReservationCommand(1, new List<ReservationItemInput> { new(1), new(2) }), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2500, reservation.TotalPrice);
            _reservationsRepoMock.Verify(r => r.Update(reservation), Times.Once);
        }
    }
}