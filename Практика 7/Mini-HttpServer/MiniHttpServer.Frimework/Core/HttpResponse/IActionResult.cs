using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Frimework.Core.HttpResponse
{
    public interface IActionResult
    {
        Task ExecuteAsync(HttpListenerContext context);
    }
}
