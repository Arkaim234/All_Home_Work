using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameAndDot.Shared.Models
{
    public class ClientObject
    {
        protected internal string Id { get; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        private readonly TcpClient _client;
        private readonly ServerObject _server;
        private readonly NetworkStream _stream;

        private readonly List<byte> _buffer = new();

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            _client = tcpClient;
            _server = serverObject;
            _stream = _client.GetStream();
            Color = GenerateRandomColor();
        }

        private string GenerateRandomColor()
        {
            Random random = new Random();
            return $"#{random.Next(0x1000000):X6}";
        }

        public async Task ProcessAsync()
        {
            var recvBuf = new byte[1024];

            try
            {
                while (true)
                {
                    int read = await _stream.ReadAsync(recvBuf, 0, recvBuf.Length);
                    if (read == 0)
                    {
                        break;
                    }

                    _buffer.AddRange(recvBuf.AsSpan(0, read).ToArray());

                    while (true)
                    {
                        int endIndex = GameProtocol.FindPacketEndIndex(_buffer);
                        if (endIndex == -1)
                            break;

                        int packetLength = endIndex + 2;
                        byte[] packetBytes = _buffer.Take(packetLength).ToArray();
                        _buffer.RemoveRange(0, packetLength);

                        EventMessege? messageRequest = null;
                        try
                        {
                            messageRequest = GameProtocol.DeserializeMessage(packetBytes);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка парсинга XPacket: " + ex.Message);
                            continue;
                        }

                        if (messageRequest == null)
                            continue;

                        await HandleMessageAsync(messageRequest);
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
                    await _server.BroadcastMessageAsync(disconnectMessage, Id);
                }

                _server.RemoveConnection(Id);
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
                    foreach (var client in _server.Clients)
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
                        Players = _server.Clients.Select(c => c.Username).ToList(),
                        Points = _server.points.Select(p => new PointData
                        {
                            Username = p.Username,
                            X = p.X,
                            Y = p.Y,
                            Color = p.Color
                        }).ToList(),
                        PlayerColors = playerColors
                    };

                    await _server.BroadcastMessageAllAsync(messageResponse);
                    break;

                case EventType.PointedPlaced:
                    Console.WriteLine($"Сервер получил точку: {messageRequest.Username}, ({messageRequest.X},{messageRequest.Y}), цвет: {messageRequest.Color}");

                    _server.points.Add(new PointData
                    {
                        Username = Username,
                        X = messageRequest.X,
                        Y = messageRequest.Y,
                        Color = messageRequest.Color
                    });

                    Console.WriteLine("Сервер пересылает точку всем...");
                    await _server.BroadcastMessageAsync(messageRequest, Id);
                    break;
            }
        }

        public async Task SendMessageAsync(EventMessege message)
        {
            byte[] packetBytes = GameProtocol.SerializeMessage(message);
            await _stream.WriteAsync(packetBytes, 0, packetBytes.Length);
            await _stream.FlushAsync();
        }

        protected internal void Close()
        {
            try
            {
                _stream.Close();
                _client.Close();
            }
            catch { }
        }
    }
}
