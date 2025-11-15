namespace ConsoleApp
{
    /// <summary>
    /// Простейшие настройки HTTP-сервера (домен и порт, на которых он слушает запросы).
    /// </summary>
    public class Settings
    {
        public string Domain { get; set; } = "localhost";
        public string Port { get; set; } = "1337";

        public Settings() { }
        public Settings(string domain, string port)
        {
            Domain = domain;
            Port = port;
        }
    }
}