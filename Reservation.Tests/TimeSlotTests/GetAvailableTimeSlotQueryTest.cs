using Moq;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Features.TimeSlot.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.TimeSlotTests
{
    public class GetAvailableTimeSlotsQueryTests
    {
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetAvailableTimeSlotsQueryHandler _handler;

        public GetAvailableTimeSlotsQueryTests()
        {
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<List<TimeSlotResult>>(It.IsAny<string>()))
                .ReturnsAsync((List<TimeSlotResult>?)null);

            _handler = new GetAvailableTimeSlotsQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_ReturnsOnlyAvailableSlots()
        {
            var slots = new List<Domain.Models.TimeSlot>
            {
                new() { TimeSlotId = 1, IsAvailable = true },
                new() { TimeSlotId = 2, IsAvailable = false }
            };
            _slotsRepoMock.Setup(r => r.GetAll()).Returns(slots);

            var result = await _handler.Handle(new GetAvailableTimeSlotsQuery(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }
    }
}