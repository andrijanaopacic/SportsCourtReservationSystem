using FluentValidation;
using FluentValidation.Results;
using Moq;
using Reservation.Application.Features.Court.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.CourtTests
{
    public class CreateCourtCommandTests
    {
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateCourtCommand>> _validatorMock;
        private readonly CreateCourtCommandHandler _handler;

        public CreateCourtCommandTests()
        {
            _courtsRepoMock = new Mock<ICourtRepository>();
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<CreateCourtCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateCourtCommand>()))
                .Returns(new ValidationResult());

            _handler = new CreateCourtCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenValidationFails_ReturnsFailResult()
        {
            var command = new CreateCourtCommand("", "", "", 0, false, 0);
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Name", "Court name is required.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateCourtCommand>())).Returns(failedValidation);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _courtsRepoMock.Verify(r => r.Add(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSportDoesNotExist_ReturnsFailResult()
        {
            var command = new CreateCourtCommand("Teren", "Beograd", "", 1500, true, 99);
            _sportsRepoMock.Setup(r => r.GetById(99)).Returns((Sport?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _courtsRepoMock.Verify(r => r.Add(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_AddsCourtAndCallsSaveChanges()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var command = new CreateCourtCommand("Teren 1", "Beograd", "Opis", 1500, true, 1);
            _sportsRepoMock.Setup(r => r.GetById(1)).Returns(sport);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _courtsRepoMock.Verify(r => r.Add(It.IsAny<Court>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenValid_InvalidatesCachePatterns()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var command = new CreateCourtCommand("Teren 1", "Beograd", "Opis", 1500, true, 1);
            _sportsRepoMock.Setup(r => r.GetById(1)).Returns(sport);

            await _handler.Handle(command, CancellationToken.None);

            _cacheMock.Verify(c => c.RemoveByPatternAsync("sports*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("sport_*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("court_*"), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPatternAsync("courts*"), Times.Once);
        }
    }
}