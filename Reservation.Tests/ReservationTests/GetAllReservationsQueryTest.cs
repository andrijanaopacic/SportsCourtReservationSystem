using Moq;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Features.Reservation.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class GetAllReservationsQueryTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetAllReservationsQueryHandler _handler;

        public GetAllReservationsQueryTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<List<ReservationResult>>(It.IsAny<string>()))
                .ReturnsAsync((List<ReservationResult>?)null);

            _handler = new GetAllReservationsQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WithoutStatusFilter_ReturnsAllReservations()
        {
            var data = new List<ReservationEntity>
            {
                new() { ReservationId = 1, Status = ReservationStatus.UPCOMING, ReservationItems = new List<ReservationItem>() },
                new() { ReservationId = 2, Status = ReservationStatus.CANCELLED, ReservationItems = new List<ReservationItem>() }
            };
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(data);

            var result = await _handler.Handle(new GetAllReservationsQuery(null), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingReservations()
        {
            var data = new List<ReservationEntity>
            {
                new() { ReservationId = 1, Status = ReservationStatus.UPCOMING, ReservationItems = new List<ReservationItem>() },
                new() { ReservationId = 2, Status = ReservationStatus.CANCELLED, ReservationItems = new List<ReservationItem>() }
            };
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(data);

            var result = await _handler.Handle(new GetAllReservationsQuery("CANCELLED"), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }
    }
}