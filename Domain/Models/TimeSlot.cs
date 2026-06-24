using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Models
{
    public class TimeSlot
    {
        public int TimeSlotId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int CourtId { get; set; }
        public Court Court { get; set; } = null!;
    }
}
