using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Shared
{
    public static class ContentType
    {
        public static string GetContentType(string path)
        {
            string extension = Path.GetExtension(path).ToLower();

            if (extension == ".html" || extension == ".htm")
                return "text/html; charset=UTF-8";
            if (extension == ".css")
                return "text/css; charset=UTF-8";
            if (extension == ".js")
                return "application/javascript; charset=UTF-8";
            if (extension == ".json")
                return "application/json; charset=UTF-8";
            if (extension == ".png")
                return "image/png";
            if (extension == ".jpg" || extension == ".jpeg")
                return "image/jpeg";
            if (extension == ".gif")
                return "image/gif";
            if (extension == ".svg")
                return "image/svg+xml";
            if (extension == ".ico")
                return "image/x-icon";
            if (extension == ".txt")
                return "text/plain; charset=UTF-8";
            if (extension == ".webp")
                return "image/webp";
            if (extension == ".php")
                return "text/html; charset=UTF-8";
            if (extension == ".woff")
                return "font/woff";
            if (extension == ".woff2")
                return "font/woff2";
            if (extension == ".ttf")
                return "font/ttf";
            if (extension == ".eot")
                return "application/vnd.ms-fontobject";
            if (extension == ".otf")
                return "font/otf";

            return "text/html; charset=UTF-8";
        }
    }
}
