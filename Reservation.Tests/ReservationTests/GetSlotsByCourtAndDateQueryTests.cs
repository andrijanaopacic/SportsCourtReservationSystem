using Moq;
using Reservation.Application.Features.Reservation.Queries;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.ReservationTests
{
    public class GetSlotsByCourtAndDateQueryTests
    {
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly GetSlotsByCourtAndDateQueryHandler _handler;

        public GetSlotsByCourtAndDateQueryTests()
        {
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);

            _handler = new GetSlotsByCourtAndDateQueryHandler(_uowMock.Object);
        }

        [Fact]
        public async Task Handle_ReturnsSlotsMatchingCourtAndDate()
        {
            var date = DateOnly.FromDateTime(DateTime.Today);
            var data = new List<TimeSlot>
            {
                new() { TimeSlotId = 1, CourtId = 1, Date = date },
                new() { TimeSlotId = 2, CourtId = 2, Date = date }
            };
            _slotsRepoMock.Setup(r => r.GetAll()).Returns(data);

            var result = await _handler.Handle(new GetSlotsByCourtAndDateQuery(1, date), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }
    }
}