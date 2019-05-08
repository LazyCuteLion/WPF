using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public static class HttpExtension
    {

        public static void Start(this HttpListener listener, int port)
        {
            listener.Prefixes.Add($"http://+:{port}/");
            listener.Start();
        }

        public static Task<HttpListener> WheneverAcceptContext(this HttpListener listener, Action<HttpListenerContext> action, CancellationToken token)
        {
            if (action == null)
                throw new ArgumentNullException();

            if (!listener.IsListening)
                throw new Exception("HttpListener 未启动！");

            return Task.Run(() =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        var contextTask = listener.GetContextAsync();
                        contextTask.Wait(token);
                        var context = contextTask.Result;
                        try { action(context); } catch { }
                        if (token.IsCancellationRequested)
                            break;
                    }
                    catch
                    {
                        break;
                    }
                }
                return listener;
            }, token);
        }
    }
}

namespace System.Net.WebSockets
{
    public static class WebSocketExtension
    {
        public static async Task ConnectAsync(this ClientWebSocket client, string uri)
        {
            await client.ConnectAsync(new Uri(uri), CancellationToken.None);
        }

        public static async Task CloseAsync<T>(this T client) where T : WebSocket
        {
            await client.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
        }

        public static async Task SendAsync<T>(this T client, string msg, Encoding encoding = null) where T : WebSocket
        {
            if (client == null)
                throw new ArgumentNullException();
            if (encoding == null)
                encoding = Encoding.UTF8;
            var data = encoding.GetBytes(msg);
            await client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task SendAsync<T>(this T client, byte[] data, int index = 0, int length = -1) where T : WebSocket
        {
            if (client == null)
                throw new ArgumentNullException();
            if (index < 0)
                index = 0;
            if (length < 0)
                length = data.Length - index;
            var temp = new byte[length];
            Array.Copy(data, index, temp, 0, length);
            await client.SendAsync(new ArraySegment<byte>(temp), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// 循环接收数据；
        /// ”Close事件“请使用该Task.ContinueWith
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="received"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task<WebSocket> WheneverReceived(this WebSocket socket, Action<WebSocket, byte[]> received, CancellationToken token)
        {
            if (received == null)
                throw new ArgumentNullException();
            return Task.Run(() =>
            {
                var data = new byte[65535];
                while (true)
                {
                    try
                    {
                        var t = socket.ReceiveAsync(new ArraySegment<byte>(data), CancellationToken.None);
                        t.Wait(token);
                        var r = t.Result;
                        if (r.CloseStatus.HasValue)
                            break;
                        if (r.Count > 0)
                        {
                            var temp = new byte[r.Count];
                            Array.Copy(data, temp, r.Count);
                            Array.Clear(data, 0, data.Length);
                            try { received(socket, temp); } catch { }
                        }
                        if (token.IsCancellationRequested)
                            break;
                    }
                    catch
                    {
                        break;
                    }
                }
                Debug.WriteLine("WebSocket[{0}] Received Task is completed", socket);
                return socket;
            }, token);
        }

    }

    public class WebSocketServer
    {
        HttpListener listener;
        CancellationTokenSource tokenSource;

        public bool Started { get { return listener.IsListening; } }

        /// <summary>
        /// 本机地址和80端口
        /// </summary>
        public WebSocketServer() : this("http://*/") { }

        /// <summary>
        /// 指定端口
        /// </summary>
        /// <param name="port"></param>
        public WebSocketServer(int port) : this("http://*:" + port) { }

        public WebSocketServer(string url)
        {
            listener = new HttpListener();
            if (url.StartsWith("ws://"))
                url = "http" + url.Substring(2);
            else if (url.StartsWith("/"))
                url = "http://*" + url;

            if (!url.StartsWith("http://"))
                url = "http://" + url;
            if (!url.EndsWith("/"))
                url += "/";
            listener.Prefixes.Add(url);
        }

        public void Start()
        {
            listener.Start();
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void Close()
        {
            this.Stop();
            listener.Close();
            if (tokenSource != null)
            {
                if (!tokenSource.IsCancellationRequested)
                    tokenSource.Cancel();
                tokenSource.Dispose();
            }
        }

        /// <summary>
        /// 每当客户端连接时
        /// </summary>
        /// <param name="connected"></param>
        /// <returns></returns>
        public Task WheneverWebSocketConnected(Action<WebSocket, IPEndPoint> connected)
        {
            if (connected == null)
                throw new ArgumentNullException();
            if (tokenSource != null)
            {
                if (!tokenSource.IsCancellationRequested)
                    tokenSource.Cancel();
                tokenSource.Dispose();
            }

            tokenSource = new CancellationTokenSource();

            return listener.WheneverAcceptContext(async (context) =>
            {
                if (context.Request.IsWebSocketRequest)
                {
                    var ws = await context.AcceptWebSocketAsync(null);
                    var rep = context.Request.RemoteEndPoint;
                    try { connected(ws.WebSocket, rep); } catch { }
                }
            }, tokenSource.Token);

        }
    }
}

namespace System.Net.Sockets
{
    public static class SocketExtension
    {
        #region UdpClient

        public static async Task<int> SendAsync(this UdpClient client, string msg, string ip, int port, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            var data = encoding.GetBytes(msg);
            return await client.SendAsync(data, data.Length, ip, port);
        }

        public static int Send(this UdpClient client, string msg, string ip, int port, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            var data = encoding.GetBytes(msg);
            return client.Send(data, data.Length, ip, port);
        }

        /// <summary>
        /// 循环接收数据
        /// </summary>
        /// <param name="udp"></param>
        /// <param name="received"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task<UdpClient> WheneverReceived(this UdpClient udp, Action<UdpReceiveResult> received, CancellationToken token)
        {
            return Task.Run(() =>
           {
               var lep = udp.Client?.LocalEndPoint;
               while (true)
               {
                   try
                   {
                       var t = udp.ReceiveAsync();
                       t.Wait(token);
                       try { received(t.Result); } catch { }
                       if (token.IsCancellationRequested)
                           break;
                   }
                   catch
                   {
                       break;
                   }
               }
               Debug.WriteLine("UdpClient[{0}] Receive Task is completed", lep);
               return udp;
           }, token);
        }

        /// <summary>
        /// 循环接收数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="received"></param>
        /// <returns></returns>
        public static Task<UdpClient> WheneverReceived(this UdpClient client, Action<UdpReceiveResult> received)
        {
            return client.WheneverReceived(received, CancellationToken.None);
        }

        #endregion

        #region TcpClient

        public static void Disconnect(this TcpClient client)
        {
            try
            {
                if (client.Connected && client.Client != null)
                    client.Client.Disconnect(true);
            }
            catch { }
        }

        public static void Send(this TcpClient client, string msg, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            var data = encoding.GetBytes(msg);
            client.Send(data);
        }

        public static void Send(this TcpClient client, byte[] data, int index = 0, int length = -1)
        {
            if (index < 0)
                index = 0;
            if (length <= 0)
                length = data.Length - index;
            if (length < index)
                throw new ArgumentOutOfRangeException("index,length", "index 不能大于 length");
            var stream = client.GetStream();
            stream.Write(data, index, length);
        }

        public static async Task SendAsync(this TcpClient client, byte[] data, int index = 0, int length = -1)
        {
            if (index < 0)
                index = 0;
            if (length <= 0)
                length = data.Length - index;
            if (length < index)
                throw new ArgumentOutOfRangeException("index,length", "index 不能大于 length");
            var stream = client.GetStream();
            await stream.WriteAsync(data, index, length);
        }

        public static async Task SendAsync(this TcpClient client, string msg, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            var data = encoding.GetBytes(msg);
            await client.SendAsync(data);
        }

        public static IPEndPoint GetRemoteEndPoint(this TcpClient client)
        {
            return client.Client.RemoteEndPoint as IPEndPoint;
        }

        /// <summary>
        /// 循环接收数据；
        /// ”Close事件“请使用该Task.ContinueWith
        /// </summary>
        /// <param name="client"></param>
        /// <param name="received"></param>
        /// <returns></returns>
        public static Task<TcpClient> WheneverReceived(this TcpClient client, Action<TcpClient, byte[]> received)
        {
            return client.WheneverReceived(received, CancellationToken.None);
        }

        /// <summary>
        /// 循环接收数据；
        /// ”Close事件“请使用该Task.ContinueWith
        /// </summary>
        /// <param name="client"></param>
        /// <param name="received"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task<TcpClient> WheneverReceived(this TcpClient client, Action<TcpClient, byte[]> received, CancellationToken token)
        {
            if (received == null)
                throw new ArgumentNullException();
            if (!client.Connected)
                throw new SocketException((int)SocketError.NotConnected);

            return Task.Run(() =>
            {
                var socket = client.Client;
                while (client.Connected)
                {
                    try
                    {
                        if (socket.Poll(100, SelectMode.SelectRead))
                        {
                            var data = new byte[client.ReceiveBufferSize];
                            var length = socket.Receive(data);
                            if (length > 0)
                            {
                                var temp = new byte[length];
                                Array.Copy(data, temp, length);
                                Array.Clear(data, 0, data.Length);
                                try { received(client, temp); } catch { }
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (socket.Poll(100, SelectMode.SelectError))
                            break;
                        if (token.IsCancellationRequested)
                            break;
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode != SocketError.WouldBlock)
                            break;
                    }
                    catch
                    {
                        break;
                    }
                }
                Debug.WriteLine("TcpClient[{0}] Receive Task is completed", socket.RemoteEndPoint);
                return client;
            }, token);
        }

        #endregion

        #region TcpListener
        /// <summary>
        /// 每当客户端连接时
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="connected"></param>
        /// <returns></returns>
        public static Task<TcpListener> WheneverTcpClientConnected(this TcpListener listener, Action<TcpClient> connected)
        {
            return listener.WheneverTcpClientConnected(connected, CancellationToken.None);
        }

        /// <summary>
        /// 每当客户端连接时
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="connected"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task<TcpListener> WheneverTcpClientConnected(this TcpListener listener, Action<TcpClient> connected, CancellationToken token)
        {
            if (connected == null)
                throw new ArgumentNullException();
            listener.Start();
            return Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var t = listener.AcceptTcpClientAsync();
                        t.Wait(token);
                        try { connected(t.Result); } catch { }
                        if (token.IsCancellationRequested)
                            break;
                    }
                    catch { break; }
                }
                Debug.WriteLine("TcpListener[{0}] AcceptTcpClient Task is completed", listener.LocalEndpoint);
                return listener;
            }, token);
        }
        #endregion
    }
}
