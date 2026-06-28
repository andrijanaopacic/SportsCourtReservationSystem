using Moq;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Features.TimeSlot.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.TimeSlotTests
{
    public class GetTimeSlotsByCourtQueryTests
    {
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetTimeSlotsByCourtQueryHandler _handler;

        public GetTimeSlotsByCourtQueryTests()
        {
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _courtsRepoMock = new Mock<ICourtRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<List<TimeSlotResult>>(It.IsAny<string>()))
                .ReturnsAsync((List<TimeSlotResult>?)null);

            _handler = new GetTimeSlotsByCourtQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCourtDoesNotExist_ReturnsFailResult()
        {
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _handler.Handle(new GetTimeSlotsByCourtQuery(99), CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenCourtExists_ReturnsSlotsForThatCourt()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var slots = new List<Domain.Models.TimeSlot>
            {
                new() { TimeSlotId = 1, CourtId = 1 },
                new() { TimeSlotId = 2, CourtId = 1 }
            };
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _slotsRepoMock.Setup(r => r.GetByCourt(1)).Returns(slots);

            var result = await _handler.Handle(new GetTimeSlotsByCourtQuery(1), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }
    }
}