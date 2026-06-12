using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Models
{
    public class Sport
    {
        public int SportId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }


        public List<Court> Courts { get; set; } = new();
    }
}
