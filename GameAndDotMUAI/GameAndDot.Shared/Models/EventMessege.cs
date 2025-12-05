using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameAndDot.Shared.Enums;

namespace GameAndDot.Shared.Models
{
    public class EventMessege
    {
        public EventType Type {  get; set; }
        public string Id { get; set; }
        public string Username { get; set; }
        public List<string> Players { get; set; } = new();
        public int X { get; set; }
        public int Y { get; set; }
        public string Color { get; set; } = string.Empty;
        public Dictionary<string, string> PlayerColors { get; set; } = new();
        public List<PointData> Points { get; set; } = new();
    }
}
