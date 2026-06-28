using Moq;
using Reservation.Application.Features.Sport.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.SportTests
{
    public class GetAllSportsQueryTests
    {
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetAllSportsQueryHandler _handler;

        public GetAllSportsQueryTests()
        {
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<List<Sport>>(It.IsAny<string>()))
                .ReturnsAsync((List<Sport>?)null);

            _handler = new GetAllSportsQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_ReturnsAllSportsFromRepository()
        {
            var data = new List<Sport>
            {
                new() { SportId = 1, Name = "Tenis", MaxPlayers = 4 },
                new() { SportId = 2, Name = "Fudbal", MaxPlayers = 22 }
            };
            _sportsRepoMock.Setup(r => r.SearchByName(null)).Returns(data);

            var result = await _handler.Handle(new GetAllSportsQuery(null), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }
    }
}