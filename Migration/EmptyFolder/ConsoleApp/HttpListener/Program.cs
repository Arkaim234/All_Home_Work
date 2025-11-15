using System.Net;

public class HttpServer
{
    private Settings Settings;

    private HttpListener _listener = new();

    private CancellationTokenSource _cts;

    public HttpServer()
    {
        Settings = Settings.Instance();
    }

    public void Start(ref bool keepAlive)
    {
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://" + Settings.Domain + ":" + Settings.Port + "/");
        keepAlive = true;
        _listener.Start();
        Console.WriteLine($"http://{Settings.Domain}:{Settings.Port}/ - чтобы зайти на index.html");
        Console.WriteLine($"http://{Settings.Domain}:{Settings.Port}/Delta/ - чтобы зайти на Delta");
        Console.WriteLine("Сервер запущен");

        _ = ListenAsync(_cts.Token);
    }

    public void Stop(ref bool keepRunning)
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        Console.WriteLine("Сервер остановлен");
        keepRunning = false;
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var context = await _listener.GetContextAsync();
            _ = ListenerCallback(context, token);
        }
    }

    private async Task ListenerCallback(HttpListenerContext context, CancellationToken token)
    {
        try
        {
            // отправляемый в ответ код htmlвозвращает
            var request = context.Request;
            var response = context.Response;

            string path = GetPath.Path(request, Settings.PublicDirectoryPath);

            byte[] buffer = null;
            try
            {
                buffer = GetResponse.GetBytes(path);
                response.ContentType = Shared.ContentType.GetContentType(path);
                response.ContentLength64 = buffer.Length;

                using Stream output = response.OutputStream;
                // отправляем данные
                await output.WriteAsync(buffer);
                await output.FlushAsync();

                Console.WriteLine($"Запрос обработан {path}");
            }

            catch (FileNotFoundException)
            {
                Console.WriteLine("Файл не найден");
                response.StatusCode = 404;
                response.ContentLength64 = 0;
                using Stream output = response.OutputStream;
                await output.FlushAsync();
            }

            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Директория не найдена: {ex.Message}");
                response.StatusCode = 404;
                response.ContentLength64 = 0;
                using Stream output = response.OutputStream;
                await output.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                response.StatusCode = 500;
                response.ContentLength64 = 0;
                using Stream output = response.OutputStream;
                await output.FlushAsync();
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
