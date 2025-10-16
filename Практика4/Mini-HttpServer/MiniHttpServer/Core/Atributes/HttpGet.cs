using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Atributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class HttpGet : Attribute
    {
        public string Route { get; }
        public HttpGet(string route) {
            Route = route;
        }

        public HttpGet() { }
    }
}

