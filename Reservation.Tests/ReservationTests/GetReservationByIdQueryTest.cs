using Moq;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Features.Reservation.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class GetReservationByIdQueryTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetReservationByIdQueryHandler _handler;

        public GetReservationByIdQueryTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<ReservationResult>(It.IsAny<string>()))
                .ReturnsAsync((ReservationResult?)null);

            _handler = new GetReservationByIdQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenReservationDoesNotExist_ReturnsFailResult()
        {
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(42)).Returns((ReservationEntity?)null);

            var result = await _handler.Handle(new GetReservationByIdQuery(42), CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenReservationExists_ReturnsOkWithDetails()
        {
            var reservation = new ReservationEntity
            {
                ReservationId = 1,
                Status = ReservationStatus.UPCOMING,
                ApplicationUserId = "user1",
                TotalPrice = 1000,
                ReservationItems = new List<ReservationItem>()
            };
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(reservation);

            var result = await _handler.Handle(new GetReservationByIdQuery(1), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value!.ReservationId);
        }
    }
}