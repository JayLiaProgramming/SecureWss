using C5Debugger;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace SecureWss.Websockets
{
    internal class Server
    {
        private HttpServer _httpsServer;
        private static Dictionary<string, string> _contentTypes = new Dictionary<string, string>
        {
            { "htm", "text/html" },
            { "html", "text/html" },
            { "js", "application/javascript" },
            { "json", "application/json" },
            { "css", "text/css" },
            { "webp", "image/webp" },
            { "png", "image/png" },
            { "jsonld", "application/ld+json" },
            { "mid", "audio/midi" },
            { "midi", "audio/x-midi" },
            { "mjs", "text/javascript" },
            { "mp3", "audio/mpeg" },
            { "mp4", "video/mp4" },
            { "mpeg", "video/mpeg" },
            { "mpkg", "application/vnd.apple.installer+xml" },
            { "odp", "application/vnd.oasis.opendocument.presentation" },
            { "ods", "application/vnd.oasis.opendocument.spreadsheet" },
            { "odt", "application/vnd.oasis.opendocument.text" },
            { "oga", "audio/ogg" },
            { "ogv", "video/ogg" },
            { "ogx", "application/ogg" },
            { "opus", "audio/opus" },
            { "otf", "font/otf" },
            { "pdf", "application/pdf" },
            { "php", "application/x-httpd-php" },
            { "ppt", "application/vnd.ms-powerpoint" },
            { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { "rar", "application/vnd.rar" },
            { "rtf", "application/rtf" },
            { "sh", "application/x-sh" },
            { "svg", "image/svg+xml" },
            { "swf", "application/x-shockwave-flash" },
            { "tar", "application/x-tar" },
            { "tif", "image/tiff" },
            { "tiff", "image/tiff" },
            { "ts", "video/mp2t" },
            { "ttf", "font/ttf" },
            { "txt", "text/plain" },
            { "vsd", "application/vnd.visio" },
            { "wav", "audio/wav" },
            { "weba", "audio/webm" },
            { "webm", "video/webm" },
            { "woff", "font/woff" },
            { "woff2", "font/woff2" },
            { "xhtml", "application/xhtml+xml" },
            { "xls", "application/vnd.ms-excel" },
            { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { "xml", "application/xml" },
            { "xul", "application/vnd.mozilla.xul+xml" },
            { "zip", "application/zip" },
            { "7z", "application/x-7z-compressed" },
            { "collection", "font/collection" },
            { "sfnt", "font/sfnt" },
            { "ico", "image/vnd.microsoft.icon" }
        };
        public bool IsRunning { get => _httpsServer?.IsListening ?? false; }//_wsServer?.IsListening ?? false; }

        public void Start(int port, string certPath = "", string certPassword = "", string rootPath = @"\html")
        {
            try
            {
                Debug.Print(DebugLevel.WebSocket, $"Creating Server from directory {rootPath}");
                _httpsServer = new HttpServer(port, true)
                {
                    RootPath = rootPath
                };
                
                Debug.Print($"RootPath = {_httpsServer.RootPath}");
                if (!string.IsNullOrWhiteSpace(certPath)) 
                {
                    Debug.Print(DebugLevel.WebSocket, "Assigning SSL Configuration");
                    _httpsServer.SslConfiguration = new ServerSslConfiguration(new X509Certificate2(certPath, certPassword))
                    {
                        ClientCertificateRequired = false,
                        CheckCertificateRevocation = false,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        //this is just to test, you might want to actually validate
                        ClientCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                        {
                            Debug.Print(DebugLevel.WebSocket, "HTTPS ClientCerticateValidation Callback triggered");
                            return true;
                        }
                    };
                }
                Debug.Print(DebugLevel.WebSocket, "Adding Echo Service");
                _httpsServer.AddWebSocketService<EchoService>("/echo");
                Debug.Print(DebugLevel.WebSocket, "Assigning Log Info");
                _httpsServer.Log.Level = LogLevel.Trace;
                _httpsServer.Log.Output = delegate
                {
                    //Debug.Print(DebugLevel.WebSocket, "{1} {0}\rCaller:{2}\rMessage:{3}\rs:{4}", d.Level.ToString(), d.Date.ToString(), d.Caller.ToString(), d.Message, s);
                };
                Debug.Print(DebugLevel.WebSocket, "Starting");

                _httpsServer.OnGet += (sender, e) =>
                {
                    Debug.Print(DebugLevel.WebSocket, $"OnGet requesting {e.Request}");
                    var req = e.Request;
                    var res = e.Response;

                    var path = req.RawUrl;

                    if (path == "/")
                        path += "index.html";

                    var localPath = Path.Combine(rootPath, path.Substring(1));

                    byte[] contents;
                    if (File.Exists(localPath))
                        contents = File.ReadAllBytes(localPath);
                    else
                    {
                        e.Response.StatusCode = 404;
                        contents = Encoding.UTF8.GetBytes("Path not found " + e.Request.RawUrl);
                    }

                    var extention = Path.GetExtension(path);
                    if (!_contentTypes.TryGetValue(extention, out var contentType))
                        contentType = "text/html";

                    res.ContentLength64 = contents.LongLength;

                    res.Close(contents, true);
                };

                _httpsServer.Start();
                Debug.Print(DebugLevel.WebSocket, "Ready");
            }
            catch (Exception ex)
            {
                Debug.Print(DebugLevel.Error, "WebSocket Failed to start {0}", ex.Message);
            }
        }

        public void Stop()
        {
            _httpsServer?.Stop();

            _httpsServer = null;
        }

    }
    public class EchoService : WebSocketBehavior
    {
        public EchoService()
        {
            try
            {
                Debug.Print(DebugLevel.WebSocket, "Echo Service Created");
            }
            catch (Exception ex)
            {
                Debug.Print(DebugLevel.Error, "Websocket.Constructor error {0}", ex.Message);
            }
        }

        protected override void OnOpen()
        {
            try
            {
                base.OnOpen();
                Debug.Print(DebugLevel.WebSocket, $"New Client Connected: {ID}");
                
            }
            catch (Exception ex)
            {
                Debug.Print(DebugLevel.Error, "Websocket.OnOpen error {0}", ex.Message);
            }
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {                
                var received = e.Data;
                Debug.Print(DebugLevel.WebSocket, $"EchoService Received {received} and echoing back");
                Send(received);
            }
            catch (Exception ex)
            {
                Debug.Print(DebugLevel.Error, "WebSocket.OnMessage error {0}", ex.Message);
            }
        }
        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Debug.Print(DebugLevel.Error, "WebSocket.OnError message {0}", e.Message);
        }
    }
}
