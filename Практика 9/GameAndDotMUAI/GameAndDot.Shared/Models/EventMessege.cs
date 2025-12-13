using GameAndDot.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XProtocol;
using XProtocol.Serializator;

namespace GameAndDot.Shared.Models
{
    public class EventMessege
    {
        [XField(1)]
        public EventType Type {  get; set; }
        [XField(2)]
        public string Id { get; set; }
        [XField(3)]
        public string Username { get; set; }
        [XField(4)]
        public List<string> Players { get; set; } = new();
        [XField(5)]
        public int X { get; set; }
        [XField(6)]
        public int Y { get; set; }
        [XField(7)]
        public string Color { get; set; } = string.Empty;
        [XField(8)]
        public Dictionary<string, string> PlayerColors { get; set; } = new();
        [XField(9)]
        public List<PointData> Points { get; set; } = new();
    }
}
