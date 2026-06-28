using Moq;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class CancelReservationCommandTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly CancelReservationCommandHandler _handler;

        public CancelReservationCommandTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _handler = new CancelReservationCommandHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenReservationDoesNotExist_ReturnsFailResult()
        {
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(99)).Returns((ReservationEntity?)null);

            var result = await _handler.Handle(new CancelReservationCommand(99), CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenAlreadyCancelled_ReturnsFailResult()
        {
            var reservation = new ReservationEntity { ReservationId = 1, Status = ReservationStatus.CANCELLED };
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(reservation);

            var result = await _handler.Handle(new CancelReservationCommand(1), CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenValid_SetsStatusToCancelledAndFreesSlots()
        {
            var slot = new TimeSlot { TimeSlotId = 1, IsAvailable = false };
            var reservation = new ReservationEntity
            {
                ReservationId = 1,
                Status = ReservationStatus.UPCOMING,
                ApplicationUserId = "user1",
                ReservationItems = new List<ReservationItem> { new() { TimeSlotId = 1, TimeSlot = slot } }
            };
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(reservation);
            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _handler.Handle(new CancelReservationCommand(1), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(ReservationStatus.CANCELLED, reservation.Status);
            Assert.True(slot.IsAvailable);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}