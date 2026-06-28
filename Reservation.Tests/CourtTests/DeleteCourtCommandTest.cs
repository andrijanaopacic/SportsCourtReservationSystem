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
    public class DeleteCourtCommandTests
    {
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<DeleteCourtCommand>> _validatorMock;
        private readonly DeleteCourtCommandHandler _handler;

        public DeleteCourtCommandTests()
        {
            _courtsRepoMock = new Mock<ICourtRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<DeleteCourtCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<DeleteCourtCommand>()))
                .Returns(new ValidationResult());

            _handler = new DeleteCourtCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCourtDoesNotExist_ReturnsFailResult()
        {
            var command = new DeleteCourtCommand(99);
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _courtsRepoMock.Verify(r => r.Remove(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_RemovesCourtAndCallsSaveChanges()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            var command = new DeleteCourtCommand(1);
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _courtsRepoMock.Verify(r => r.Remove(court), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}