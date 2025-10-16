using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Atributes
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class EndpointAttribute : Attribute
    {
        public EndpointAttribute() { }
    }
}
