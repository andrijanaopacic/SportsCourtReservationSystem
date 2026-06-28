using Moq;
using Reservation.Application.Features.Court.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.CourtTests
{
    public class GetAllCourtsQueryTests
    {
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetAllCourtsQueryHandler _handler;

        public GetAllCourtsQueryTests()
        {
            _courtsRepoMock = new Mock<ICourtRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<List<CourtDetailsResult>>(It.IsAny<string>()))
                .ReturnsAsync((List<CourtDetailsResult>?)null);

            _handler = new GetAllCourtsQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_ReturnsAllCourtsFromRepository()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var data = new List<Court>
            {
                new() { CourtId = 1, Name = "Teren 1", SportId = 1, Sport = sport, IsIndoor = true },
                new() { CourtId = 2, Name = "Teren 2", SportId = 1, Sport = sport, IsIndoor = false }
            };
            _courtsRepoMock.Setup(r => r.GetAllWithSport()).Returns(data);

            var result = await _handler.Handle(new GetAllCourtsQuery(null, null, null), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task Handle_FiltersByIsIndoor()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var data = new List<Court>
            {
                new() { CourtId = 1, Name = "Teren 1", SportId = 1, Sport = sport, IsIndoor = true },
                new() { CourtId = 2, Name = "Teren 2", SportId = 1, Sport = sport, IsIndoor = false }
            };
            _courtsRepoMock.Setup(r => r.GetAllWithSport()).Returns(data);

            var result = await _handler.Handle(new GetAllCourtsQuery(null, true, null), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }
    }
}