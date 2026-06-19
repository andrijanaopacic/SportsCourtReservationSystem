using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Reservation.API.DTOs.Court;
using Reservation.API.Extensions;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Tests.CourtTests
{
    public class CourtsControllerTests
    {
        private readonly Mock<ICourtRepository> _courtsRepoMock;
        private readonly Mock<ISportRepository> _sportsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateCourtRequest>> _validatorMock;
        private readonly CourtsController _controller;

        public CourtsControllerTests()
        {
            _courtsRepoMock = new Mock<ICourtRepository>();
            _sportsRepoMock = new Mock<ISportRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Courts).Returns(_courtsRepoMock.Object);
            _uowMock.SetupGet(u => u.Sports).Returns(_sportsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<CourtDto>(It.IsAny<string>())).ReturnsAsync((CourtDto?)null);
            _cacheMock.Setup(c => c.GetAsync<List<CourtDto>>(It.IsAny<string>())).ReturnsAsync((List<CourtDto>?)null);

            _validatorMock = new Mock<IValidator<CreateCourtRequest>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateCourtRequest>())).Returns(new ValidationResult());

            _controller = new CourtsController(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            _courtsRepoMock.Setup(r => r.GetByIdWithSport(42)).Returns((Court?)null);

            var result = await _controller.GetById(42);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact] 
        public async Task GetById_WhenFound_ReturnsOkWithCourtDto()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var court = new Court
            {
                CourtId = 1,
                Name = "Tenis Court",
                Location = "Beograd",
                PricePerHour = 2000,
                SportId = 1,
                Sport = sport
            };
            _courtsRepoMock.Setup(r => r.GetByIdWithSport(1)).Returns(court);

            var result = await _controller.GetById(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<CourtDto>(ok.Value);
            Assert.Equal("Tenis Court", dto.Name);
            Assert.Equal("Beograd", dto.Location);
            Assert.Equal("Tenis", dto.SportName);
        }

        [Fact]
        public async Task Create_WhenValidationFails_ReturnsBadRequest()
        {
            var request = new CreateCourtRequest { Name = "", SportId = 0 };
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Name", "Court name is required.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateCourtRequest>())).Returns(failedValidation);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            _courtsRepoMock.Verify(r => r.Add(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenSportNotFound_ReturnsNotFound()
        {
            var request = new CreateCourtRequest { Name = "Teren", SportId = 99 };
            _sportsRepoMock.Setup(r => r.GetById(99)).Returns((Sport?)null);

            var result = await _controller.Create(request);

            Assert.IsType<NotFoundObjectResult>(result);
            _courtsRepoMock.Verify(r => r.Add(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Create_AddsCourtAndCallsSaveChanges()
        {
            var sport = new Sport { SportId = 1, Name = "Tenis" };
            var request = new CreateCourtRequest
            {
                Name = "Teren 1",
                Location = "Beograd",
                PricePerHour = 1500,
                SportId = 1
            };
            _sportsRepoMock.Setup(r => r.GetById(1)).Returns(sport);

            var result = await _controller.Create(request);

            _courtsRepoMock.Verify(r => r.Add(It.IsAny<Court>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Update_WhenValidationFails_ReturnsBadRequest()
        {
            var request = new CreateCourtRequest { Name = "", SportId = 0 };
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Name", "Court name is required.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateCourtRequest>())).Returns(failedValidation);

            var result = await _controller.Update(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
            _courtsRepoMock.Verify(r => r.Update(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenNotFound_ReturnsNotFound()
        {
            _courtsRepoMock.Setup(r => r.GetById(99)).Returns((Court?)null);

            var result = await _controller.Delete(99);

            Assert.IsType<NotFoundObjectResult>(result);
            _courtsRepoMock.Verify(r => r.Remove(It.IsAny<Court>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenFound_CallsRemoveAndSaveChanges()
        {
            var court = new Court { CourtId = 1, Name = "Teren 1" };
            _courtsRepoMock.Setup(r => r.GetById(1)).Returns(court);

            var result = await _controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            _courtsRepoMock.Verify(r => r.Remove(court), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.Once);
        }

        private List<Court> SampleCourts() => new()
        {
            new Court { CourtId = 1, Name = "Centralni teren", IsIndoor = true },
            new Court { CourtId = 2, Name = "Padel Arena", IsIndoor = false },
            new Court { CourtId = 3, Name = "Teniski centar Novak", IsIndoor = true }
        };

        [Fact]
        public void FilterByName_WhenNameIsNull_ReturnsAllCourts()
        {
            var result = SampleCourts().FilterByName(null);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void FilterByName_WhenNameMatchesPart_ReturnsFilteredCourts()
        {
            var result = SampleCourts().FilterByName("teren");
            Assert.Equal(1, result.Count());
        }

        [Fact]
        public void FilterByIndoor_WhenTrue_ReturnsOnlyIndoorCourts()
        {
            var result = SampleCourts().FilterByIndoor(true);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void FilterByIndoor_WhenFalse_ReturnsOnlyOutdoorCourts()
        {
            var result = SampleCourts().FilterByIndoor(false);
            Assert.Single(result);
        }

    }
}
