using MiniHttpServer.Frimework.Core.Atributes;
using MiniHttpServer.Services;
using Aspose.Email.Clients.Activity;
using MiniHttpServer.Frimework.Core;
using MiniHttpServer.Frimework.Core.HttpResponse;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class AuthEndpoint : EndpointBase
    {
        private readonly EmailService emailService = new EmailService();

        // Get /auth/
        [HttpGet("login")]
        public IActionResult LoginPage()
        {
            var model = new { Title = "Авторизация" };
            return new PageResult("Template/Page/login.thtml", model);
        }

        // Get /auth/json
        [HttpGet("json")]
        public IActionResult GetJson()
        {
            var user = new { Username = "Борис", Age = 23 };
            return new JsonResult(user);

            // ответ  '{"username":"Борис","Age":23}'
        }

        // Post /auth/
        [HttpPost("auth")]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Отправка на почту email указанного email и password
            await emailService.SendEmailAsync(email, "Авторизация прошла успешно", password);
            return new JsonResult(new { message = "Авторизация прошла успешно" });
        }


        // Post /auth/sendEmail
        [HttpPost("sendEmail")]
        public void SendEmail(string to, string title, string message)
        {
            // Отправка на почту email указанного email и password


        }

    }
}
