using MiniHttpServer;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;

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


    private async void ListenerCallback(IAsyncResult result)
    {
        if (_listener.IsListening && !_token.IsCancellationRequested)
        {
            var context = _listener.EndGetContext(result);
            var response = context.Response;
            string? responseText = null;
            var request = context.Request;

            string path = request.Url.AbsolutePath.Trim('/');

            if (path == _config.SearcherUri)
                responseText = GetResponseText(_config.SearcherPath);
            else if (path == _config.ChatGPTUri)
                responseText = GetResponseText(_config.ChatGPTPath);
            else
            {
                if (string.IsNullOrEmpty(path) || path == "index")
                    path = "index.html";

                string filePath = Path.Combine("Public", path);

                if (Path.GetExtension(filePath) == string.Empty)
                    filePath += ".html";

                responseText = GetResponseText(filePath);
            }

            if (responseText == null)
            {
                response.StatusCode = 404;
                responseText = "Ошибка сервера. Страница не найдена";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;

            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            await output.FlushAsync();

            Console.WriteLine($"Запрос обработан: {request.Url.AbsolutePath}");

            if (!_token.IsCancellationRequested)
                Receive();
        }
    }


    public string? GetResponseText(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine($"Файл не найден: {path}");
                return null;
            }

            string content = System.IO.File.ReadAllText(path);
            Console.WriteLine($"Успешно загружен файл: {path}");
            return content;
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Директория не найдена");
            return null;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Файл не найден: {path}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении файла {path}: {ex.Message}");
            return null;
        }
    }
}