using Moq;
using Reservation.Application.Features.Court.Queries;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.CourtTests
{
    public class GetCourtByIdQueryTests
    {
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly GetCourtByIdQueryHandler _handler;

        public GetCourtByIdQueryTests()
        {
            _courtsRepoMock = new Mock<ICourtRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<CourtDetailsResult>(It.IsAny<string>()))
                .ReturnsAsync((CourtDetailsResult?)null);

            _handler = new GetCourtByIdQueryHandler(_uowMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCourtDoesNotExist_ReturnsFailResult()
        {
            var query = new GetCourtByIdQuery(42);
            _courtsRepoMock.Setup(r => r.GetByIdWithSport(42)).Returns((Court?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Handle_WhenCourtExists_ReturnsOkWithDetails()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var court = new Court
            {
                CourtId = 1,
                Name = "Teren 1",
                Location = "Beograd",
                PricePerHour = 1500,
                SportId = 1,
                Sport = sport
            };
            var query = new GetCourtByIdQuery(1);
            _courtsRepoMock.Setup(r => r.GetByIdWithSport(1)).Returns(court);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Teren 1", result.Value!.Name);
            Assert.Equal("Tenis", result.Value.SportName);
        }
    }
}