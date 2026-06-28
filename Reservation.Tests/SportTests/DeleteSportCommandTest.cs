using FluentValidation;
using FluentValidation.Results;
using Moq;
using Reservation.Application.Features.Sport.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.SportTests
{
    public class DeleteSportCommandTests
    {
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<DeleteSportCommand>> _validatorMock;
        private readonly DeleteSportCommandHandler _handler;

        public DeleteSportCommandTests()
        {
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<DeleteSportCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<DeleteSportCommand>()))
                .Returns(new ValidationResult());

            _handler = new DeleteSportCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSportDoesNotExist_ReturnsFailResult()
        {
            var command = new DeleteSportCommand(99);
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(99)).Returns((Sport?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Remove(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSportHasCourts_ReturnsFailResult()
        {
            var sport = new Sport
            {
                SportId = 1,
                Name = "Tenis",
                Courts = new List<Court> { new() { CourtId = 1, Name = "Teren 1" } }
            };
            var command = new DeleteSportCommand(1);
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(1)).Returns(sport);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Remove(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_RemovesSportAndCallsSaveChanges()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis", Courts = new List<Court>() };
            var command = new DeleteSportCommand(1);
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(1)).Returns(sport);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Remove(sport), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}