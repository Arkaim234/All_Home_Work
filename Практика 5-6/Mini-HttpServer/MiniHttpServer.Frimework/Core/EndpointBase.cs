using MiniHttpServer.Frimework.Core.HttpResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Frimework.Core
{
    public abstract class EndpointBase
    {
        protected HttpListenerContext Context { get; private set; }

        internal void SetContext(HttpListenerContext context)
        {
            Context = context;
        }


        protected IActionResult Page(string pathTemplate, object data) => new PageResult(pathTemplate, data);

        protected IActionResult Json(object data) => new JsonResult(data);
    }
}
