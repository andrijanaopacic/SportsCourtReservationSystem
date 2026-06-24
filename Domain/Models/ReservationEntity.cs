using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Models
{
    public class ReservationEntity
    {
        public int ReservationId { get; set; }
        public ReservationStatus Status { get; set; }
        public decimal TotalPrice { get; set; }

        public DateOnly Date { get; set; }
        public List<ReservationItem> ReservationItems { get; set; } = new();

        public string ApplicationUserId { get; set; } = string.Empty;
        //public ApplicationUser? User { get; set; }
    }

}
