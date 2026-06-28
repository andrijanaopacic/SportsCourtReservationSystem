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
    public class UpdateSportCommandTests
    {
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<UpdateSportCommand>> _validatorMock;
        private readonly UpdateSportCommandHandler _handler;

        public UpdateSportCommandTests()
        {
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<UpdateSportCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<UpdateSportCommand>()))
                .Returns(new ValidationResult());

            _handler = new UpdateSportCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenValidationFails_ReturnsFailResult()
        {
            var command = new UpdateSportCommand(1, "", 0);
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Name", "Sport name is required.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<UpdateSportCommand>())).Returns(failedValidation);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Update(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSportDoesNotExist_ReturnsFailResult()
        {
            var command = new UpdateSportCommand(99, "Tenis", 4);
            _sportsRepoMock.Setup(r => r.GetById(99)).Returns((Sport?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Update(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_UpdatesSportAndCallsSaveChanges()
        {
            var sport = new Sport { SportId = 1, Name = "Stari naziv", MaxPlayers = 2 };
            var command = new UpdateSportCommand(1, "Novi naziv", 6);
            _sportsRepoMock.Setup(r => r.GetById(1)).Returns(sport);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Novi naziv", sport.Name);
            Assert.Equal(6, sport.MaxPlayers);
            _sportsRepoMock.Verify(r => r.Update(sport), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenValid_InvalidatesCachePatterns()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis", MaxPlayers = 4 };
            var command = new UpdateSportCommand(1, "Tenis", 4);
            _sportsRepoMock.Setup(r => r.GetById(1)).Returns(sport);

            await _handler.Handle(command, CancellationToken.None);

            _cacheMock.Verify(c => c.RemoveAsync("sport_1"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("sports*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("sport_*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("court_*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("courts*"), Times.Once);
        }
    }
}