using GameAndDot.Shared;

namespace GameAndDot.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Server starting");


            ServerObject server = new ServerObject();// создаем сервер
            await server.ListenAsync(); // запускаем сервер
        }
    }  
}
