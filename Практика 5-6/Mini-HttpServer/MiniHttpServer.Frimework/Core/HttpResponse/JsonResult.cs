using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Frimework.Core.HttpResponse
{
    public class JsonResult : IActionResult
    {
        public object? Data { get; }
        public int StatusCode { get; }

        public JsonResult(object? data, int statusCode = 200)
        {
            Data = data;
            StatusCode = statusCode;
        }

        public async Task ExecuteAsync(HttpListenerContext context)
        {
            // Указываем заголовки
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = StatusCode;

            // Сериализация объекта в JSON 
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
            };

            string json = JsonSerializer.Serialize(Data, options);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Записываем тело ответа
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}