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
    public class UpdateCourtCommandTests
    {
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<UpdateCourtCommand>> _validatorMock;
        private readonly UpdateCourtCommandHandler _handler;

        public UpdateCourtCommandTests()
        {
            _courtsRepoMock = new Mock<ICourtRepository>();
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<UpdateCourtCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<UpdateCourtCommand>()))
                .Returns(new ValidationResult());

            _handler = new UpdateCourtCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCourtDoesNotExist_ReturnsFailResult()
        {
            var command = new UpdateCourtCommand(99, "Teren", "Beograd", "", 1500, true, 1);
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _courtsRepoMock.Verify(r => r.Update(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSportDoesNotExist_ReturnsFailResult()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var command = new UpdateCourtCommand(1, "Teren", "Beograd", "", 1500, true, 99);
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _sportsRepoMock.Setup(r => r.GetById(99)).Returns((Sport?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _courtsRepoMock.Verify(r => r.Update(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_UpdatesCourtAndCallsSaveChanges()
        {
            var court = new Court { CourtId = 1, Name = "Stari naziv" };
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var command = new UpdateCourtCommand(1, "Novi naziv", "Novi Sad", "Opis", 2000, false, 1);
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);
            _sportsRepoMock.Setup(r => r.GetById(1)).Returns(sport);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Novi naziv", court.Name);
            _courtsRepoMock.Verify(r => r.Update(court), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}