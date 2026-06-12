using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Models
{
    public class Court
    {
        public int CourtId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public bool IsIndoor { get; set; }


        public int SportId { get; set; }
        public Sport Sport { get; set; }
    }
}
