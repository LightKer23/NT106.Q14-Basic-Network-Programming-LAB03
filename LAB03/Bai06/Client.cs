using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Bai06.TCPSocket;

namespace Bai06
{
    public partial class Client : Form
    {
        private TCPClient _client;
        private string _userName;

        public Client()
        {
            InitializeComponent();
            btnOutRoom.Enabled = false;
            btnGui.Enabled = false;
            txtMessage.Enabled = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                btnConnect.Enabled = false;
                btnOutRoom.Enabled = true;
                btnGui.Enabled = true;
                txtMessage.Enabled = true;

                if (string.IsNullOrEmpty(txtHost.Text.Trim()))
                {
                    btnConnect.Enabled = true;
                    MessageBox.Show("Vui lòng nhập địa chỉ Server IP.");
                    return;
                }

                if (string.IsNullOrEmpty(txtPort.Text.Trim()))
                {
                    btnConnect.Enabled = true;
                    MessageBox.Show("Vui lòng nhập địa chỉ Port.");
                    return;
                }
                if (string.IsNullOrEmpty(textName.Text.Trim()))
                {
                    btnConnect.Enabled = true;
                    MessageBox.Show("Vui lòng nhập tên người dùng.");
                    return;
                }

                string host = txtHost.Text;
                int port = int.Parse(txtPort.Text);
                _userName = textName.Text.Trim();

                _client = new TCPClient();

                _client.OnJoin += name =>
                {
                    this.Invoke(new Action(() =>
                    {
                        lstTroChuyen.Items.Add($"{name} đã tham gia phòng");
                    }));
                };

                _client.OnLeave += name =>
                {
                    this.Invoke(new Action(() =>
                    {
                        if (name == _userName) return;
                        lstTroChuyen.Items.Add($"{name} đã rời phòng");
                    }));
                };

                _client.OnMsg += (user, text) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        if (user == _userName) return;
                        lstTroChuyen.Items.Add($"{user}: {text}");
                    }));
                };

                _client.OnPrivateNotice += (from, text) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        var result = MessageBox.Show($"{from} {text}", "Chat riêng", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                            _client.SendPrivateAccept(_userName, from);
                    }));
                };

                _client.OnPrivateReady += (roomId, u1, u2) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        string other = (_userName == u1) ? u2 : u1;
                        var chatForm = new ClientAndClient(_client, _userName, other, roomId);
                        chatForm.Show();
                    }));
                };

                _client.OnSys += msg =>
                {
                    this.Invoke(new Action(() =>
                    {
                        lstTroChuyen.Items.Add(msg);
                    }));
                };

                _client.OnUserList += names =>
                {
                    this.Invoke(new Action(() =>
                    {
                        lstNguoiThamGia.Items.Clear();
                        foreach (var n in names)
                        {
                            if (!string.IsNullOrWhiteSpace(n))
                                lstNguoiThamGia.Items.Add(n);
                        }
                    }));
                };

                bool ok = _client.Connect(host, port, _userName);
                if (ok)
                {
                    lstTroChuyen.Items.Add($"Đã kết nối đến Server {host}:{port}");
                }
                else
                {
                    lstTroChuyen.Items.Add($"Kết nối thất bại đến Server {host}:{port}");
                    btnConnect.Enabled = true;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _client?.Disconnect();
            lstTroChuyen.Items.Clear();
            lstNguoiThamGia.Items.Clear();
            btnConnect.Enabled = true;
            btnGui.Enabled = false;
            btnOutRoom.Enabled = false;
            txtMessage.Enabled = false;


        }


        private void btnGui_Click(object sender, EventArgs e)
        {
            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            if (_client.IsDisconnected() == true)
            {
                MessageBox.Show("Lỗi kết nối Server!", "Lỗi");
                txtMessage.Clear();
                return;
            }


            if (_client.SendChat(_userName, msg))
            {
                lstTroChuyen.Items.Add($"Bạn: {msg}");
            }

            txtMessage.Clear();
        }

        private void lstNguoiThamGia_DoubleClick(object sender, EventArgs e)
        {

            if (lstNguoiThamGia.SelectedItem == null) return;

            string target = lstNguoiThamGia.SelectedItem.ToString();
            if (target == _userName) return;

            _client.Send($"[PRIVATE_INVITE]|{_userName}|{target}");

        }


        private void txtMessage_TextChanged(object sender, EventArgs e)
        {
            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg))
                btnGui.Enabled = false;
            else
                btnGui.Enabled = true;
        }

        private void txtMessage_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtMessage.Text.Trim()))
                btnGui.Enabled = false;
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            _client?.Disconnect();
        }
    }
}
