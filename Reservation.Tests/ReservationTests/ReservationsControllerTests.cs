using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Reservation.API.Controllers;
using Reservation.API.DTOs.Reservation;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System.Security.Claims;

namespace Reservation.Tests.ReservationTests
{
    public class ReservationsControllerTests
    {
        private readonly Mock<IReservationRepository> _reservationsRepoMock;
        private readonly Mock<ITimeSlotRepository> _timeSlotsRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IValidator<CreateReservationRequest>> _validatorMock;
        private readonly ReservationsController _controller;

        private const string TestUserId = "user-123";

        public ReservationsControllerTests()
        {
            _reservationsRepoMock = new Mock<IReservationRepository>();
            _timeSlotsRepoMock = new Mock<ITimeSlotRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(u => u.Reservations).Returns(_reservationsRepoMock.Object);
            _uowMock.SetupGet(u => u.TimeSlots).Returns(_timeSlotsRepoMock.Object);

            _cacheMock = new Mock<ICacheService>();
            _cacheMock.Setup(c => c.GetAsync<ReservationDto>(It.IsAny<string>()))
                      .ReturnsAsync((ReservationDto?)null);
            _cacheMock.Setup(c => c.GetAsync<List<ReservationDto>>(It.IsAny<string>()))
                      .ReturnsAsync((List<ReservationDto>?)null);
            _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()))
                      .Returns(Task.CompletedTask);
            _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
                      .Returns(Task.CompletedTask);

            _validatorMock = new Mock<IValidator<CreateReservationRequest>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateReservationRequest>()))
                          .Returns(new ValidationResult());

            _controller = new ReservationsController(_uowMock.Object, _cacheMock.Object, _validatorMock.Object);

            // Postavljamo autentikovanog korisnika
            SetAuthenticatedUser(TestUserId);
        }

        // ─── Helper metode ───────────────────────────────────────────────

        private void SetAuthenticatedUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static Court MakeCourt(int id = 1, string name = "Tenis Teren") =>
            new Court { CourtId = id, Name = name };

        private static TimeSlot MakeSlot(int id, DateOnly date, bool isAvailable = true, Court? court = null) =>
            new TimeSlot
            {
                TimeSlotId = id,
                Date = date,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                Price = 1500,
                TotalPrice = 3000,
                IsAvailable = isAvailable,
                CourtId = 1,
                Court = court ?? MakeCourt()
            };

        private static ReservationEntity MakeReservation(
            int id,
            string userId,
            ReservationStatus status,
            List<ReservationItem>? items = null,
            DateOnly? date = null)
        {
            var r = new ReservationEntity
            {
                ReservationId = id,
                ApplicationUserId = userId,
                Status = status,
                Date = date ?? DateOnly.FromDateTime(DateTime.Today),
                ReservationItems = items ?? new List<ReservationItem>()
            };
            r.TotalPrice = r.ReservationItems.Sum(i => i.Price);
            return r;
        }

        // ─── GET ALL ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_WhenCacheHit_ReturnsCachedData()
        {
            var cached = new List<ReservationDto> { new ReservationDto { ReservationId = 1 } };
            _cacheMock.Setup(c => c.GetAsync<List<ReservationDto>>("reservations_all"))
                      .ReturnsAsync(cached);

            var result = await _controller.GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ReservationDto>>(ok.Value);
            Assert.Single(returned);
            _reservationsRepoMock.Verify(r => r.GetAll(), Times.Never);
        }

        [Fact]
        public async Task GetAll_WhenCacheMiss_ReturnsFromRepository()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var reservation = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot } });

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { reservation });

            var result = await _controller.GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<ReservationDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetAll_WhenStatusFilter_DoesNotUseCache()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var upcomingRes = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot } });
            var cancelledRes = MakeReservation(2, TestUserId, ReservationStatus.CANCELLED);

            _reservationsRepoMock.Setup(r => r.GetAll())
                .Returns(new List<ReservationEntity> { upcomingRes, cancelledRes });

            var result = await _controller.GetAll("UPCOMING");

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<ReservationDto>>(ok.Value);
            Assert.Single(list);
            _cacheMock.Verify(c => c.GetAsync<List<ReservationDto>>("reservations_all"), Times.Never);
        }

        [Fact]
        public async Task GetAll_WhenNoStatusFilter_SetsCache()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot } });

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { res });

            await _controller.GetAll(null);

            _cacheMock.Verify(
                c => c.SetAsync(
                    "reservations_all",
                    It.IsAny<List<ReservationDto>>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);
        }

        // ─── GET BY ID ────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(99)).Returns((ReservationEntity?)null);

            var result = await _controller.GetById(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_WhenFound_ReturnsOkWithDto()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot } });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ReservationDto>(ok.Value);
            Assert.Equal(1, dto.ReservationId);
            Assert.Equal(TestUserId, dto.ApplicationUserId);
        }

        [Fact]
        public async Task GetById_WhenCacheHit_ReturnsCachedData()
        {
            var cached = new ReservationDto { ReservationId = 5, Status = "UPCOMING" };
            _cacheMock.Setup(c => c.GetAsync<ReservationDto>("reservation_5"))
                      .ReturnsAsync(cached);

            var result = await _controller.GetById(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ReservationDto>(ok.Value);
            Assert.Equal(5, dto.ReservationId);
            _reservationsRepoMock.Verify(r => r.GetByIdWithItems(It.IsAny<int>()), Times.Never);
        }

        // ─── GET MINE ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetMine_WhenCacheHit_ReturnsCachedData()
        {
            var cacheKey = $"reservations_user_{TestUserId}";
            var cached = new List<ReservationDto> { new ReservationDto { ReservationId = 1 } };
            _cacheMock.Setup(c => c.GetAsync<List<ReservationDto>>(cacheKey))
                      .ReturnsAsync(cached);

            var result = await _controller.GetMine();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<ReservationDto>>(ok.Value);
            Assert.Single(list);
            _reservationsRepoMock.Verify(r => r.GetByUser(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetMine_WhenCacheMiss_ReturnsOnlyCurrentUserReservations()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var myRes = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot } });

            _reservationsRepoMock.Setup(r => r.GetByUser(TestUserId))
                                 .Returns(new List<ReservationEntity> { myRes });

            var result = await _controller.GetMine();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<ReservationDto>>(ok.Value);
            Assert.Single(list);
            Assert.All(list, dto => Assert.Equal(TestUserId, dto.ApplicationUserId));
        }

        [Fact]
        public async Task GetMine_SetsUserCache()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot } });

            _reservationsRepoMock.Setup(r => r.GetByUser(TestUserId))
                                 .Returns(new List<ReservationEntity> { res });

            await _controller.GetMine();

            _cacheMock.Verify(
                c => c.SetAsync(
                    $"reservations_user_{TestUserId}",
                    It.IsAny<List<ReservationDto>>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);
        }

        // ─── CREATE ───────────────────────────────────────────────────────

        [Fact]
        public async Task Create_WhenValidationFails_ReturnsBadRequest()
        {
            var request = new CreateReservationRequest { Items = new List<CreateReservationItemRequest>() };
            var failedValidation = new ValidationResult(new List<ValidationFailure>
            {
                new("Items", "Rezervacija mora imati barem jednu stavku.")
            });
            _validatorMock.Setup(v => v.Validate(It.IsAny<CreateReservationRequest>()))
                          .Returns(failedValidation);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            _reservationsRepoMock.Verify(r => r.Add(It.IsAny<ReservationEntity>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenTimeSlotNotFound_ReturnsBadRequest()
        {
            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 999 }
                }
            };
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(999)).Returns((TimeSlot?)null);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
            _reservationsRepoMock.Verify(r => r.Add(It.IsAny<ReservationEntity>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenTimeSlotInPast_ReturnsBadRequest()
        {
            var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
            var slot = MakeSlot(1, pastDate);

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_WhenTimeSlotNotAvailable_ReturnsBadRequest()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var unavailableSlot = MakeSlot(1, future, isAvailable: false);

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(unavailableSlot);

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_WhenSlotAlreadyReserved_ReturnsBadRequest()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);

            // Postoji aktivna rezervacija za isti slot
            var existingItem = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot };
            var existingRes = MakeReservation(10, "other-user", ReservationStatus.UPCOMING,
                new List<ReservationItem> { existingItem });

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll())
                .Returns(new List<ReservationEntity> { existingRes });

            var result = await _controller.Create(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_WhenValid_AddsReservationAndCallsSaveChanges()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _controller.Create(request);

            _reservationsRepoMock.Verify(r => r.Add(It.IsAny<ReservationEntity>()), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.AtLeastOnce);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Create_WhenValid_SetsTotalPriceCorrectly()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot1 = MakeSlot(1, future);
            slot1.TotalPrice = 3000;
            var slot2 = MakeSlot(2, future);
            slot2.TotalPrice = 2000;

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 },
                    new CreateReservationItemRequest { TimeSlotId = 2 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot1);
            _timeSlotsRepoMock.Setup(r => r.GetById(2)).Returns(slot2);

            ReservationEntity? added = null;
            _reservationsRepoMock.Setup(r => r.Add(It.IsAny<ReservationEntity>()))
                                 .Callback<ReservationEntity>(res => added = res);

            await _controller.Create(request);

            Assert.NotNull(added);
            Assert.Equal(5000, added!.TotalPrice);
        }

        [Fact]
        public async Task Create_WhenValid_SetsSlotIsAvailableToFalse()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future, isAvailable: true);

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            await _controller.Create(request);

            Assert.False(slot.IsAvailable);
        }

        [Fact]
        public async Task Create_WhenValid_SetsFutureStatusToUpcoming()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            ReservationEntity? added = null;
            _reservationsRepoMock.Setup(r => r.Add(It.IsAny<ReservationEntity>()))
                                 .Callback<ReservationEntity>(res => added = res);

            await _controller.Create(request);

            Assert.NotNull(added);
            Assert.Equal(ReservationStatus.UPCOMING, added!.Status);
        }

        [Fact]
        public async Task Create_WhenTodaySlot_SetsStatusToActive()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var slot = MakeSlot(1, today);

            var request = new CreateReservationRequest
            {
                Date = today,
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            ReservationEntity? added = null;
            _reservationsRepoMock.Setup(r => r.Add(It.IsAny<ReservationEntity>()))
                                 .Callback<ReservationEntity>(res => added = res);

            await _controller.Create(request);

            Assert.NotNull(added);
            Assert.Equal(ReservationStatus.ACTIVE, added!.Status);
        }

        [Fact]
        public async Task Create_WhenValid_InvalidatesCache()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);

            var request = new CreateReservationRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<CreateReservationItemRequest>
                {
                    new CreateReservationItemRequest { TimeSlotId = 1 }
                }
            };

            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity>());
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            await _controller.Create(request);

            _cacheMock.Verify(c => c.RemoveAsync("reservations_all"), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync($"reservations_user_{TestUserId}"), Times.Once);
        }

        // ─── CANCEL ───────────────────────────────────────────────────────

        [Fact]
        public async Task Cancel_WhenNotFound_ReturnsNotFound()
        {
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(99)).Returns((ReservationEntity?)null);

            var result = await _controller.Cancel(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Cancel_WhenAlreadyCancelled_ReturnsBadRequest()
        {
            var res = MakeReservation(1, TestUserId, ReservationStatus.CANCELLED);
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);

            var result = await _controller.Cancel(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Cancel_WhenValid_SetsStatusToCancelled()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var item = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot };
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING, new List<ReservationItem> { item });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var result = await _controller.Cancel(1);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(ReservationStatus.CANCELLED, res.Status);
        }

        [Fact]
        public async Task Cancel_WhenValid_SetsSlotIsAvailableBackToTrue()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future, isAvailable: false);
            var item = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot };
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING, new List<ReservationItem> { item });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            await _controller.Cancel(1);

            Assert.True(slot.IsAvailable);
        }

        [Fact]
        public async Task Cancel_WhenValid_CallsSaveChangesAndInvalidatesCache()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var item = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot };
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING, new List<ReservationItem> { item });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            await _controller.Cancel(1);

            _uowMock.Verify(u => u.SaveChanges(), Times.AtLeastOnce);
            _cacheMock.Verify(c => c.RemoveAsync("reservations_all"), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync($"reservations_user_{TestUserId}"), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync("reservation_1"), Times.Once);
        }

        // ─── UPDATE ───────────────────────────────────────────────────────

        [Fact]
        public async Task Update_WhenNotFound_ReturnsNotFound()
        {
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(99)).Returns((ReservationEntity?)null);

            var request = new UpdateReservationRequest
            {
                Items = new List<CreateReservationItemRequest> { new() { TimeSlotId = 1 } }
            };

            var result = await _controller.Update(99, request);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_WhenCancelled_ReturnsBadRequest()
        {
            var res = MakeReservation(1, TestUserId, ReservationStatus.CANCELLED);
            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);

            var request = new UpdateReservationRequest
            {
                Items = new List<CreateReservationItemRequest> { new() { TimeSlotId = 5 } }
            };

            var result = await _controller.Update(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_WhenAddingAlreadyReservedSlot_ReturnsBadRequest()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot2 = MakeSlot(2, future);
            var itemInOtherRes = new ReservationItem { RowNumber = 1, TimeSlotId = 2, Price = 3000, TimeSlot = slot2 };
            var otherRes = MakeReservation(99, "other-user", ReservationStatus.UPCOMING,
                new List<ReservationItem> { itemInOtherRes });

            // Trenutna rezervacija korisnika
            var slot1 = MakeSlot(1, future);
            var item1 = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot1 };
            var currentRes = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { item1 });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(currentRes);
            _reservationsRepoMock.Setup(r => r.GetAll())
                .Returns(new List<ReservationEntity> { currentRes, otherRes });

            // Pokušavamo dodati slot 2 koji već drži otherRes
            var request = new UpdateReservationRequest
            {
                Items = new List<CreateReservationItemRequest>
                {
                    new() { TimeSlotId = 1 },
                    new() { TimeSlotId = 2 }
                }
            };

            var result = await _controller.Update(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_WhenRemovingAllItems_RemovesReservation()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot1 = MakeSlot(1, future);
            var item1 = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot1 };
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { item1 });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { res });
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot1);

            // Šaljemo prazan items lista — brišemo sve
            var request = new UpdateReservationRequest
            {
                Items = new List<CreateReservationItemRequest>()
            };

            var result = await _controller.Update(1, request);

            _reservationsRepoMock.Verify(r => r.Remove(res), Times.Once);
            _uowMock.Verify(u => u.SaveChanges(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Update_WhenValid_UpdatesAndCallsSaveChanges()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot1 = MakeSlot(1, future);
            var slot2 = MakeSlot(2, future);

            var item1 = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot1 };
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { item1 });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { res });
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot1);
            _timeSlotsRepoMock.Setup(r => r.GetById(2)).Returns(slot2);

            // Zamenjujemo slot 1 sa slot 2
            var request = new UpdateReservationRequest
            {
                Items = new List<CreateReservationItemRequest>
                {
                    new() { TimeSlotId = 2 }
                }
            };

            var result = await _controller.Update(1, request);

            _reservationsRepoMock.Verify(r => r.Update(It.IsAny<ReservationEntity>()), Times.AtLeastOnce);
            _uowMock.Verify(u => u.SaveChanges(), Times.AtLeastOnce);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_WhenValid_FreesRemovedSlots()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot1 = MakeSlot(1, future, isAvailable: false);
            var slot2 = MakeSlot(2, future, isAvailable: true);

            var item1 = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot1 };
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { item1 });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { res });
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot1);
            _timeSlotsRepoMock.Setup(r => r.GetById(2)).Returns(slot2);

            var request = new UpdateReservationRequest
            {
                Items = new List<CreateReservationItemRequest>
                {
                    new() { TimeSlotId = 2 }
                }
            };

            await _controller.Update(1, request);

            // Slot 1 koji smo uklonili treba biti dostupan ponovo
            Assert.True(slot1.IsAvailable);
        }

        [Fact]
        public async Task Update_WhenValid_InvalidatesAllRelatedCaches()
        {
            var future = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var slot = MakeSlot(1, future);
            var item = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot };
            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { item });

            _reservationsRepoMock.Setup(r => r.GetByIdWithItems(1)).Returns(res);
            _reservationsRepoMock.Setup(r => r.GetAll()).Returns(new List<ReservationEntity> { res });
            _timeSlotsRepoMock.Setup(r => r.GetById(1)).Returns(slot);

            var request = new UpdateReservationRequest
            {
                Items = new List<CreateReservationItemRequest> { new() { TimeSlotId = 1 } }
            };

            await _controller.Update(1, request);

            _cacheMock.Verify(c => c.RemoveAsync("reservations_all"), Times.AtLeastOnce);
            _cacheMock.Verify(c => c.RemoveAsync($"reservations_user_{TestUserId}"), Times.AtLeastOnce);
            _cacheMock.Verify(c => c.RemoveAsync("reservation_1"), Times.AtLeastOnce);
        }

        // ─── GET BY COURT AND DATE ─────────────────────────────────────────

        [Fact]
        public async Task GetByCourtAndDate_ReturnsOnlyNonCancelledForThatCourt()
        {
            var date = new DateOnly(2026, 7, 10);
            var slot = new TimeSlot
            {
                TimeSlotId = 1, CourtId = 1, Date = date,
                StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0),
                Court = MakeCourt(1)
            };
            var activeItem = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot };
            var activeRes = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING,
                new List<ReservationItem> { activeItem });

            var cancelledItem = new ReservationItem { RowNumber = 1, TimeSlotId = 2, Price = 2000,
                TimeSlot = new TimeSlot { TimeSlotId = 2, CourtId = 1, Date = date, Court = MakeCourt(1) } };
            var cancelledRes = MakeReservation(2, TestUserId, ReservationStatus.CANCELLED,
                new List<ReservationItem> { cancelledItem });

            _reservationsRepoMock.Setup(r => r.GetAll())
                .Returns(new List<ReservationEntity> { activeRes, cancelledRes });

            var result = await _controller.GetByCourtAndDate(1, date);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<List<ReservationDto>>(ok.Value);
            Assert.Single(list);
            Assert.Equal(1, list[0].ReservationId);
        }

        // ─── GET COURT CALENDAR ───────────────────────────────────────────

        [Fact]
        public void GetCourtCalendar_ReturnsGroupedCountByDate()
        {
            var july1 = new DateOnly(2026, 7, 1);
            var july2 = new DateOnly(2026, 7, 2);

            TimeSlot makeSlot(int id, DateOnly d) => new TimeSlot
            {
                TimeSlotId = id, CourtId = 1, Date = d,
                StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0),
                Court = MakeCourt(1)
            };

            var items = new List<ReservationItem>
            {
                new() { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = makeSlot(1, july1) },
                new() { RowNumber = 2, TimeSlotId = 2, Price = 3000, TimeSlot = makeSlot(2, july1) },
                new() { RowNumber = 3, TimeSlotId = 3, Price = 3000, TimeSlot = makeSlot(3, july2) },
            };

            var res = MakeReservation(1, TestUserId, ReservationStatus.UPCOMING, items);

            _reservationsRepoMock.Setup(r => r.GetAll())
                .Returns(new List<ReservationEntity> { res });

            var result = _controller.GetCourtCalendar(1, 2026, 7);

            var ok = Assert.IsType<OkObjectResult>(result);
            var calendar = Assert.IsAssignableFrom<List<CourtCalendarDayDTO>>(ok.Value);

            Assert.Equal(2, calendar.Count);
            Assert.Equal(2, calendar.First(d => d.Date == july1).ReservationCount);
            Assert.Equal(1, calendar.First(d => d.Date == july2).ReservationCount);
        }

        [Fact]
        public void GetCourtCalendar_ExcludesCancelledReservations()
        {
            var july1 = new DateOnly(2026, 7, 1);
            var slot = new TimeSlot
            {
                TimeSlotId = 1, CourtId = 1, Date = july1, Court = MakeCourt(1),
                StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0)
            };

            var cancelledItem = new ReservationItem { RowNumber = 1, TimeSlotId = 1, Price = 3000, TimeSlot = slot };
            var cancelledRes = MakeReservation(1, TestUserId, ReservationStatus.CANCELLED,
                new List<ReservationItem> { cancelledItem });

            _reservationsRepoMock.Setup(r => r.GetAll())
                .Returns(new List<ReservationEntity> { cancelledRes });

            var result = _controller.GetCourtCalendar(1, 2026, 7);

            var ok = Assert.IsType<OkObjectResult>(result);
            var calendar = Assert.IsAssignableFrom<List<CourtCalendarDayDTO>>(ok.Value);
            Assert.Empty(calendar);
        }
    }
}
