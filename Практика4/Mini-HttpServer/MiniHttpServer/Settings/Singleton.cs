using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Text.Json;

namespace MiniHttpServer.Settings
{
    internal class Singleton
    {
        private static Singleton instance;
        public JsonEntity Settings { get; private set; }
        private Singleton()
        {
            var json = File.ReadAllText("Settings/settings.json");
            Settings = JsonSerializer.Deserialize<JsonEntity>(json);
        }

        public static Singleton GetInstance()
        {
            if (instance == null)
            {
                instance = new Singleton();
            }
            return instance;
        }

    }
}
