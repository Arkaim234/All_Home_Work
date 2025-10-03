using MiniHttpServer;
using System.ComponentModel;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

public class Program
{
    public static async Task Main(string[] args)
    {
        JsonEntity settings = null;
        string fs = null;

        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        await Task.Run(() =>
        {
            try
            {
                fs = System.IO.File.ReadAllText("settings.json");
                settings = JsonSerializer.Deserialize<JsonEntity>(fs);

                if (settings == null)
                {
                    Console.WriteLine("Ошибка: настройки не загружены");
                    return;
                }

                if (!System.IO.File.Exists(settings.SearcherPath))
                    Console.WriteLine($"Файл {settings.SearcherPath} не найден");
                if (!System.IO.File.Exists(settings.ChatGPTPath))
                    Console.WriteLine($"Файл {settings.ChatGPTPath} не найден");
                if (!System.IO.File.Exists("Public/index.html"))
                    Console.WriteLine($"Файл Public/index.html не найден");

                var searcherurl = $"http://{settings.Domain}:{settings.Port}/{settings.SearcherUri}/";
                var chatgpturl = $"http://{settings.Domain}:{settings.Port}/{settings.ChatGPTUri}/";

                HttpServer server = new HttpServer(settings);

                Console.WriteLine($"Запуск сервера на {settings.Domain}:{settings.Port}");
                server.Start(token);

                Console.WriteLine("Сервер запущен. Введите '/stop' для остановки");
                while (!token.IsCancellationRequested)
                {
                    var input = Console.ReadLine();
                    if (input == "/stop")
                    {
                        Console.WriteLine("Получена команда остановки...");
                        cts.Cancel();
                        break;
                    }
                }

                server.Stop();
                Console.WriteLine("Сервер остановлен");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Файл settings.json не найден");
                return;
            }
            catch (JsonException)
            {
                Console.WriteLine("Ошибка формата JSON");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
                return;
            }
        });
    }
}