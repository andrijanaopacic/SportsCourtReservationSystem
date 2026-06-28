using FluentValidation;
using FluentValidation.Results;
using Moq;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;
using Xunit;

namespace Reservation.Tests.TimeSlotTests
{
    public class DeleteTimeSlotCommandTests
    {
        private readonly Mock<ITimeSlotRepository> _slotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<DeleteTimeSlotCommand>> _validatorMock;
        private readonly DeleteTimeSlotCommandHandler _handler;

        public DeleteTimeSlotCommandTests()
        {
            _slotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_slotsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();

            _validatorMock = new Mock<IValidator<DeleteTimeSlotCommand>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<DeleteTimeSlotCommand>()))
                .Returns(new ValidationResult());

            _handler = new DeleteTimeSlotCommandHandler(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_WhenSlotDoesNotExist_ReturnsFailResult()
        {
            var command = new DeleteTimeSlotCommand(99);
            _slotsRepoMock.Setup(r => r.GetById(99)).Returns((Domain.Models.TimeSlot?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result.IsSuccess);
            _slotsRepoMock.Verify(r => r.Remove(It.IsAny<Domain.Models.TimeSlot>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValid_RemovesSlotAndCallsSaveChanges()
        {
            var slot = new Domain.Models.TimeSlot { TimeSlotId = 1, CourtId = 1 };
            var command = new DeleteTimeSlotCommand(1);
            _slotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            _slotsRepoMock.Verify(r => r.Remove(slot), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}