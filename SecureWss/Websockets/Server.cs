using C5Debugger;

using System;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace SecureWss.Websockets
{
    internal class Server
    {
        private WebSocketServer _wsServer;
        public bool IsRunning { get => _wsServer?.IsListening ?? false; }

        public void Start(int port, string certPath = "", string certPassword = "")
        {
            try
            {
                Debug.Print(DebugLevel.WebSocket, "Creating Server");
                _wsServer = new WebSocketServer(IPAddress.Any, port, !string.IsNullOrWhiteSpace(certPath));
                if (!string.IsNullOrWhiteSpace(certPath)) 
                {
                    Debug.Print(DebugLevel.WebSocket, "Assigning SSL Configuration");
                    _wsServer.SslConfiguration = new ServerSslConfiguration(new X509Certificate2(certPath, certPassword))
                    {
                        ClientCertificateRequired = false,
                        CheckCertificateRevocation = false,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11,
                        //this is just to test, you might want to actually validate
                        ClientCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                            Debug.Print(DebugLevel.WebSocket, "ClientCerticateValidation Callback triggered");
                            return true;
                        }
                    };
                }
                Debug.Print(DebugLevel.WebSocket, "Adding Echo Service");
                _wsServer.AddWebSocketService<EchoService>("/echo");
                Debug.Print(DebugLevel.WebSocket, "Assigning Log Info");
                _wsServer.Log.Level = LogLevel.Trace;
                _wsServer.Log.Output = delegate
                {
                    //Debug.Print(DebugLevel.WebSocket, "{1} {0}\rCaller:{2}\rMessage:{3}\rs:{4}", d.Level.ToString(), d.Date.ToString(), d.Caller.ToString(), d.Message, s);
                };
                Debug.Print(DebugLevel.WebSocket, "Starting");
                _wsServer.Start();
            }
            catch (Exception ex)
            {
                Debug.Print(DebugLevel.Error, "WebSocket Failed to start {0}", ex.Message);
            }
        }

        public void Stop()
        {
            if (_wsServer != null)
                _wsServer.Stop();

            _wsServer = null;
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
        protected override void OnError(ErrorEventArgs e)
        {
            Debug.Print(DebugLevel.Error, "WebSocket.OnError message {0}", e.Message);
        }
    }
}
