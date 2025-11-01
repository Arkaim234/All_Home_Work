using MiniHttpServer.Frimework.Core.HttpResponse;
using System.Net;
using System.Reflection;
using System.Text;
using MiniTemplateEngine;

namespace MiniHttpServer.Frimework.Core.HttpResponse
{
    public class PageResult : IActionResult
    {
        private readonly string _pathTemplate;
        private readonly object? _data;
        public int _statusCode;


        public PageResult(string pathTemplate, object? data, int statusCode = 200)
        {
            _pathTemplate = pathTemplate;
            _data = data;
            _statusCode = statusCode;
        }

        public async Task ExecuteAsync(HttpListenerContext context)
        {
            // context.Response.ContentType = "application/html";
            // context.Response.StatusCode = "200";
            // return TepmlatorEngine.GetByFile(pathTemplate, data)

            // TODO: доработать логику в EndpointHandler
            // TODO: вызов методов шаблонизатора
            // TODO: реализовать JsonResult
            // Создать проект с тестами для  MiniHttpServer.Framework.UnitTests
            // покрыть тестами класс HttpServer

            // Подключаем шаблонизатор (в проекте уже есть iniTemplateEngine)
            IHtmlTemplateRenderer renderer = new HtmlTemplateRenderer();

            // Рендерим шаблон в строку
            string html = renderer.RenderFromFile(_pathTemplate, _data ?? new { });

            // Кодируем в байты
            var bytes = Encoding.UTF8.GetBytes(html);

            // Устанавливаем заголовки и отправляем
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.StatusCode = _statusCode;
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }
    
    }
}