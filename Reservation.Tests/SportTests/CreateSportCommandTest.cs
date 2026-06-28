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
    public class CreateSportCommandTests
    {
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateSportCommand>> _validatorMock;
        private readonly CreateSportCommandHandler _handler;

        public CreateSportCommandTests()
        {
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<CreateSportCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateSportCommand>()))
                .Returns(new ValidationResult());

            _handler = new CreateSportCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenValidationFails_ReturnsFailResult()
        {
            var command = new CreateSportCommand("", 0);
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Name", "Sport name is required.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateSportCommand>())).Returns(failedValidation);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Add(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenNameAlreadyExists_ReturnsFailResult()
        {
            var command = new CreateSportCommand("Tenis", 4);
            _sportsRepoMock.Setup(r => r.GetByName("Tenis"))
                .Returns(new Sport { SportId = 1, Name = "Tenis", MaxPlayers = 4 });

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Add(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_AddsSportAndCallsSaveChanges()
        {
            var command = new CreateSportCommand("Padel", 4);
            _sportsRepoMock.Setup(r => r.GetByName("Padel")).Returns((Sport?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _sportsRepoMock.Verify(r => r.Add(It.IsAny<Sport>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenValid_InvalidatesCachePatterns()
        {
            var command = new CreateSportCommand("Padel", 4);
            _sportsRepoMock.Setup(r => r.GetByName("Padel")).Returns((Sport?)null);

            await _handler.Handle(command, CancellationToken.None);

            _cacheMock.Verify(c => c.RemoveByPatternAsync("sports*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("sport_*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("court_*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("courts*"), Times.Once);
        }
    }
}