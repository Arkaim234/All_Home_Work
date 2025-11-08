using MiniHttpServer.Frimework.Core.Abstracts;
using MiniHttpServer.Frimework.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Frimework.Core.Handlers
{
    class StaticFilesHandler : Handler
    {
        public async override Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var isGetNethod = context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
            var isStaticFile = request.Url.AbsolutePath.Split('/').Any(x => x.Contains("."));

            if (isGetNethod && isStaticFile)
            {
                var response = context.Response;

                byte[]? buffer = null;

                string path = request.Url.AbsolutePath.Trim('/');
                /*
                if (path == null || path == "/")
                    buffer = GetResponseBytes.Invoke($"Public/index.html");
                */
                buffer = GetResponseBytes.Invoke(path);

                response.ContentType = MiniHttpServer.Frimework.Shared.ContentType.GetContentType(path.Trim('/'));

                if (buffer == null)
                {
                    response.StatusCode = 404;
                    string errorText = "<html><body>404 - Not Found</html></body>";
                    buffer = Encoding.UTF8.GetBytes(errorText);
                }

                response.ContentLength64 = buffer.Length;

                using Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                await output.FlushAsync();

                if (response.StatusCode == 200)
                    Console.WriteLine($"Запрос обработан: {request.Url.AbsolutePath} - Status: {response.StatusCode}");
                else
                    Console.WriteLine($"Ошибка запроса: {request.Url.AbsolutePath} - Status: {response.StatusCode}");
            }
     
            // передача запроса дальше по цепи при наличии в ней обработчиков
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }
    }
}
