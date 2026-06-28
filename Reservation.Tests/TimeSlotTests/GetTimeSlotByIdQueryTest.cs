using Moq;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Features.TimeSlot.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.TimeSlotTests
{
    public class GetTimeSlotByIdQueryTests
    {
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetTimeSlotByIdQueryHandler _handler;

        public GetTimeSlotByIdQueryTests()
        {
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<TimeSlotResult>(It.IsAny<string>()))
                .ReturnsAsync((TimeSlotResult?)null);

            _handler = new GetTimeSlotByIdQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSlotDoesNotExist_ReturnsFailResult()
        {
            var query = new GetTimeSlotByIdQuery(42);
            _slotsRepoMock.Setup(r => r.GetByIdWithCourt(42)).Returns((Domain.Models.TimeSlot?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenSlotExists_ReturnsOkWithDetails()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var slot = new Domain.Models.TimeSlot
            {
                TimeSlotId = 1,
                CourtId = 1,
                Court = court,
                Date = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Price = 1000,
                TotalPrice = 1000,
                IsAvailable = true
            };
            var query = new GetTimeSlotByIdQuery(1);
            _slotsRepoMock.Setup(r => r.GetByIdWithCourt(1)).Returns(slot);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Teren 1", result.Value!.CourtName);
        }
    }
}