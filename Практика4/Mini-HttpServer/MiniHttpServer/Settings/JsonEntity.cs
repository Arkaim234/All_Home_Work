using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Settings
{
    public class JsonEntity
    {
        public string SearcherPath { get; set; }
        public string ChatGPTPath { get; set; }

        public string LoginUri {  get; set; }
        public string OlaraUri { get; set; }
        public string SearcherUri { get; set; }
        public string ChatGPTUri { get; set; }
        public string Domain { get; set; }
        public string Port { get; set; }

        public JsonEntity(string loginUri,string olaraUri, string searcherPath, string chatGPTPath, string searcherUri,string chatGPTUri, string domain, string port)
        {
            LoginUri = loginUri;
            OlaraUri = olaraUri;
            SearcherPath = searcherPath;
            ChatGPTPath = chatGPTPath;
            SearcherUri = searcherUri;
            ChatGPTUri = chatGPTUri;
            Domain = domain;
            Port = port;
        }
        public JsonEntity() { }
    }
}
