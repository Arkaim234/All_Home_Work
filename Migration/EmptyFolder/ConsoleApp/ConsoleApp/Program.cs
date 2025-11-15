using System.Reflection;
using ConsoleApp.Models;
using MigrationLib;

namespace ConsoleApp
{
    /// <summary>
    /// Точка входа консольного приложения: инициализирует миграции и запускает HTTP-сервер.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            // HTTP-настройки
            var settings = new Settings("localhost", "1337");

            string connectionString =
                "Host=localhost;Port=5432;Database=Migration;Username=postgres;Password=Olganik496";

            // Сборка, где лежат модели [Table]
            Assembly modelsAssembly = typeof(User).Assembly;

            // Собираем сервис миграций
            var dbAdapter = new PostgresDatabaseAdapter(connectionString);
            var sqlGenerator = new PostgresSqlGenerator();
            var snapshotBuilder = new ModelSnapshotBuilder();
            var migrationGenerator = new MigrationGenerator(sqlGenerator);

            var migrationService = new MigrationService(
                dbAdapter,
                migrationGenerator,
                snapshotBuilder,
                modelsAssembly);

            var server = new HttpServer(settings, migrationService);

            bool keepRunning = true;
            bool keepAlive = false;

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                keepRunning = false;
            };

            server.Start(ref keepAlive);

            Console.WriteLine("Команды: /start, /restart, /off, /stop, /help");

            while (keepRunning)
            {
                var command = Console.ReadLine();

                switch (command)
                {
                    case "/start":
                        if (!keepAlive)
                            server.Start(ref keepAlive);
                        else
                            Console.WriteLine("Сервер уже запущен");
                        break;

                    case "/restart":
                        server.Stop(ref keepAlive);
                        Thread.Sleep(500);
                        server.Start(ref keepAlive);
                        break;

                    case "/off":
                        if (keepAlive)
                            server.Stop(ref keepAlive);
                        else
                            Console.WriteLine("Сервер уже выключен");
                        break;

                    case "/stop":
                        server.Stop(ref keepRunning);
                        keepRunning = false;
                        break;

                    case "/help":
                        Console.WriteLine("Доступные команды:\n" +
                                          "/start   - запустить сервер\n" +
                                          "/restart - перезапустить сервер\n" +
                                          "/off     - выключить сервер\n" +
                                          "/stop    - остановить сервер и выйти");
                        break;

                    default:
                        Console.WriteLine($"\"{command}\" is unknown command");
                        break;
                }
            }
        }
    }
}
