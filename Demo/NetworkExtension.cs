using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public static class HttpExtension
    {
        private static ConcurrentDictionary<int, CancellationTokenSource> task_tokens = new ConcurrentDictionary<int, CancellationTokenSource>();
        private static ConcurrentDictionary<int, Delegate> received_actions = new ConcurrentDictionary<int, Delegate>();

        public static void Start(this HttpListener listener, int port)
        {
            //if(listener.IsListening)
            listener.Prefixes.Add($"http://+:{port}/");
            listener.Start();
        }

        public static void WhenReceived(this HttpListener listener, Action<HttpListenerContext> action)
        {
            if (action == null)
                throw new ArgumentNullException();

            var key = listener.GetHashCode();
            if (!received_actions.ContainsKey(key))
            {
                received_actions[key] = action;
            }
            else
            {
                var temp = Delegate.Combine(received_actions[key], action);
                received_actions[key] = temp;
            }

            if (task_tokens.ContainsKey(key))
                return;
            var context = SynchronizationContext.Current;
            var cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (true)
                {
                    if (cts.IsCancellationRequested)
                        break;
                    var httpContext = await listener.GetContextAsync();
                    if (received_actions.ContainsKey(key))
                    {
                        context.Post(_ =>
                        {
                            received_actions[key].DynamicInvoke(httpContext);
                        }, null);
                    }
                }
            }, cts.Token).ConfigureAwait(false);
            task_tokens[key] = cts;
        }

        public static void RemoveAction(this HttpListener listener, Delegate action)
        {
            var key = listener.GetHashCode();
            if (received_actions.ContainsKey(key))
            {
                var temp = Delegate.RemoveAll(received_actions[key], action);
                received_actions[key] = temp;
            }
        }

        public static void ClearActions(this HttpListener listener)
        {
            var key = listener.GetHashCode();

            if (received_actions.TryRemove(key, out Delegate r))
                r = null;

            if (task_tokens.TryRemove(key, out CancellationTokenSource t))
            {
                t.Cancel();
                t.Dispose();
            }
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

        public static async Task CloseAsync(this WebSocket client, WebSocketCloseStatus state)
        {
            await client.CloseAsync(state, "", CancellationToken.None);
        }

        public static async Task SendAsync(this WebSocket client, string msg, Encoding encoding = null)
        {
            if (client == null)
                throw new ArgumentNullException();
            if (encoding == null)
                encoding = Encoding.UTF8;
            var data = encoding.GetBytes(msg);
            await client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task SendAsync(this WebSocket client, byte[] data, int index = 0, int length = -1)
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

        public static async Task<byte[]> ReceiveAsync(this WebSocket socket)
        {
            var data = new byte[65535];
            var r = await socket.ReceiveAsync(new ArraySegment<byte>(data), CancellationToken.None);
            if (r.CloseStatus.HasValue)
                throw new WebException("", WebExceptionStatus.ConnectionClosed);
            if (r.Count > 0)
            {
                var temp = new byte[r.Count];
                Array.Copy(data, temp, r.Count);
                Array.Clear(data, 0, data.Length);
                return temp;
            }
            else
            {
                throw new Exception("未知错误");
            }
        }

        private static ConcurrentDictionary<int, CancellationTokenSource> task_tokens = new ConcurrentDictionary<int, CancellationTokenSource>();
        private static ConcurrentDictionary<int, Delegate> received_actions = new ConcurrentDictionary<int, Delegate>();
        private static ConcurrentDictionary<int, Delegate> closed_actions = new ConcurrentDictionary<int, Delegate>();

        private static void Run(WebSocket socket)
        {
            var key = socket.GetHashCode();

            if (task_tokens.ContainsKey(key))
                return;

            var cts = new CancellationTokenSource();
            var context = SynchronizationContext.Current;
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (cts.IsCancellationRequested)
                        {
                            //if (closed_actions.ContainsKey(key))
                            //{
                            //    var action = closed_actions[key];
                            //    context.Post(_ =>
                            //    {
                            //        action.DynamicInvoke(WebSocketCloseStatus.InternalServerError);
                            //    }, null);
                            //}
                            break;
                        }

                        var data = await socket.ReceiveAsync();

                        if (received_actions.ContainsKey(key))
                        {
                            var action = received_actions[key];
                            context.Post(_ =>
                            {
                                action.DynamicInvoke(data);
                            }, null);
                        }

                        if (socket.CloseStatus.HasValue && closed_actions.ContainsKey(key))
                        {
                            var action = closed_actions[key];
                            context.Post(_ =>
                            {
                                action.DynamicInvoke(socket.CloseStatus);
                            }, null);
                            ClearActions(socket);
                            break;
                        }
                    }
                    catch
                    {
                        if (closed_actions.ContainsKey(key))
                        {
                            var state = socket.CloseStatus.HasValue ? socket.CloseStatus.Value : WebSocketCloseStatus.Empty;
                            var action = closed_actions[key];
                            context.Post(_ =>
                            {
                                action.DynamicInvoke(state);
                            }, null);
                        }
                        ClearActions(socket);
                        break;
                    }
                }
            }, cts.Token).ConfigureAwait(false);
            task_tokens[key] = cts;
        }

        public static void When(this WebSocket socket, Action<byte[]> received, Action<WebSocketCloseStatus> closed)
        {
            var key = socket.GetHashCode();
            if (received != null)
            {
                if (received_actions.ContainsKey(key))
                {
                    var temp = Delegate.Combine(received_actions[key], received);
                    received_actions[key] = temp;
                }
                else
                {
                    received_actions[key] = received;
                }
            }
            if (closed != null)
            {
                if (closed_actions.ContainsKey(key))
                {
                    var temp = Delegate.Combine(closed_actions[key], closed);
                    closed_actions[key] = temp;
                }
                else
                {
                    closed_actions[key] = closed;
                }
            }
            Run(socket);
        }

        public static void WhenReceived(this WebSocket socket, Action<byte[]> action)
        {
            if (action == null)
                throw new ArgumentNullException();
            When(socket, action, null);
        }

        public static void WhenClosed(this WebSocket socket, Action<WebSocketCloseStatus> action)
        {
            if (action == null)
                throw new ArgumentNullException();
            When(socket, null, action);
        }

        public static void RemoveAction(this WebSocket socket, Delegate action)
        {
            try
            {
                var key = socket.GetHashCode();
                if (action is Action<byte[]>)
                {
                    if (received_actions.ContainsKey(key))
                    {
                        var temp = Delegate.RemoveAll(received_actions[key], action);
                        received_actions[key] = temp;
                    }
                }
                else
                {
                    if (closed_actions.ContainsKey(key))
                    {
                        var temp = Delegate.RemoveAll(closed_actions[key], action);
                        closed_actions[key] = temp;
                    }
                }
            }
            catch { }

        }

        public static void ClearActions(this WebSocket socket)
        {
            try
            {
                var key = socket.GetHashCode();
                if (received_actions.TryRemove(key, out Delegate a))
                {
                    a = null;
                }
                if (closed_actions.TryRemove(key, out a))
                {
                    a = null;
                }

                if (task_tokens.TryRemove(key, out CancellationTokenSource t))
                {
                    t.Cancel();
                    t.Dispose();
                }
            }
            catch { }
        }
    }

    public class WebSocketResult
    {
        public WebSocket WebSocket { get; private set; }

        public IPEndPoint RemoteEndPoint { get; private set; }

        public WebSocketResult(WebSocket ws, IPEndPoint p)
        {
            this.WebSocket = ws;
            this.RemoteEndPoint = p;
        }
    }

    public class WebSocketServer
    {
        HttpListener listener;

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
            listener.Close();
        }

        public async Task<WebSocketResult> AcceptWebSocketAsync()
        {
            var context = await listener.GetContextAsync();
            var wsContext = await context.AcceptWebSocketAsync(null);
            return new WebSocketResult(wsContext.WebSocket, context.Request.RemoteEndPoint);
        }

    }
}

namespace System.Net.Sockets
{
    public static class SocketExtension
    {
        private static ConcurrentDictionary<int, CancellationTokenSource> task_tokens = new ConcurrentDictionary<int, CancellationTokenSource>();

        #region UdpClient

        private static ConcurrentDictionary<int, Delegate> udp_received_actions = new ConcurrentDictionary<int, Delegate>();

        /// <summary>
        /// 异步发送字符串
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="encoding">默认UTF-8</param>
        /// <returns></returns>
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

        public static void WhenReceived(this UdpClient client, Action<UdpReceiveResult> action)
        {
            if (action == null)
                throw new ArgumentNullException();
            var key = client.GetHashCode();
            if (udp_received_actions.ContainsKey(key))
            {
                var temp = Delegate.Combine(udp_received_actions[key], action);
                udp_received_actions[key] = temp;
            }
            else
            {
                udp_received_actions[key] = action;
            }

            if (task_tokens.ContainsKey(key))
                return;
            var cts = new CancellationTokenSource();
            var context = SynchronizationContext.Current;
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (cts.IsCancellationRequested)
                            break;
                        var r = await client.ReceiveAsync();
                        if (udp_received_actions.ContainsKey(key))
                        {
                            var a = udp_received_actions[key];
                            if (context == null)
                                a.DynamicInvoke(r);
                            else
                                context.Post(_ =>
                                {
                                    a.DynamicInvoke(r);
                                }, null);
                        }
                    }
                    catch
                    {
                        if (task_tokens.TryRemove(key, out cts))
                        {
                            cts.Dispose();
                        }
                        break;
                    }
                }
            }, cts.Token).ConfigureAwait(false);
            task_tokens[key] = cts;
        }

        public static void RemoveAction(this UdpClient client, Delegate action)
        {
            try
            {
                var key = client.GetHashCode();

                if (udp_received_actions.ContainsKey(key))
                {
                    var temp = Delegate.RemoveAll(udp_received_actions[key], action);
                    udp_received_actions[key] = temp;
                }
            }
            catch { }
        }

        public static void ClearActions(this UdpClient client)
        {
            try
            {
                var key = client.GetHashCode();
                if (udp_received_actions.TryRemove(key, out Delegate a))
                {
                    a = null;
                }

                if (task_tokens.TryRemove(key, out CancellationTokenSource t))
                {
                    t.Cancel();
                    t.Dispose();
                }
            }
            catch { }
        }

        #endregion

        #region TcpClient

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
            if (length < 0)
                length = data.Length - index;
            var stream = client.GetStream();
            stream.Write(data, index, length);
        }

        public static async Task SendAsync(this TcpClient client, byte[] data, int index = 0, int length = -1)
        {
            var stream = client.GetStream();
            if (index < 0)
                index = 0;
            if (length < 0)
                length = data.Length - index;
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

        private static ConcurrentDictionary<int, Delegate> tcp_received_actions = new ConcurrentDictionary<int, Delegate>();
        private static ConcurrentDictionary<int, Delegate> tcp_closed_actions = new ConcurrentDictionary<int, Delegate>();
        private static ConcurrentDictionary<int, Delegate> tcp_connected_actions = new ConcurrentDictionary<int, Delegate>();

        public static void When(this TcpClient client, Action<byte[]> received, Action<IPEndPoint> closed)
        {
            var key = client.GetHashCode();

            if (received != null)
            {
                if (tcp_received_actions.ContainsKey(key))
                {
                    var temp = Delegate.Combine(tcp_received_actions[key], received);
                    tcp_received_actions[key] = temp;
                }
                else
                {
                    tcp_received_actions[key] = received;
                }
            }

            if (closed != null)
            {
                if (tcp_closed_actions.ContainsKey(key))
                {
                    var temp = Delegate.Combine(tcp_closed_actions[key], closed);
                    tcp_closed_actions[key] = temp;
                }
                else
                {
                    tcp_closed_actions[key] = closed;
                }
            }

            if (task_tokens.ContainsKey(key))
                return;

            var cts = new CancellationTokenSource();
            var context = SynchronizationContext.Current;

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var stream = client.GetStream();
                        if (cts.IsCancellationRequested)
                        {
                            break;
                        }
                        var data = new byte[client.ReceiveBufferSize];
                        var length = await stream.ReadAsync(data, 0, data.Length);
                        if (length > 0 && tcp_received_actions.ContainsKey(key))
                        {
                            var temp = new byte[length];
                            Array.Copy(data, temp, length);
                            Array.Clear(data, 0, data.Length);
                            var a = tcp_received_actions[key];
                            context.Post(_ =>
                            {
                                try { a.DynamicInvoke(temp); }
                                catch { }
                            }, null);
                        }

                        if (tcp_closed_actions.ContainsKey(key))
                        {
                            var socket = client.Client;
                            if (socket.Poll(100, SelectMode.SelectRead) && socket.Available <= 0)
                            {
                                if (tcp_closed_actions.ContainsKey(key))
                                {
                                    var a = tcp_closed_actions[key];
                                    context.Post(_ =>
                                    {
                                        a.DynamicInvoke(socket.RemoteEndPoint);
                                    }, null);
                                }
                                ClearActions(client);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        //if (tcp_closed_actions.ContainsKey(key))
                        //{
                        //    IPEndPoint p = null;
                        //    try
                        //    {
                        //        p = (IPEndPoint)client.Client.RemoteEndPoint;
                        //    }
                        //    catch { }
                        //    var a = tcp_closed_actions[key];
                        //    context.Post(_ =>
                        //                                {
                        //                                    a.DynamicInvoke(p);
                        //                                }, null);
                        //}
                        ClearActions(client);
                        break;
                    }
                }
            }, cts.Token).ConfigureAwait(false);
            task_tokens[key] = cts;
        }

        public static void WhenReceived(this TcpClient client, Action<byte[]> action)
        {
            if (action == null)
                throw new ArgumentNullException();
            if (!client.Connected)
                throw new SocketException((int)SocketError.NotConnected);

            When(client, action, null);
        }

        /// <summary>
        /// 当连接关闭时
        /// 调用TcpClient.Close()方法不会触发
        /// </summary>
        /// <param name="client"></param>
        /// <param name="closed"></param>
        public static void WhenClosed(this TcpClient client, Action<IPEndPoint> closed)
        {
            if (closed == null)
                throw new ArgumentNullException();
            if (!client.Connected)
                throw new SocketException((int)SocketError.NotConnected);

            When(client, null, closed);
        }

        public static void RemoveAction(this TcpClient client, Delegate action)
        {
            try
            {
                var key = client.GetHashCode();

                if (action is Action<byte[]>)
                {
                    if (tcp_received_actions.ContainsKey(key))
                    {
                        var temp = Delegate.RemoveAll(tcp_received_actions[key], action);
                        tcp_received_actions[key] = temp;
                    }
                }
                else
                {
                    if (tcp_closed_actions.ContainsKey(key))
                    {
                        var temp = Delegate.RemoveAll(tcp_closed_actions[key], action);
                        tcp_closed_actions[key] = temp;
                    }
                }

            }
            catch { }
        }

        public static void ClearActions(this TcpClient client)
        {
            try
            {
                var key = client.GetHashCode();
                Delegate a;
                if (tcp_received_actions.TryRemove(key, out a))
                {
                    a = null;
                }
                if (tcp_closed_actions.TryRemove(key, out a))
                {
                    a = null;
                }
                CancellationTokenSource t;
                if (task_tokens.TryRemove(key, out t))
                {
                    t.Cancel();
                    t.Dispose();
                }
            }
            catch { }
        }

        #endregion

        #region TcpListener

        public static void WhenConnected(this TcpListener listener, Action<TcpClient> action)
        {
            if (action == null)
                throw new ArgumentNullException();
            var key = listener.GetHashCode();
            if (tcp_connected_actions.ContainsKey(key))
            {
                var temp = Delegate.Combine(tcp_connected_actions[key], action);
                tcp_connected_actions[key] = temp;
            }
            else
            {
                tcp_connected_actions[key] = action;
            }
            if (task_tokens.ContainsKey(key))
                return;
            var cts = new CancellationTokenSource();
            var context = SynchronizationContext.Current;
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (cts.IsCancellationRequested)
                            break;
                        var client = await listener.AcceptTcpClientAsync();
                        if (tcp_connected_actions.ContainsKey(key))
                        {
                            var a = tcp_connected_actions[key];
                            context.Post(_ =>
                            {
                                a.DynamicInvoke(client);
                            }, null);
                        }
                    }
                    catch { break; }
                }
            }, cts.Token).ConfigureAwait(false);
            task_tokens[key] = cts;
        }

        public static void RemoveAction(this TcpListener listener, Delegate action)
        {
            try
            {
                var key = listener.GetHashCode();

                if (tcp_connected_actions.ContainsKey(key))
                {
                    var temp = Delegate.RemoveAll(tcp_connected_actions[key], action);
                    tcp_connected_actions[key] = temp;
                }
            }
            catch { }
        }

        public static void ClearActions(this TcpListener listener)
        {
            try
            {
                var key = listener.GetHashCode();
                Delegate a;
                if (tcp_connected_actions.TryRemove(key, out a))
                {
                    a = null;
                }
                CancellationTokenSource t;
                if (task_tokens.TryRemove(key, out t))
                {
                    t.Cancel();
                    t.Dispose();
                }
            }
            catch { }
        }

        #endregion
    }
}
