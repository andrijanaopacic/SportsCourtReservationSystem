using Moq;
using Reservation.Application.Features.Reservation.Queries;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class GetCourtCalendarQueryTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly GetCourtCalendarQueryHandler _handler;

        public GetCourtCalendarQueryTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);

            _handler = new GetCourtCalendarQueryHandler(_uowMock.Object);
        }

        [Fact]
        public async Task Handle_GroupsReservationsByDate()
        {
            var date1 = new DateOnly(2026, 7, 5);
            var date2 = new DateOnly(2026, 7, 10);

            var data = new List<ReservationEntity>
            {
                new() { Status = ReservationStatus.UPCOMING,
                        ReservationItems = new List<ReservationItem> { new() { TimeSlot = new TimeSlot { CourtId = 1, Date = date1 } } } },
                new() { Status = ReservationStatus.UPCOMING,
                        ReservationItems = new List<ReservationItem> { new() { TimeSlot = new TimeSlot { CourtId = 1, Date = date1 } } } },
                new() { Status = ReservationStatus.UPCOMING,
                        ReservationItems = new List<ReservationItem> { new() { TimeSlot = new TimeSlot { CourtId = 1, Date = date2 } } } }
            };
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(data);

            var result = await _handler.Handle(new GetCourtCalendarQuery(1, 2026, 7), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
            Assert.Equal(2, result.Value!.First(d => d.Date == date1).ReservationCount);
        }
    }
}