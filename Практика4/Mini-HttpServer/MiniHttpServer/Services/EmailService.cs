using MiniHttpServer.Settings.EmailSettings;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MiniHttpServer.Services
{
    internal class EmailService
    {
        private readonly List<SmtpSettings> _smtpList;

        public EmailService()
        {
            _smtpList = new List<SmtpSettings>
            {
                new SmtpSettings
                {
                    Name = "Gmail",
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Username = "stalinstudentkpfu@gmail.com",
                    Password = "smyk cnxh tcwb kbqk"
                },
                new SmtpSettings
                {
                    Name = "Mail.ru",
                    Host = "smtp.mail.ru",
                    Port = 587,
                    EnableSsl = true,
                    Username = "vsempizda2022@mail.ru",
                    Password = "rxXVPyPO4D4K69xdZpZp"
                }
            };
        }

        public async Task SendEmailAsync(string to, string title, string password)
        {
            foreach (var smtpSettings in _smtpList)
            {
                MailAddress from = new MailAddress(smtpSettings.Username, "Лопатин Никита Алексеевич 11-409");
                MailAddress recepient = new MailAddress(to);

                MailMessage m = new MailMessage(from, recepient);
                m.Subject = title;
                m.Body = $@"
                            <html>
                                <body style='font-family: Arial, sans-serif; color: #333;'>
                                    <h2 style='color:#2e6c80;'>Здравствуйте!</h2>
                                    <p>Вы успешно авторизовались на сайте.</p>
                                    <p>Ваши данные для входа:</p>
                                    <ul>
                                        <li><b>Логин:</b> {to.ToString()}</li>
                                        <li><b>Пароль:</b> {password.ToString()}</li>
                                    </ul>
                                </body>
                            </html>";
                m.IsBodyHtml = true;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "HomeWork_4.zip");
                m.Attachments.Add(new Attachment(path));

                SmtpClient smtp = new SmtpClient(smtpSettings.Host, smtpSettings.Port);
                smtp.Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password);
                smtp.EnableSsl = smtpSettings.EnableSsl;

                await smtp.SendMailAsync(m);
                Console.WriteLine($"Письмо отправлено через {smtpSettings.Name}");
                return;
            }
        }
    }
}
