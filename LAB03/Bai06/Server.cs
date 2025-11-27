using Bai06.TCPSocket;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai06
{
    public partial class Server : Form
    {

        private TCPServer _server;
        private int _SoLuong = 0;
        public Server()
        {
            InitializeComponent();
            txtBoxIP.Text = GetLocalWiFiIP();
            txtBoxStatus.Text = "Chưa lắng nghe...";
            txtBoxPort.Text = "9876";
            txtBoxConnects.Text = "0";

            btnCloseConnect.Enabled = false;
        }

        private void btnOpenConnect_Click(object sender, EventArgs e)
        {

            try
            {
                int port = int.Parse(txtBoxPort.Text);
                if (_server == null)
                {
                    _server = new TCPServer();

                    _server.OnJoin += name =>
                    {
                        if (lstBoxDiaryLog.InvokeRequired)
                            lstBoxDiaryLog.Invoke(new Action(() =>
                            {
                                lstBoxDiaryLog.Items.Add($"{name} đã tham gia");
                                txtBoxConnects.Text = (++_SoLuong).ToString();
                            }));
                    };

                    _server.OnLeave += name =>
                    {
                        this.Invoke(new Action(() =>
                        {
                            lstBoxDiaryLog.Items.Add($"{name} đã rời phòng");
                            txtBoxConnects.Text = (--_SoLuong).ToString();
                        }));
                    };


                    _server.OnMsg += (user, text) =>
                    {
                        this.Invoke(new Action(() =>
                        {
                            var lines = text.Split('\n');

                            lstBoxDiaryLog.Items.Add($"{user}: {lines[0]}");

                            for (int i = 1; i < lines.Length; i++)
                            {
                                lstBoxDiaryLog.Items.Add("    " + lines[i].Trim());
                            }
                        }));
                    };


                    _server.OnSys += msg =>
                    {
                        this.Invoke(new Action(() =>
                        {
                            lstBoxDiaryLog.Items.Add(msg);
                        }));
                    };
                }
                _server.Start(port);

                txtBoxStatus.Text = "Đang lắng nghe...";
                lstBoxDiaryLog.Items.Add($"Server đang chạy tại {GetLocalWiFiIP()}:{port}");

                btnOpenConnect.Enabled = false;
                btnCloseConnect.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở kết nối: " + ex.Message);
            }


        }

        private void btnCloseConnect_Click(object sender, EventArgs e)
        {
            try
            {
                _SoLuong = 0;
                _server?.Stop();
                txtBoxStatus.Text = "Đã dừng";
                btnCloseConnect.Enabled = false;
                txtBoxConnects.Text = "" + _SoLuong;
                lstBoxDiaryLog.Items.Add("Server đã dừng.");

                btnOpenConnect.Enabled = true;
            }
            catch (Exception ex)
            {
                lstBoxDiaryLog.Items.Add("Lỗi dừng: " + ex.Message);
            }
        }

        private string GetLocalWiFiIP()
        {
            string[] blacklist = { "virtual", "vmware", "hyper", "npcap", "loopback", "miniport" };

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                string name = ni.Name.ToLower();
                if (blacklist.Any(x => name.Contains(x)))
                    continue;

                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !ip.Address.ToString().StartsWith("169.254"))
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return "127.0.0.1";
        }

        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            _server?.Stop();
        }
    }
}
