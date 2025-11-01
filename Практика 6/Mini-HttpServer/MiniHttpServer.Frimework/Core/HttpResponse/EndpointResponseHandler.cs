using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Frimework.Core.HttpResponse
{
    public static class EndpointResponseHandler
    {
        public static async Task HandleResultAsync(HttpListenerContext context, object? result)
        {
            try
            {
                if (result is IActionResult actionResult)
                {
                    await actionResult.ExecuteAsync(context);
                    return;
                }

                if (result is string text)
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.StatusCode = 200;
                    await WriteResponseAsync(context.Response, text);
                    return;
                }

                if (result is byte[] bytes)
                {
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.StatusCode = 200;
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    return;
                }

                if (result is null)
                {
                    context.Response.StatusCode = 204;
                    return;
                }

                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.StatusCode = 200;
                string json = JsonSerializer.Serialize(new { data = result });
                await WriteResponseAsync(context.Response, json);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.StatusCode = 500;
                await WriteResponseAsync(context.Response, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            await response.OutputStream.FlushAsync();
        }
    }
}
