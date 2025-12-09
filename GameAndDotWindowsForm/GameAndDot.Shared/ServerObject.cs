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
        private TcpListener tcpListener; // сервер для прослушивания
        public List<PointData> points { get; private set; } = new();
        public List<ClientObject> Clients { get; private  set; } = new(); // все подключения
        public ServerObject()
        {
            var config = SettingsManager.GetInstance();
            tcpListener = new TcpListener(config.HostAddress, config.PortNumber);
        }
        protected internal void RemoveConnection(string id)
        {
            ClientObject? client = Clients.FirstOrDefault(c => c.Id == id);
            if (client != null)
            {
                string username = client.Username;
                Clients.Remove(client);
                client.Close();

                points.RemoveAll(p => p.Username == username);
            }
        }
        // прослушивание входящих подключений
        public async Task ListenAsync()
        {
            try
            {
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Clients.Add(clientObject);
                    Task.Run(clientObject.ProcessAsync);
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
            foreach (var client in Clients)
            {
                if (client.Id != id) // не отправляем обратно отправителю
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
            tcpListener.Stop(); //остановка сервера
        }
    }
}
