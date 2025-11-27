namespace Bai06
{
    partial class Client
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            grpBoxTCPServer = new GroupBox();
            textName = new TextBox();
            label1 = new Label();
            btnConnect = new Button();
            txtPort = new TextBox();
            txtHost = new TextBox();
            lblConnects = new Label();
            lblPort = new Label();
            lblStatus = new Label();
            lblIP = new Label();
            groupBox1 = new GroupBox();
            btnBrowse = new Button();
            btnGui = new Button();
            txtMessage = new TextBox();
            label2 = new Label();
            grpBoxActivityLog = new GroupBox();
            lstTroChuyen = new ListBox();
            lstNguoiThamGia = new ListBox();
            label3 = new Label();
            btnOutRoom = new Button();
            grpBoxTCPServer.SuspendLayout();
            groupBox1.SuspendLayout();
            grpBoxActivityLog.SuspendLayout();
            SuspendLayout();
            // 
            // grpBoxTCPServer
            // 
            grpBoxTCPServer.Controls.Add(textName);
            grpBoxTCPServer.Controls.Add(label1);
            grpBoxTCPServer.Controls.Add(btnConnect);
            grpBoxTCPServer.Controls.Add(txtPort);
            grpBoxTCPServer.Controls.Add(txtHost);
            grpBoxTCPServer.Controls.Add(lblConnects);
            grpBoxTCPServer.Controls.Add(lblPort);
            grpBoxTCPServer.Controls.Add(lblStatus);
            grpBoxTCPServer.Controls.Add(lblIP);
            grpBoxTCPServer.Font = new Font("Tahoma", 10.2F);
            grpBoxTCPServer.Location = new Point(12, 6);
            grpBoxTCPServer.Name = "grpBoxTCPServer";
            grpBoxTCPServer.Size = new Size(765, 60);
            grpBoxTCPServer.TabIndex = 3;
            grpBoxTCPServer.TabStop = false;
            grpBoxTCPServer.Text = "Kết nối";
            // 
            // textName
            // 
            textName.Font = new Font("Tahoma", 10F);
            textName.Location = new Point(508, 20);
            textName.Name = "textName";
            textName.Size = new Size(126, 28);
            textName.TabIndex = 10;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(458, 23);
            label1.Name = "label1";
            label1.Size = new Size(44, 21);
            label1.TabIndex = 9;
            label1.Text = "Tên:";
            // 
            // btnConnect
            // 
            btnConnect.FlatAppearance.BorderColor = Color.White;
            btnConnect.Location = new Point(654, 20);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 30);
            btnConnect.TabIndex = 8;
            btnConnect.Text = "Kết nối";
            btnConnect.TextAlign = ContentAlignment.BottomCenter;
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // txtPort
            // 
            txtPort.Location = new Point(310, 20);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(137, 28);
            txtPort.TabIndex = 6;
            // 
            // txtHost
            // 
            txtHost.Location = new Point(99, 20);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(146, 28);
            txtHost.TabIndex = 4;
            // 
            // lblConnects
            // 
            lblConnects.AutoSize = true;
            lblConnects.Location = new Point(313, 60);
            lblConnects.Name = "lblConnects";
            lblConnects.Size = new Size(0, 21);
            lblConnects.TabIndex = 3;
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(251, 23);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(53, 21);
            lblPort.TabIndex = 2;
            lblPort.Text = "Cổng:";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(13, 60);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 21);
            lblStatus.TabIndex = 1;
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Location = new Point(13, 23);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(83, 21);
            lblIP.TabIndex = 0;
            lblIP.Text = "Server IP:";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnBrowse);
            groupBox1.Controls.Add(btnGui);
            groupBox1.Controls.Add(txtMessage);
            groupBox1.Controls.Add(label2);
            groupBox1.Font = new Font("Tahoma", 10.2F);
            groupBox1.Location = new Point(12, 403);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(590, 70);
            groupBox1.TabIndex = 4;
            groupBox1.TabStop = false;
            groupBox1.Text = "Tin Nhắn";
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(455, 26);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(44, 29);
            btnBrowse.TabIndex = 9;
            btnBrowse.Text = "...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // btnGui
            // 
            btnGui.FlatAppearance.BorderColor = Color.White;
            btnGui.Location = new Point(505, 25);
            btnGui.Name = "btnGui";
            btnGui.Size = new Size(72, 30);
            btnGui.TabIndex = 8;
            btnGui.Text = "Gửi";
            btnGui.TextAlign = ContentAlignment.BottomCenter;
            btnGui.UseVisualStyleBackColor = true;
            btnGui.Click += btnGui_Click;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(13, 26);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(434, 28);
            txtMessage.TabIndex = 4;
            txtMessage.Click += txtMessage_Click;
            txtMessage.TextChanged += txtMessage_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(313, 24);
            label2.Name = "label2";
            label2.Size = new Size(0, 21);
            label2.TabIndex = 2;
            // 
            // grpBoxActivityLog
            // 
            grpBoxActivityLog.Controls.Add(lstTroChuyen);
            grpBoxActivityLog.Font = new Font("Tahoma", 10.2F);
            grpBoxActivityLog.Location = new Point(12, 72);
            grpBoxActivityLog.Name = "grpBoxActivityLog";
            grpBoxActivityLog.Size = new Size(590, 325);
            grpBoxActivityLog.TabIndex = 5;
            grpBoxActivityLog.TabStop = false;
            grpBoxActivityLog.Text = "Trò chuyện";
            // 
            // lstTroChuyen
            // 
            lstTroChuyen.FormattingEnabled = true;
            lstTroChuyen.ItemHeight = 21;
            lstTroChuyen.Location = new Point(13, 27);
            lstTroChuyen.Name = "lstTroChuyen";
            lstTroChuyen.Size = new Size(564, 277);
            lstTroChuyen.TabIndex = 0;
            // 
            // lstNguoiThamGia
            // 
            lstNguoiThamGia.Font = new Font("Tahoma", 10.2F);
            lstNguoiThamGia.FormattingEnabled = true;
            lstNguoiThamGia.ItemHeight = 21;
            lstNguoiThamGia.Location = new Point(626, 99);
            lstNguoiThamGia.Name = "lstNguoiThamGia";
            lstNguoiThamGia.Size = new Size(151, 298);
            lstNguoiThamGia.TabIndex = 6;
            lstNguoiThamGia.DoubleClick += lstNguoiThamGia_DoubleClick;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Tahoma", 10.2F);
            label3.Location = new Point(626, 72);
            label3.Name = "label3";
            label3.Size = new Size(129, 21);
            label3.TabIndex = 7;
            label3.Text = "Người Tham Gia";
            // 
            // btnOutRoom
            // 
            btnOutRoom.FlatAppearance.BorderColor = Color.White;
            btnOutRoom.Font = new Font("Tahoma", 10.2F);
            btnOutRoom.Location = new Point(626, 412);
            btnOutRoom.Name = "btnOutRoom";
            btnOutRoom.Size = new Size(151, 36);
            btnOutRoom.TabIndex = 11;
            btnOutRoom.Text = "Rời khỏi phòng";
            btnOutRoom.UseVisualStyleBackColor = true;
            btnOutRoom.Click += button1_Click;
            // 
            // Client
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(797, 484);
            Controls.Add(btnOutRoom);
            Controls.Add(label3);
            Controls.Add(lstNguoiThamGia);
            Controls.Add(grpBoxActivityLog);
            Controls.Add(groupBox1);
            Controls.Add(grpBoxTCPServer);
            Name = "Client";
            Text = "Client";
            FormClosing += Client_FormClosing;
            grpBoxTCPServer.ResumeLayout(false);
            grpBoxTCPServer.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            grpBoxActivityLog.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox grpBoxTCPServer;
        private Button btnConnect;
        private TextBox txtPort;
        private TextBox txtHost;
        private Label lblConnects;
        private Label lblPort;
        private Label lblStatus;
        private Label lblIP;
        private GroupBox groupBox1;
        private Button btnGui;
        private TextBox txtMessage;
        private Label label2;
        private GroupBox grpBoxActivityLog;
        private ListBox lstTroChuyen;
        private TextBox textName;
        private Label label1;
        private ListBox lstNguoiThamGia;
        private Label label3;
        private Button btnOutRoom;
        private Button btnBrowse;
    }
}