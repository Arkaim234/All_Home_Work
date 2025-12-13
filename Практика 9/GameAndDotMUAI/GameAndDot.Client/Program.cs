using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Models;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using GameAndDot.Shared.Protocol;

namespace GameAndDot.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = SettingsManager.GetInstance();
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.Write("Введите свое имя: ");
            string? userName = Console.ReadLine();
            Console.WriteLine($"Добро пожаловать, {userName}");

            try
            {
                socket.Connect(config.HostAddress, config.PortNumber); //подключение клиента
                _ = Task.Run(() => ReceiveLoopAsync(socket));
                // запускаем ввод сообщений
                await SendLoopAsync(socket, userName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                socket.Close();
            }

            async Task ReceiveLoopAsync(Socket socket)
            {
                // Буфер для чтения из сокета
                byte[] buffer = new byte[4096];
                // Накопитель текста между чтениями
                var pending = new StringBuilder();

                try
                {
                    while (true)
                    {
                        int bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);

                        if (bytesRead == 0)
                        {
                            Console.WriteLine("Соединение разорвано");
                            break;
                        }

                        string part = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        pending.Append(part);

                        while (true)
                        {
                            string full = pending.ToString();
                            int newlineIndex = full.IndexOf('\n');
                            if (newlineIndex == -1)
                                break; 

                            string jsonText = full.Substring(0, newlineIndex);

                            pending.Clear();
                            pending.Append(full.Substring(newlineIndex + 1));

                            if (string.IsNullOrWhiteSpace(jsonText))
                                continue;

                            EventMessege? message;
                            try
                            {
                                message = JsonSerializer.Deserialize<EventMessege>(jsonText);
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"Ошибка парсинга JSON: {ex.Message}");
                                Console.WriteLine($"Сырой текст: {jsonText}");
                                continue;
                            }

                            if (message is null)
                                continue;

                            switch (message.Type)
                            {
                                case EventType.PlayerConected:
                                    Console.WriteLine($"{message.Username} подключился");
                                    break;

                                case EventType.PointedPlaced:
                                    Console.WriteLine($"{message.Username}: ({message.X}, {message.Y})");
                                    break;

                                case EventType.PlayerDisconected:
                                    Console.WriteLine($"{message.Username} отключился");
                                    break;
                            }
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Ошибка сокета: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при приёме данных: {ex.Message}");
                }
            }

            async Task SendLoopAsync(Socket socket, string userName)
            {
                while (true)
                {
                    var connectMessege = new EventMessege
                    {
                        Type = Shared.Enums.EventType.PlayerConected,
                        Username = userName
                    };
                    string json = JsonSerializer.Serialize(connectMessege);
                    byte[] buffer = Encoding.UTF8.GetBytes(json + "\n");
                    ArraySegment<byte> result = new ArraySegment<byte>(buffer, 0, buffer.Length);
                    await socket.SendAsync(result, SocketFlags.None);
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
