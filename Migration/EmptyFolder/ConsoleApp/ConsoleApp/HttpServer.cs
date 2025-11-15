using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MigrationLib;

namespace ConsoleApp
{
    /// <summary>
    /// HTTP-сервер на HttpListener, который отдаёт JSON API для управления миграциями.
    /// </summary>
    public class HttpServer
    {
        private readonly Settings _settings;
        private readonly MigrationService _migrationService;

        private HttpListener _listener = new();
        private CancellationTokenSource? _cts;

        public HttpServer(Settings settings, MigrationService migrationService)
        {
            _settings = settings;
            _migrationService = migrationService;
        }

        public void Start(ref bool keepAlive)
        {
            _cts = new CancellationTokenSource();

            _listener = new HttpListener();
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://{_settings.Domain}:{_settings.Port}/");
            _listener.Start();

            keepAlive = true;

            Console.WriteLine($"HTTP-сервер слушает http://{_settings.Domain}:{_settings.Port}/");
            Console.WriteLine("Маршруты:");
            Console.WriteLine("  GET /migrate/create");
            Console.WriteLine("  GET /migrate/apply");
            Console.WriteLine("  GET /migrate/rollback");
            Console.WriteLine("  GET /migrate/status");
            Console.WriteLine("  GET /migrate/log");

            _ = ListenAsync(_cts.Token);
        }

        public void Stop(ref bool flag)
        {
            try
            {
                _cts?.Cancel();

                if (_listener.IsListening)
                    _listener.Stop();

                _listener.Close();
            }
            catch { }

            Console.WriteLine("Сервер остановлен");
            flag = false;
        }

        private async Task ListenAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    await HandleRequestAsync(context, token);
                }
            }
            catch (HttpListenerException)
            {
                // listener остановлен
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken token)
        {
            var request = context.Request;
            var response = context.Response;
            response.ContentType = "application/json; charset=utf-8";

            object result;

            try
            {
                if (request.HttpMethod != "GET")
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    result = new { error = "Only GET is allowed" };
                }
                else
                {
                    switch (request.Url!.AbsolutePath)
                    {
                        case "/migrate/create":
                            {
                                var mig = _migrationService.CreateMigration();
                                result = new { migration = mig.MigrationName, status = "created" };
                                break;
                            }

                        case "/migrate/apply":
                            {
                                var mig = _migrationService.ApplyLastMigration();
                                result = new { migration = mig.MigrationName, status = "applied" };
                                break;
                            }

                        case "/migrate/rollback":
                            {
                                var mig = _migrationService.RollbackLastMigration();
                                result = new { migration = mig.MigrationName, status = "rolled_back" };
                                break;
                            }

                        case "/migrate/status":
                            {
                                var status = _migrationService.GetStatus();
                                result = status;
                                break;
                            }

                        case "/migrate/log":
                            {
                                var log = _migrationService.GetLog()
                                    .Select(m => new
                                    {
                                        m.Id,
                                        m.MigrationName,
                                        m.AppliedAt
                                    }).ToList();
                                result = new { log };
                                break;
                            }

                        default:
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            result = new { error = "Not found" };
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                result = new { error = ex.Message };
            }

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;

            await using var output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length, token);
        }
    }
}
