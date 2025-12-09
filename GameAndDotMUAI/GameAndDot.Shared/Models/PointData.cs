using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XProtocol.Serializator;

namespace GameAndDot.Shared.Models
{
    public class PointData
    {
        [XField(10)]
        public string Username { get; set; } = string.Empty;
        [XField(11)]
        public int X { get; set; }
        [XField(12)]
        public int Y { get; set; }
        [XField(13)]
        public string Color { get; set; } = string.Empty;
    }
}
