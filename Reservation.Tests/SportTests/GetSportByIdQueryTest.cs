using Moq;
using Reservation.Application.Features.Sport.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.SportTests
{
    public class GetSportByIdQueryTests
    {
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetSportByIdQueryHandler _handler;

        public GetSportByIdQueryTests()
        {
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<SportDetailsResult>(It.IsAny<string>()))
                .ReturnsAsync((SportDetailsResult?)null);

            _handler = new GetSportByIdQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSportDoesNotExist_ReturnsFailResult()
        {
            var query = new GetSportByIdQuery(42);
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(42)).Returns((Sport?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenSportExists_ReturnsOkWithDetails()
        {
            var sport = new Sport
            {
                SportId = 1,
                Name = "Tenis",
                MaxPlayers = 4,
                Courts = new List<Court>
                {
                    new() { CourtId = 1, Name = "Teren 1", Location = "Beograd", PricePerHour = 1500, IsIndoor = true, SportId = 1 }
                }
            };
            var query = new GetSportByIdQuery(1);
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(1)).Returns(sport);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Tenis", result.Value!.Name);
            Assert.Equal(1, result.Value.TotalCourtsCount);
            Assert.Single(result.Value.Courts);
        }

        [Fact]
        public async Task Handle_WhenCached_ReturnsCachedValueWithoutCallingRepository()
        {
            var cached = new SportDetailsResult(1, "Tenis", 4, 0, new List<CourtSummary>());
            _cacheMock.Setup(c => c.GetAsync<SportDetailsResult>("sport_1")).ReturnsAsync(cached);

            var query = new GetSportByIdQuery(1);
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Same(cached, result.Value);
            _sportsRepoMock.Verify(r => r.GetByIdWithCourts(It.IsAny<int>()), Times.Never);
        }
    }
}