using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameAndDot.Shared.Models
{
    public class PointData
    {
        public string Username { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public string Color { get; set; } = string.Empty;
    }
}
