using MiniHttpServer.Settings;
using MiniHttpServer.Shared;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace MiniHttpServer.Server 
{
    public class HttpServer
    {
        private HttpListener _listener = new();
        private JsonEntity _config;
        private CancellationToken _token;

        public HttpServer(JsonEntity config) { _config = config; }

        public void Start(CancellationToken token)
        {
            _token = token;
            _listener = new HttpListener();
            string url = "http://" + _config.Domain + ":" + _config.Port + "/";
            _listener.Prefixes.Add(url);
            _listener.Start();
            Console.WriteLine("Сервер запущен! Проверяй в браузере: " + url);
            Receive();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private void Receive()
        {
            _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        }

        protected async void ListenerCallback(IAsyncResult result)
        {
            if (_listener.IsListening && !_token.IsCancellationRequested)
            {
                var context = _listener.EndGetContext(result);
                var response = context.Response;
                byte[]? buffer = null;
                var request = context.Request;

                string path = request.Url.AbsolutePath.Trim('/');

                if (path == null || path == "/")
                    buffer = GetResponseBytes.Invoke($"Public/index.html");

                if (path == _config.SearcherUri)
                    buffer = GetResponseBytes.Invoke("searcher.html");
                else if (path == _config.ChatGPTUri)
                    buffer = GetResponseBytes.Invoke("chatgpt.html");
                else if (path == _config.OlaraUri)
                    buffer = GetResponseBytes.Invoke("login.html");
                else
                {
                    buffer = GetResponseBytes.Invoke(path);
                }

                response.ContentType = MiniHttpServer.Shared.ContentType.GetContentType(path.Trim('/'));

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

                if(response.StatusCode == 200)
                    Console.WriteLine($"Запрос обработан: {request.Url.AbsolutePath} - Status: {response.StatusCode}");
                else
                    Console.WriteLine($"Ошибка запроса: {request.Url.AbsolutePath} - Status: {response.StatusCode}");

                if (!_token.IsCancellationRequested)
                    Receive();
            }
        }
    }
}