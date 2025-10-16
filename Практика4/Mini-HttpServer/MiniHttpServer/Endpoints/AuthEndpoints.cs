using MiniHttpServer.Core.Atributes;
using MiniHttpServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Endpoints
{
    [Endpoint]
    internal class AuthEndpoint
    {
        private readonly EmailService emailService = new EmailService();

        // Get /auth/
        [HttpGet]
        public string LoginPage()
        {
            return "index.html";
        }

        // Post /auth/
        [HttpPost("auth")]
        public async Task Login(string email, string password)
        {
            // Отправка на почту email указанного email и password
            Console.WriteLine("Члены");
            await emailService.SendEmailAsync(email, "Авторизация прошла успешно", password);
        }


        // Post /auth/sendEmail
        [HttpPost("sendEmail")]
        public void SendEmail(string to, string title, string message)
        {
            // Отправка на почту email указанного email и password


        }

    }
}
