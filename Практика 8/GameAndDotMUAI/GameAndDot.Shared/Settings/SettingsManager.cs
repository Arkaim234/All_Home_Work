using System.Net;
using System.Text.Json;

public class SettingsManager
{
    private static SettingsManager instance;
    private static readonly object _lock = new object();

    public IPAddress HostAddress { get; private set; }
    public int PortNumber { get; private set; }

    private SettingsManager()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "settings.json");
            var json = File.ReadAllText(path);

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            HostAddress = IPAddress.Parse(dict["Host"].GetString());
            PortNumber = dict["Port"].GetInt32();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            HostAddress = IPAddress.Any;
            PortNumber = 8888;
        }
    }

    public static SettingsManager GetInstance()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                if (instance == null)
                    instance = new SettingsManager();
            }
        }
        return instance;
    }
}