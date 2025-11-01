using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Frimework.Server;
using MiniHttpServer.Frimework.Settings;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.UnitTests
{
    [TestClass]
    public class HttpServerTests
    {
        private static HttpServer? _server;
        private static readonly JsonEntity TestConfig = new JsonEntity
        {
            Port = "8090",
            Domain = "localhost"
        };
        private static readonly string BaseUrl = $"http://{TestConfig.Domain}:{TestConfig.Port}/";
        private static CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        [ClassInitialize]
        public static void StartServer(TestContext context)
        {
            // создаём и запускаем сервер
            _server = new HttpServer(TestConfig);
            _server.Start(_cancellationToken.Token);
            Thread.Sleep(500); // даём время подняться
        }

        [ClassCleanup]
        public static void StopServer()
        {
            _cancellationToken.Cancel();
            Thread.Sleep(100);
            _server?.Stop();
        }

        // ---------------------- ТЕСТЫ ----------------------

        [TestMethod("1. Сервер поднимается и отвечает на запросы")]
        public async Task Server_RespondsToPing()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(BaseUrl);
            Assert.AreNotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode, "Сервер не ответил на запрос");
        }

        [TestMethod("2. Проверка HTML-эндпоинта /auth/")]
        public async Task HttpServer_Returns_HtmlPage()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(BaseUrl + "auth/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("text/html; charset=utf-8", response.Content.Headers.ContentType!.ToString());
            Assert.IsTrue(body.Contains("Авторизация", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod("3. Проверка JSON-эндпоинта /auth/json")]
        public async Task HttpServer_Returns_Json()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(BaseUrl + "auth/json");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());

            string json = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(json.Contains("\"name\""), "Ответ не содержит JSON-полей");
        }

        [TestMethod("4. Проверка обработки несуществующего пути")]
        public async Task HttpServer_Returns404()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(BaseUrl + "not-found-page.html");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            string html = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(html.Contains("404", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod("5. Проверка POST-запроса /auth/auth")]
        public async Task HttpServer_Handles_PostRequest()
        {
            using var client = new HttpClient();
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("email","test@example.com"),
                new KeyValuePair<string,string>("password","12345")
            });

            var response = await client.PostAsync(BaseUrl + "auth/auth", form);
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(content.Contains("OK", StringComparison.OrdinalIgnoreCase)
                       || content.Contains("успешно", StringComparison.OrdinalIgnoreCase));
        }
    }
}
