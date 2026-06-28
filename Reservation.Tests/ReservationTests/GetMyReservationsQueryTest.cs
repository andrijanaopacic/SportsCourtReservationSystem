using Moq;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Features.Reservation.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class GetMyReservationsQueryTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetMyReservationsQueryHandler _handler;

        public GetMyReservationsQueryTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<List<ReservationResult>>(It.IsAny<string>()))
                .ReturnsAsync((List<ReservationResult>?)null);

            _handler = new GetMyReservationsQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_ReturnsReservationsForGivenUser()
        {
            var data = new List<ReservationEntity>
            {
                new() { ReservationId = 1, ApplicationUserId = "user1", Status = ReservationStatus.UPCOMING, ReservationItems = new List<ReservationItem>() }
            };
            _reservationsRepoMock.Setup(r => r.GetByUser("user1")).Returns(data);

            var result = await _handler.Handle(new GetMyReservationsQuery("user1"), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }
    }
}