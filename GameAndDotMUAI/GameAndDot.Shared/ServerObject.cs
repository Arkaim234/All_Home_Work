using GameAndDot.Shared.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GameAndDot.Shared
{
    public class ServerObject
    {
        private Socket _listensocket; // сервер для прослушивания
        public List<PointData> points { get; private set; } = new();
        public List<ClientObject> Clients { get; private  set; } = new(); // все подключения
        public ServerObject()
        {
            var config = SettingsManager.GetInstance();
            var endPoint = new IPEndPoint(config.HostAddress, config.PortNumber);

            _listensocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _listensocket.Bind(endPoint);

            _listensocket.Listen(100);

            Console.WriteLine($"Сервер слушает {endPoint}");
        }
        protected internal void RemoveConnection(string id)
        {
            var client = Clients.FirstOrDefault(c => c.Id == id);
            if (client != null)
            {
                Clients.Remove(client);
                client.Close();

                Console.WriteLine($"{client.Username} покинул чат");

            }
        }
        // прослушивание входящих подключений
        public async Task ListenAsync()
        {
            try
            {
                _listensocket.Listen();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    Socket clientsocket = await _listensocket.AcceptAsync();

                    var clientObject = new ClientObject(clientsocket, this);
                    Clients.Add(clientObject);
                    _ = Task.Run(clientObject.ProcessAsync);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }

        // трансляция сообщения подключенным клиентам
        public async Task BroadcastMessageAsync(EventMessege message, string id)
        {
            foreach (var client in Clients.ToList())
            {
                if (client.Id != id) // если id клиента не равно id отправителя
                {
                    await client.SendMessageAsync(message);
                }
            }
        }
        public async Task BroadcastMessageAllAsync(EventMessege message)
        {
            foreach (var client in Clients)
            {
                await client.SendMessageAsync(message);
            }
        }

        // отключение всех клиентов
        protected internal void Disconnect()
        {
            foreach (var client in Clients)
            {
                client.Close(); //отключение клиента
            }
            _listensocket.Close(); //остановка сервера
        }
    }
}
