using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Bai06.TCPSocket
{
    public class TCPClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenThread;
        private string _userName;
        public event Action<string[]>? OnUserList;

        public event Action<string>? OnMessageReceived;
        public event Action<string>? OnSys;
        public event Action<string>? OnJoin;
        public event Action<string>? OnLeave;
        public event Action<string, string>? OnMsg;
        public event Action<int, string, string>? OnPrivateReady;
        public event Action<int, string, string>? OnPrivateMsg;
        public event Action<string, string>? OnPrivateNotice;

        public bool Connect(string host, int port, string name)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(host, port);
                _stream = _client.GetStream();

                _userName = name;

                string localIp = ((System.Net.IPEndPoint)_client.Client.LocalEndPoint).Address.MapToIPv4().ToString();
                int localPort = ((System.Net.IPEndPoint)_client.Client.LocalEndPoint).Port;

                Send($"[JOIN]|{name}|{localIp}:{localPort}");

                _listenThread = new Thread(Listen);
                _listenThread.IsBackground = true;
                _listenThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                OnMessageReceived?.Invoke($"[SYS]|Không thể kết nối tới server: {ex.Message}");
                return false;
            }
        }


        private void Listen()
        {
            byte[] buffer = new byte[1024];
            int byteCount;
            try
            {
                while ((byteCount = _stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    ParseMessage(message);
                }
            }
            catch
            {
                OnMessageReceived?.Invoke("Kết nối tới server.");
            }
        }

        public bool Send(string message)
        {
            if (_client == null || !_client.Connected || _stream == null)
            {
                MessageBox.Show("Lỗi kết nối Server!", "Lỗi");
                return false;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
                return true;
            }
            catch
            {
                MessageBox.Show("Lỗi kết nối Server!", "Lỗi");
                return false;
            }
        }

        public bool SendChat(string name, string message)
        {
            return Send($"[MSG]|{name}|{message}");
        }

        public void Disconnect()
        {
            try
            {
                if (_client != null && _client.Connected)
                {
                    string localIp = ((System.Net.IPEndPoint)_client.Client.LocalEndPoint)
                                     .Address.MapToIPv4().ToString();
                    int localPort = ((System.Net.IPEndPoint)_client.Client.LocalEndPoint).Port;

                    Send($"[LEAVE]|{_userName}|{localIp}:{localPort}");
                }
            }
            catch { }

            _client?.Close();
        }

        private void ParseMessage(string msg)
        {
            if (msg.StartsWith("[JOIN]|"))
            {
                var parts = msg.Split('|');
                OnJoin?.Invoke(parts[1]);
            }
            else if (msg.StartsWith("[LEAVE]|"))
            {
                var parts = msg.Split('|');
                OnLeave?.Invoke(parts[1]);
            }
            else if (msg.StartsWith("[MSG]|"))
            {
                string temp = msg.Substring(6);

                int i = temp.IndexOf('|');
                string user = temp.Substring(0, i);
                string text = temp.Substring(i + 1);

                OnMsg?.Invoke(user, text);
            }
            else if (msg.StartsWith("[SYS]|"))
            {
                OnSys?.Invoke(msg.Substring(5));
            }
            else if (msg.StartsWith("[PRIVATE_NOTICE]|"))
            {
                var parts = msg.Split('|');
                string from = parts[1];
                string text = parts[2];
                OnPrivateNotice?.Invoke(from, text);
            }
            else if (msg.StartsWith("[PRIVATE_READY]|"))
            {
                var parts = msg.Split('|');
                int roomId = int.Parse(parts[1]);
                string u1 = parts[2];
                string u2 = parts[3];
                OnPrivateReady?.Invoke(roomId, u1, u2);
            }
            else if (msg.StartsWith("[PRIVATE_MSG]|"))
            {
                var parts = msg.Split('|');
                int roomId = int.Parse(parts[1]);
                string from = parts[2];
                string msgText = parts[3];
                OnPrivateMsg?.Invoke(roomId, from, msgText);
            }
            else if (msg.StartsWith("[FILE]|"))
            {
                var parts = msg.Split('|');
                string user = parts[1];
                string fileName = parts[2];
                string base64 = parts[3];

                string content = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

                OnMsg?.Invoke(user, $"[File nhận từ {user}] {fileName}\n{content}");
            }
            else if (msg.StartsWith("[LIST]|"))
            {
                string[] names = msg.Substring(7).Split(',');
                OnUserList?.Invoke(names);
            }
            else
            {
                OnMessageReceived?.Invoke(msg);
            }
        }

        public void SendPrivateAccept(string from, string to)
        {
            Send($"[PRIVATE_ACCEPT]|{from}|{to}");
        }

        public void SendPrivateMsg(int roomId, string from, string msg)
        {
            Send($"[PRIVATE_MSG]|{roomId}|{from}|{msg}");
        }

        public bool IsDisconnected()
        {
            try
            {
                if (_client == null || !_client.Connected)
                    return true;

                return _client.Client.Poll(0, SelectMode.SelectRead) && _client.Available == 0;
            }
            catch
            {
                return true;
            }
        }

    }
}
