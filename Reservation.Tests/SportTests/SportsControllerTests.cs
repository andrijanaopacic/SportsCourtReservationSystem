using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Reservation.API.Controllers;
using Reservation.API.DTOs.Sport;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Tests.SportTests
{
    public class SportsControllerTests
    {

        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateSportRequest>> _validatorMock;
        private readonly SportsController _controller;

        public SportsControllerTests()
        {
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<List<Sport>>(It.IsAny<string>())).ReturnsAsync((List<Sport>?)null);
            _cacheMock.Setup(c => c.GetAsync<Sport>(It.IsAny<string>())).ReturnsAsync((Sport?)null);

            _validatorMock = new Mock<IValidator<CreateSportRequest>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateSportRequest>())).Returns(new ValidationResult());

            _controller = new SportsController(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }


        [Fact]
        public async Task GetAll_ReturnsAllSports()
        {
            var data = new List<Sport>
            {
                new() { SportId = 1, Name = "Tenis", MaxPlayers = 4 },
                new() { SportId = 2, Name = "Football", MaxPlayers = 22 }
            };

            _sportsRepoMock.Setup(r => r.SearchByName(null)).Returns(data);

            var result = await _controller.GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<Sport>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(42)).Returns((Sport?)null);

            var result = await _controller.GetById(42);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsOk()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis", MaxPlayers = 4 };
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(1)).Returns(sport);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(sport, ok.Value);
        }

        [Fact]
        public async Task Create_WhenValidationFails_ReturnsBadRequest()
        {
            var request = new CreateSportRequest { Name = "", MaxPlayers = 0 };
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Name", "Sport name is required.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateSportRequest>())).Returns(failedValidation);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            _sportsRepoMock.Verify(r => r.Add(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenNameAlreadyExists_ReturnsBadRequest()
        {
            var request = new CreateSportRequest { Name = "Tenis", MaxPlayers = 4 };
            _sportsRepoMock.Setup(r => r.GetByName("Tenis")).Returns(new Sport { SportId = 1, Name = "Tenis", MaxPlayers = 4 });

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            _sportsRepoMock.Verify(r => r.Add(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Create_AddsSportAndCallsSaveChanges()
        {
            var request = new CreateSportRequest { Name = "Padel", MaxPlayers = 4 };
            _sportsRepoMock.Setup(r => r.GetByName("Padel")).Returns((Sport?)null);

            var result = await _controller.Create(request);

            _sportsRepoMock.Verify(r => r.Add(It.IsAny<Sport>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Update_WhenValidationFails_ReturnsBadRequest()
        {
            var request = new CreateSportRequest { Name = "", MaxPlayers = 0 };
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Name", "Sport name is required.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateSportRequest>())).Returns(failedValidation);

            var result = await _controller.Update(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
            _sportsRepoMock.Verify(r => r.Update(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenNotFound_ReturnsNotFound()
        {
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(99)).Returns((Sport?)null);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
            _sportsRepoMock.Verify(r => r.Remove(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenHasCourts_ReturnsBadRequest()
        {
            var sport = new Sport
            {
                SportId = 1,
                Name = "Tenis",
                Courts = new List<Court> { new() { CourtId = 1, Name = "Court 1" } }
            };
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(1)).Returns(sport);

            var result = await _controller.Delete(1);

            Assert.IsType<BadRequestObjectResult>(result);
            _sportsRepoMock.Verify(r => r.Remove(It.IsAny<Sport>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenFoundWithNoCourts_CallsRemoveAndSaveChanges()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis", Courts = new List<Court>() };
            _sportsRepoMock.Setup(r => r.GetByIdWithCourts(1)).Returns(sport);

            var result = await _controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            _sportsRepoMock.Verify(r => r.Remove(sport), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }

    }
}
