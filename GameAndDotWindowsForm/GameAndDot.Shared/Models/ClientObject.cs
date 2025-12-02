using GameAndDot.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameAndDot.Shared.Models
{
    public class ClientObject
    {
        protected internal string Id { get; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = String.Empty;
        protected internal StreamWriter Writer { get; }
        protected internal StreamReader Reader { get; }
        public string Color { get; set; } = string.Empty;

        TcpClient client;
        ServerObject server; // объект сервера

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            client = tcpClient;
            server = serverObject;
            // получаем NetworkStream для взаимодействия с сервером
            var stream = client.GetStream();
            // создаем StreamReader для чтения данных
            Reader = new StreamReader(stream);
            // создаем StreamWriter для отправки данных
            Writer = new StreamWriter(stream);
            Color = GenerateRandomColor();
        }
        private string GenerateRandomColor()
        {
            Random random = new Random();
            return $"#{random.Next(0x1000000):X6}";
        }
        public async Task ProcessAsync()
        {
            try
            {
                while (true)
                {
                    var jsonRequest = await Reader.ReadLineAsync();
                    Console.WriteLine($"Сервер получил: {jsonRequest}");

                    var messageRequest = JsonSerializer.Deserialize<EventMessege>(jsonRequest);
                    if (messageRequest == null) continue;

                    switch (messageRequest.Type)
                    {
                        case Enums.EventType.PlayerConected:
                            // Сохраняем имя из сообщения
                            Username = messageRequest.Username;
                            var playerColors = new Dictionary<string,string>();
                            foreach (var client in server.Clients)
                            {
                                if (!string.IsNullOrEmpty(client.Color))
                                {
                                    playerColors[client.Username] = client.Color;
                                }
                            }
                            // Сохраняем цвет из сообщения (если есть), иначе генерируем
                            if (!string.IsNullOrEmpty(messageRequest.Color))
                                Color = messageRequest.Color;

                            Console.WriteLine($"{Username} вошел в чат, цвет: {Color}");

                            var messageResponse = new EventMessege()
                            {
                                Type = EventType.PlayerConected,
                                Username = Username,
                                Id = Id,
                                Players = server.Clients.Select(c => c.Username).ToList(),
                                Points = server.points.Select(p => new PointData
                                {
                                    Username = p.Username,
                                    X = p.X,
                                    Y = p.Y,
                                    Color = p.Color
                                }).ToList(),
                                PlayerColors = playerColors
                            };
                            string jsonResponse = JsonSerializer.Serialize(messageResponse);
                            Console.WriteLine($"Сервер отправляет: {jsonResponse}");
                            await server.BroadcastMessageAllAsync(jsonResponse);
                            break;

                        case Enums.EventType.PointedPlaced:
                            Console.WriteLine($"Сервер получил точку: {messageRequest.Username}, ({messageRequest.X},{messageRequest.Y}), цвет: {messageRequest.Color}");

                            server.points.Add(new PointData
                            {
                                Username = Username, 
                                X = messageRequest.X,
                                Y = messageRequest.Y,
                                Color = messageRequest.Color
                            });

                            Console.WriteLine($"Сервер пересылает точку всем...");
                            await server.BroadcastMessageAsync(jsonRequest, Id);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка: {e.Message}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
            }
            finally
            {
                server.RemoveConnection(Id);
            }
        }
        // закрытие подключения
        protected internal void Close()
        {
            Writer.Close();
            Reader.Close();
            client.Close();
        }
    }
}
