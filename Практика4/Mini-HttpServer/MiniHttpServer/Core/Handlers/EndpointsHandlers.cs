using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Atributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MiniHttpServer.Endpoints;

namespace MiniHttpServer.Core.Handlers
{
    class EndpointsHandlers : Handler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            if (true)
            {
                var request = context.Request;
                var parts = request.Url?.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var endpointName = parts?.FirstOrDefault();

                var assembly = Assembly.GetExecutingAssembly();
                var endpoint = assembly.GetTypes()
                       .Where(t => t.GetCustomAttribute<EndpointAttribute>() != null)
                       .FirstOrDefault(end => IsCheckedEndpoint(end.Name, endpointName));

                if (endpoint == null) return; //TODO
                var method = endpoint.GetMethods().Where(t => t.GetCustomAttributes(true)
                    .Any(attr => attr.GetType().Name.Equals($"Http{context.Request.HttpMethod}", StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault();

                if (method == null) return; //TODO
                var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();

                var postParams = new Dictionary<string, string>();
                foreach (var pair in body.Split('&'))
                {
                    var kv = pair.Split('=');
                    postParams[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
                }

                var parameters = method.GetParameters()
                                         .Select(p => postParams.ContainsKey(p.Name) ? postParams[p.Name] : null)
                                         .ToArray();

                var ret = method.Invoke(Activator.CreateInstance(endpoint), parameters);
            }
            // передача запроса дальше по цепи при наличии в ней обработчиков
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }

        private bool IsCheckedEndpoint(string className, string endpointName) =>
         className.Equals(endpointName, StringComparison.OrdinalIgnoreCase)
        || className.Equals($"{endpointName}Endpoint", StringComparison.OrdinalIgnoreCase);
    }
}
