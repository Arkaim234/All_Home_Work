using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Models;
using System.Net.Sockets;
using System.Text.Json;

namespace GameAndDot.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = SettingsManager.GetInstance();
            using TcpClient client = new TcpClient();
            Console.Write("Введите свое имя: ");
            string? userName = Console.ReadLine();
            Console.WriteLine($"Добро пожаловать, {userName}");
            StreamReader? Reader = null;
            StreamWriter? Writer = null;

            try
            {
                client.Connect(config.HostAddress, config.PortNumber); //подключение клиента
                Reader = new StreamReader(client.GetStream());
                Writer = new StreamWriter(client.GetStream());
                if (Writer is null || Reader is null) return;
                // запускаем новый поток для получения данных
                Task.Run(() => ReceiveMessageAsync(Reader));
                // запускаем ввод сообщений
                await SendMessageAsync(Writer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Writer?.Close();
            Reader?.Close();

            // отправка сообщений
            async Task SendMessageAsync(StreamWriter writer)
            {
                var connectMessege = new EventMessege
                {
                    Type = Shared.Enums.EventType.PlayerConected,
                    Username = userName
                };
                await writer.WriteLineAsync(JsonSerializer.Serialize(connectMessege));
                await writer.FlushAsync();

                while (true)
                {
                    string? input = Console.ReadLine();
                    if (input.StartsWith("point "))
                    {
                        var parts = input.Split(' ');
                        var pointMessage = new EventMessege
                        {
                            Type = EventType.PointedPlaced,
                            Username = userName,
                            X = int.Parse(parts[1]),
                            Y = int.Parse(parts[2])
                        };
                        await writer.WriteLineAsync(JsonSerializer.Serialize(pointMessage));
                        await writer.FlushAsync();
                    }
                }
            }
            // получение сообщений
            async Task ReceiveMessageAsync(StreamReader reader)
            {
                while (true)
                {
                    string? json = await reader.ReadLineAsync();
                    var message = JsonSerializer.Deserialize<EventMessege>(json);

                    switch(message.Type)
                    {
                        case EventType.PlayerConected:
                            Console.WriteLine($"{message.Username} подключился");
                            break; 
                        case EventType.PointedPlaced:
                            Console.WriteLine($"{message.Username}: ({message.X}, {message.Y})");
                            break;
                    }
                }
            }
            // чтобы полученное сообщение не накладывалось на ввод нового сообщения
            void Print(string message)
            {
                if (OperatingSystem.IsWindows())    // если ОС Windows
                {
                    var position = Console.GetCursorPosition(); // получаем текущую позицию курсора
                    int left = position.Left;   // смещение в символах относительно левого края
                    int top = position.Top;     // смещение в строках относительно верха
                                                // копируем ранее введенные символы в строке на следующую строку
                    Console.MoveBufferArea(0, top, left, 1, 0, top + 1);
                    // устанавливаем курсор в начало текущей строки
                    Console.SetCursorPosition(0, top);
                    // в текущей строке выводит полученное сообщение
                    Console.WriteLine(message);
                    // переносим курсор на следующую строку
                    // и пользователь продолжает ввод уже на следующей строке
                    Console.SetCursorPosition(left, top + 1);
                }
                else Console.WriteLine(message);
            }
        }
    }
}
