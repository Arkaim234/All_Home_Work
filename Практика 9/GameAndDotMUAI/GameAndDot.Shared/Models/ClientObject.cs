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
        public string Color { get; set; } = string.Empty;

        Socket _socket;
        ServerObject server; // объект сервера
        private readonly List<byte> _buffer = new();
        public ClientObject(Socket socket, ServerObject serverObject)
        {
            _socket = socket;
            server = serverObject;
            Color = GenerateRandomColor();
        }
        private string GenerateRandomColor()
        {
            Random random = new Random();
            return $"#{random.Next(0x1000000):X6}";
        }
        public async Task ProcessAsync()
        {
            var receiveBuffer = new byte[1024];
            try
            {
                while (true)
                {
                    var read = await _socket.ReceiveAsync(receiveBuffer,SocketFlags.None);
                    if (read == 0)
                    {
                        break;
                    }
                    _buffer.AddRange(receiveBuffer.AsSpan(0, read).ToArray());
                    while (true)
                    {
                        int endIndex = Protocol.GameProtocol.FindPacketEndIndex(_buffer);
                        if(endIndex == -1)
                            break;
                        int packetLength = endIndex + 2;
                        byte[] pucketBytes = _buffer.Take(packetLength).ToArray();
                        _buffer.RemoveRange(0, packetLength);

                        var messegeRequest = Protocol.GameProtocol.DeserializeMessage(pucketBytes);
                        if (messegeRequest == null)
                            continue;
                        await HandleMessageAsync(messegeRequest);
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
                if (!string.IsNullOrEmpty(Username))
                {
                    var disconnectMessage = new EventMessege
                    {
                        Type = EventType.PlayerDisconected,
                        Username = Username
                    };
                    await server.BroadcastMessageAsync(disconnectMessage, Id);
                }

                // Потом удаляем соединение
                server.RemoveConnection(Id);
            }
        }
        private async Task HandleMessageAsync(EventMessege messageRequest)
        {
            switch (messageRequest.Type)
            {
                case EventType.PlayerConected:
                    Username = messageRequest.Username;

                    if (!string.IsNullOrEmpty(messageRequest.Color))
                        Color = messageRequest.Color;

                    Console.WriteLine($"{Username} вошел в чат, цвет: {Color}");

                    var playerColors = new Dictionary<string, string>();
                    foreach (var client in server.Clients)
                    {
                        if (!string.IsNullOrEmpty(client.Color))
                        {
                            playerColors[client.Username] = client.Color;
                        }
                    }

                    var messageResponse = new EventMessege
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

                    await server.BroadcastMessageAllAsync(messageResponse);
                    break;

                case EventType.PointedPlaced:
                    Console.WriteLine($"Сервер получил точку: {messageRequest.Username}, ({messageRequest.X},{messageRequest.Y}), цвет: {messageRequest.Color}");

                    server.points.Add(new PointData
                    {
                        Username = Username,
                        X = messageRequest.X,
                        Y = messageRequest.Y,
                        Color = messageRequest.Color
                    });

                    Console.WriteLine("Сервер пересылает точку всем...");
                    await server.BroadcastMessageAsync(messageRequest, Id);
                    break;
            }
        }
        public async Task SendMessageAsync(EventMessege messege)
        {
            byte[] bytes = Protocol.GameProtocol.SerializeMessage(messege);
            await _socket.SendAsync(bytes);
        }
        // закрытие подключения
        protected internal void Close()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            _socket.Close();
        }
    }
}
