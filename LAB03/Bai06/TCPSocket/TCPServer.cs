using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Bai06.TCPSocket
{
    public class TCPServer
    {
        private TcpListener _listener;
        private List<TcpClient> _clients = new List<TcpClient>();
        private bool _running = false;
        private Dictionary<TcpClient, string> _userNames = new Dictionary<TcpClient, string>();


        public event Action<string>? OnServerLog;
        public event Action<string>? OnSys;
        public event Action<string, string>? OnMsg;
        public event Action<string>? OnJoin;
        public event Action<string>? OnLeave;

        private void Log(string msg) => OnServerLog?.Invoke(msg);

        public void Start(int port)
        {
            if (_running) return;

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start();
            _running = true;

            Thread t = new Thread(ListenLoop);
            t.IsBackground = true;
            t.Start();
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    lock (_clients)
                        _clients.Add(client);
                    var remote = (IPEndPoint)client.Client.RemoteEndPoint;

                    Thread t = new Thread(() => HandleClient(client));
                    t.IsBackground = true;
                    t.Start();
                }
                catch { }
            }
        }

        private void HandleClient(TcpClient client)
        {
            var remote = (IPEndPoint)client.Client.RemoteEndPoint;
            NetworkStream ns = client.GetStream();
            byte[] buffer = new byte[1024];
            int byteCount;

            try
            {
                while ((byteCount = ns.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);

                    if (message.StartsWith("[JOIN]|"))
                    {
                        var parts = message.Split('|');
                        string name = parts[1];

                        lock (_userNames)
                        {
                            if (!_userNames.ContainsKey(client))
                                _userNames[client] = name;
                        }

                        OnJoin?.Invoke($"{name} ({parts[2]})");
                        SendUserListToClient(client);
                        Broadcast(message, client);
                        BroadcastUserList();
                    }
                    else if (message.StartsWith("[LEAVE]|"))
                    {
                        var parts = message.Split('|');
                        string name = parts[1];

                        lock (_userNames)
                        {
                            if (_userNames.ContainsKey(client))
                                _userNames.Remove(client);
                        }

                        OnLeave?.Invoke(name);
                        Broadcast(message, client);
                        BroadcastUserList();
                        break; 
                    }
                    else if (message.StartsWith("[PRIVATE_MSG]|"))
                    {
                        var parts = message.Split('|');
                        int roomId = int.Parse(parts[1]);
                        string from = parts[2];
                        string msgText = parts[3];

                        var room = _privateRooms.FirstOrDefault(r => r.id == roomId);
                        if (room.id == 0) return;

                        string user1 = room.user1;
                        string user2 = room.user2;

                        foreach (var user in new[] { user1, user2 })
                        {
                            var clientTarget = _userNames.FirstOrDefault(x => x.Value == user).Key;
                            if (clientTarget != null)
                            {
                                SendToClient(clientTarget, $"[PRIVATE_MSG]|{roomId}|{from}|{msgText}");
                            }
                        }
                    }
                    else if (message.StartsWith("[PRIVATE_ACCEPT]|"))
                    {
                        var parts = message.Split('|');
                        string from = parts[1];
                        string to = parts[2];

                        int roomId = _roomCounter++;
                        _privateRooms.Add((roomId, from, to));

                        SendPrivateRoomReady(from, to, roomId);
                    }
                    else if (message.StartsWith("[FILE]|"))
                    {
                        var parts = message.Split('|');
                        string user = parts[1];
                        string fileName = parts[2];
                        string base64 = parts[3];
                        string content = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                        OnMsg?.Invoke(user, $"gửi file {fileName}:\n{content}");
                        Broadcast(message, client);
                    }
                    else if (message.StartsWith("[PRIVATE_INVITE]|"))
                    {
                        var parts = message.Split('|');
                        string from = parts[1];
                        string to = parts[2];

                        TcpClient? target = null;
                        lock (_userNames)
                            target = _userNames.FirstOrDefault(x => x.Value == to).Key;

                        if (target != null)
                        {
                            string notice = $"[PRIVATE_NOTICE]|{from}|muốn trò chuyện riêng với bạn";
                            SendToClient(target, notice);
                        }
                    }
                    else if (message.StartsWith("[MSG]|"))
                    {
                        var parts = message.Split('|');
                        OnMsg?.Invoke(parts[1], parts[2]);
                        Broadcast(message, client);
                    }
                    else if (message.StartsWith("[SYS]|"))
                    {
                        OnSys?.Invoke(message.Substring(5));
                    }
                    else
                    {
                        OnServerLog?.Invoke(message);
                    }
                }
            }
            catch
            {
                string name = "";
                lock (_userNames)
                {
                    if (_userNames.ContainsKey(client))
                    {
                        name = _userNames[client];
                        _userNames.Remove(client);
                    }
                }

                if (!string.IsNullOrEmpty(name))
                    OnLeave?.Invoke(name);
            }
            finally
            {
                lock (_clients) _clients.Remove(client);
                client.Close();
            }
        }

        private void Broadcast(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    try { client.GetStream().Write(data, 0, data.Length); } catch { }
                }
            }
        }

        private void SendUserListToClient(TcpClient client)
        {
            string list = string.Join(",", _userNames.Values);
            string message = "[LIST]|" + list;
            byte[] data = Encoding.UTF8.GetBytes(message);

            try { client.GetStream().Write(data, 0, data.Length); } catch { }
        }

        private void BroadcastUserList()
        {
            string list = string.Join(",", _userNames.Values);
            string message = "[LIST]|" + list;
            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (_clients)
            {
                foreach (var c in _clients)
                {
                    try { c.GetStream().Write(data, 0, data.Length); } catch { }
                }
            }
        }

        public void Stop()
        {
            _running = false;

            try { _listener?.Stop(); } catch { }

            lock (_clients)
            {
                foreach (var c in _clients)
                {
                    try { c.GetStream().Close(); } catch { }
                    try { c.Close(); } catch { }
                }
                _clients.Clear();
            }

            lock (_userNames)
            {
                _userNames.Clear();
            }
        }

        private void SendToClient(TcpClient client, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            try { client.GetStream().Write(data, 0, data.Length); } catch { }
        }



        private int _roomCounter = 1;
        private List<(int id, string user1, string user2)> _privateRooms = new List<(int, string, string)>();

        private void SendPrivateRoomReady(string user1, string user2, int roomId)
        {
            string msg = $"[PRIVATE_READY]|{roomId}|{user1}|{user2}";
            byte[] data = Encoding.UTF8.GetBytes(msg);

            var c1 = _userNames.FirstOrDefault(x => x.Value == user1).Key;
            var c2 = _userNames.FirstOrDefault(x => x.Value == user2).Key;

            c1?.GetStream().Write(data, 0, data.Length);
            c2?.GetStream().Write(data, 0, data.Length);
        }
    }
}
