namespace whatlanCar;

partial class MainWindow
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        btnFindWindowHandle = new Button();
        txtWindowHandle = new TextBox();
        labelPassThreshold = new Label();
        txtPassThreshold = new TextBox();
        labelDarkThreshold = new Label();
        txtDarkThreshold = new TextBox();
        labelPathForwardThreshold = new Label();
        txtPathForwardThreshold = new TextBox();
        labelPathRotateThreshold = new Label();
        txtPathRotateThreshold = new TextBox();
        labelComPort = new Label();
        txtComPort = new TextBox();
        labelVmwarePort = new Label();
        txtVmwarePort = new TextBox();
        btnStart = new Button();
        btnStop = new Button();
        btnInitControl = new Button();
        btnTestAttack = new Button();
        btnTestBoard = new Button();
        btnTestPath = new Button();
        labelCapture = new Label();
        labelDepth = new Label();
        pictureBoxCapture = new PictureBox();
        pictureBoxDepth = new PictureBox();
        labelYolo = new Label();
        panelYoloHost = new Panel();
        lblCaptureTime = new Label();
        lblInferenceTime = new Label();
        lblPassStatus = new Label();
        lblStatus = new Label();
        lstControlLog = new ListBox();
        ((System.ComponentModel.ISupportInitialize)pictureBoxCapture).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pictureBoxDepth).BeginInit();
        SuspendLayout();
        // 
        // btnFindWindowHandle
        // 
        btnFindWindowHandle.Location = new Point(16, 14);
        btnFindWindowHandle.Name = "btnFindWindowHandle";
        btnFindWindowHandle.Size = new Size(104, 32);
        btnFindWindowHandle.TabIndex = 0;
        btnFindWindowHandle.Text = "获取窗口句柄";
        btnFindWindowHandle.UseVisualStyleBackColor = true;
        btnFindWindowHandle.Click += BtnFindWindowHandle_Click;
        // 
        // txtWindowHandle
        // 
        txtWindowHandle.Location = new Point(126, 18);
        txtWindowHandle.Name = "txtWindowHandle";
        txtWindowHandle.Size = new Size(88, 23);
        txtWindowHandle.TabIndex = 1;
        txtWindowHandle.Text = "329860";
        // 
        // labelPassThreshold
        // 
        labelPassThreshold.AutoSize = true;
        labelPassThreshold.Location = new Point(230, 22);
        labelPassThreshold.Name = "labelPassThreshold";
        labelPassThreshold.Size = new Size(92, 17);
        labelPassThreshold.TabIndex = 2;
        labelPassThreshold.Text = "障碍阈值(%)：";
        // 
        // txtPassThreshold
        // 
        txtPassThreshold.Location = new Point(323, 18);
        txtPassThreshold.Name = "txtPassThreshold";
        txtPassThreshold.Size = new Size(52, 23);
        txtPassThreshold.TabIndex = 3;
        txtPassThreshold.Text = "65";
        // 
        // labelDarkThreshold
        // 
        labelDarkThreshold.AutoSize = true;
        labelDarkThreshold.Location = new Point(390, 22);
        labelDarkThreshold.Name = "labelDarkThreshold";
        labelDarkThreshold.Size = new Size(104, 17);
        labelDarkThreshold.TabIndex = 4;
        labelDarkThreshold.Text = "前方暗区阈值：";
        // 
        // txtDarkThreshold
        // 
        txtDarkThreshold.Location = new Point(495, 18);
        txtDarkThreshold.Name = "txtDarkThreshold";
        txtDarkThreshold.Size = new Size(52, 23);
        txtDarkThreshold.TabIndex = 5;
        txtDarkThreshold.Text = "78";
        // 
        // labelPathForwardThreshold
        // 
        labelPathForwardThreshold.AutoSize = true;
        labelPathForwardThreshold.Location = new Point(563, 22);
        labelPathForwardThreshold.Name = "labelPathForwardThreshold";
        labelPathForwardThreshold.Size = new Size(53, 17);
        labelPathForwardThreshold.TabIndex = 6;
        labelPathForwardThreshold.Text = "直行<：";
        // 
        // txtPathForwardThreshold
        // 
        txtPathForwardThreshold.Location = new Point(618, 18);
        txtPathForwardThreshold.Name = "txtPathForwardThreshold";
        txtPathForwardThreshold.Size = new Size(44, 23);
        txtPathForwardThreshold.TabIndex = 7;
        txtPathForwardThreshold.Text = "45";
        // 
        // labelPathRotateThreshold
        // 
        labelPathRotateThreshold.AutoSize = true;
        labelPathRotateThreshold.Location = new Point(678, 22);
        labelPathRotateThreshold.Name = "labelPathRotateThreshold";
        labelPathRotateThreshold.Size = new Size(53, 17);
        labelPathRotateThreshold.TabIndex = 8;
        labelPathRotateThreshold.Text = "转向>：";
        // 
        // txtPathRotateThreshold
        // 
        txtPathRotateThreshold.Location = new Point(733, 18);
        txtPathRotateThreshold.Name = "txtPathRotateThreshold";
        txtPathRotateThreshold.Size = new Size(44, 23);
        txtPathRotateThreshold.TabIndex = 9;
        txtPathRotateThreshold.Text = "55";
        // 
        // labelComPort
        // 
        labelComPort.AutoSize = true;
        labelComPort.Location = new Point(794, 22);
        labelComPort.Name = "labelComPort";
        labelComPort.Size = new Size(60, 17);
        labelComPort.TabIndex = 10;
        labelComPort.Text = "COM口：";
        // 
        // txtComPort
        // 
        txtComPort.Location = new Point(855, 18);
        txtComPort.Name = "txtComPort";
        txtComPort.Size = new Size(60, 23);
        txtComPort.TabIndex = 11;
        txtComPort.Text = "COM5";
        // 
        // labelVmwarePort
        // 
        labelVmwarePort.AutoSize = true;
        labelVmwarePort.Location = new Point(929, 22);
        labelVmwarePort.Name = "labelVmwarePort";
        labelVmwarePort.Size = new Size(92, 17);
        labelVmwarePort.TabIndex = 12;
        labelVmwarePort.Text = "VMware端口：";
        // 
        // txtVmwarePort
        // 
        txtVmwarePort.Location = new Point(1022, 18);
        txtVmwarePort.Name = "txtVmwarePort";
        txtVmwarePort.Size = new Size(60, 23);
        txtVmwarePort.TabIndex = 13;
        txtVmwarePort.Text = "5901";
        // 
        // btnStart
        // 
        btnStart.Location = new Point(1100, 14);
        btnStart.Name = "btnStart";
        btnStart.Size = new Size(78, 32);
        btnStart.TabIndex = 14;
        btnStart.Text = "开始";
        btnStart.UseVisualStyleBackColor = true;
        btnStart.Click += BtnStart_Click;
        // 
        // btnStop
        // 
        btnStop.Enabled = false;
        btnStop.Location = new Point(1184, 14);
        btnStop.Name = "btnStop";
        btnStop.Size = new Size(78, 32);
        btnStop.TabIndex = 15;
        btnStop.Text = "停止";
        btnStop.UseVisualStyleBackColor = true;
        btnStop.Click += BtnStop_Click;
        // 
        // btnInitControl
        // 
        btnInitControl.Location = new Point(16, 54);
        btnInitControl.Name = "btnInitControl";
        btnInitControl.Size = new Size(96, 31);
        btnInitControl.TabIndex = 16;
        btnInitControl.Text = "初始化控制";
        btnInitControl.UseVisualStyleBackColor = true;
        btnInitControl.Click += BtnInitControl_Click;
        // 
        // btnTestAttack
        // 
        btnTestAttack.Location = new Point(122, 54);
        btnTestAttack.Name = "btnTestAttack";
        btnTestAttack.Size = new Size(104, 31);
        btnTestAttack.TabIndex = 17;
        btnTestAttack.Text = "测试推理攻击";
        btnTestAttack.UseVisualStyleBackColor = true;
        btnTestAttack.Click += BtnTestAttack_Click;
        // 
        // btnTestBoard
        // 
        btnTestBoard.Location = new Point(236, 54);
        btnTestBoard.Name = "btnTestBoard";
        btnTestBoard.Size = new Size(104, 31);
        btnTestBoard.TabIndex = 18;
        btnTestBoard.Text = "测试开发板";
        btnTestBoard.UseVisualStyleBackColor = true;
        btnTestBoard.Click += BtnTestBoard_Click;
        // 
        // btnTestPath
        // 
        btnTestPath.Location = new Point(350, 54);
        btnTestPath.Name = "btnTestPath";
        btnTestPath.Size = new Size(104, 31);
        btnTestPath.TabIndex = 19;
        btnTestPath.Text = "寻路测试";
        btnTestPath.UseVisualStyleBackColor = true;
        btnTestPath.Click += BtnTestPath_Click;
        // 
        // labelCapture
        // 
        labelCapture.AutoSize = true;
        labelCapture.Location = new Point(16, 95);
        labelCapture.Name = "labelCapture";
        labelCapture.Size = new Size(32, 17);
        labelCapture.TabIndex = 20;
        labelCapture.Text = "截图";
        // 
        // labelDepth
        // 
        labelDepth.AutoSize = true;
        labelDepth.Location = new Point(291, 95);
        labelDepth.Name = "labelDepth";
        labelDepth.Size = new Size(56, 17);
        labelDepth.TabIndex = 21;
        labelDepth.Text = "深度推理";
        // 
        // pictureBoxCapture
        // 
        pictureBoxCapture.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxCapture.Location = new Point(16, 118);
        pictureBoxCapture.Name = "pictureBoxCapture";
        pictureBoxCapture.Size = new Size(259, 259);
        pictureBoxCapture.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBoxCapture.TabIndex = 22;
        pictureBoxCapture.TabStop = false;
        // 
        // pictureBoxDepth
        // 
        pictureBoxDepth.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxDepth.Location = new Point(291, 118);
        pictureBoxDepth.Name = "pictureBoxDepth";
        pictureBoxDepth.Size = new Size(259, 259);
        pictureBoxDepth.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBoxDepth.TabIndex = 23;
        pictureBoxDepth.TabStop = false;
        // 
        // labelYolo
        // 
        labelYolo.AutoSize = true;
        labelYolo.Location = new Point(16, 445);
        labelYolo.Name = "labelYolo";
        labelYolo.Size = new Size(92, 17);
        labelYolo.TabIndex = 24;
        labelYolo.Text = "YOLO推理攻击";
        // 
        // panelYoloHost
        // 
        panelYoloHost.BackColor = SystemColors.ControlDark;
        panelYoloHost.BorderStyle = BorderStyle.FixedSingle;
        panelYoloHost.Location = new Point(16, 468);
        panelYoloHost.Name = "panelYoloHost";
        panelYoloHost.Size = new Size(500, 225);
        panelYoloHost.TabIndex = 25;
        // 
        // lblCaptureTime
        // 
        lblCaptureTime.AutoSize = true;
        lblCaptureTime.Font = new Font("Microsoft YaHei UI", 9F);
        lblCaptureTime.Location = new Point(16, 390);
        lblCaptureTime.Name = "lblCaptureTime";
        lblCaptureTime.Size = new Size(82, 17);
        lblCaptureTime.TabIndex = 26;
        lblCaptureTime.Text = "截图时间：-";
        // 
        // lblInferenceTime
        // 
        lblInferenceTime.AutoSize = true;
        lblInferenceTime.Font = new Font("Microsoft YaHei UI", 9F);
        lblInferenceTime.Location = new Point(16, 414);
        lblInferenceTime.Name = "lblInferenceTime";
        lblInferenceTime.Size = new Size(82, 17);
        lblInferenceTime.TabIndex = 27;
        lblInferenceTime.Text = "推理时间：-";
        // 
        // lblPassStatus
        // 
        lblPassStatus.AutoSize = true;
        lblPassStatus.Font = new Font("Microsoft YaHei UI", 9F);
        lblPassStatus.ForeColor = Color.Gray;
        lblPassStatus.Location = new Point(291, 390);
        lblPassStatus.Name = "lblPassStatus";
        lblPassStatus.Size = new Size(70, 17);
        lblPassStatus.TabIndex = 28;
        lblPassStatus.Text = "是否通过：-";
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Microsoft YaHei UI", 9F);
        lblStatus.ForeColor = Color.Blue;
        lblStatus.Location = new Point(16, 702);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(34, 17);
        lblStatus.TabIndex = 29;
        lblStatus.Text = "就绪";
        // 
        // lstControlLog
        // 
        lstControlLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lstControlLog.FormattingEnabled = true;
        lstControlLog.Location = new Point(16, 731);
        lstControlLog.Name = "lstControlLog";
        lstControlLog.Size = new Size(1246, 89);
        lstControlLog.TabIndex = 30;
        // 
        // MainWindow
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 840);
        Controls.Add(lstControlLog);
        Controls.Add(lblStatus);
        Controls.Add(lblPassStatus);
        Controls.Add(lblInferenceTime);
        Controls.Add(lblCaptureTime);
        Controls.Add(panelYoloHost);
        Controls.Add(labelYolo);
        Controls.Add(pictureBoxDepth);
        Controls.Add(pictureBoxCapture);
        Controls.Add(labelDepth);
        Controls.Add(labelCapture);
        Controls.Add(btnTestBoard);
        Controls.Add(btnTestPath);
        Controls.Add(btnTestAttack);
        Controls.Add(btnInitControl);
        Controls.Add(btnStop);
        Controls.Add(btnStart);
        Controls.Add(txtVmwarePort);
        Controls.Add(labelVmwarePort);
        Controls.Add(txtComPort);
        Controls.Add(labelComPort);
        Controls.Add(txtPathRotateThreshold);
        Controls.Add(labelPathRotateThreshold);
        Controls.Add(txtPathForwardThreshold);
        Controls.Add(labelPathForwardThreshold);
        Controls.Add(txtDarkThreshold);
        Controls.Add(labelDarkThreshold);
        Controls.Add(txtPassThreshold);
        Controls.Add(labelPassThreshold);
        Controls.Add(txtWindowHandle);
        Controls.Add(btnFindWindowHandle);
        MinimumSize = new Size(1296, 879);
        Name = "MainWindow";
        Text = "whatlanCar 深度推理";
        ((System.ComponentModel.ISupportInitialize)pictureBoxCapture).EndInit();
        ((System.ComponentModel.ISupportInitialize)pictureBoxDepth).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button btnFindWindowHandle;
    private TextBox txtWindowHandle;
    private Label labelPassThreshold;
    private TextBox txtPassThreshold;
    private Label labelDarkThreshold;
    private TextBox txtDarkThreshold;
    private Label labelPathForwardThreshold;
    private TextBox txtPathForwardThreshold;
    private Label labelPathRotateThreshold;
    private TextBox txtPathRotateThreshold;
    private Label labelComPort;
    private TextBox txtComPort;
    private Label labelVmwarePort;
    private TextBox txtVmwarePort;
    private Button btnStart;
    private Button btnStop;
    private Button btnInitControl;
    private Button btnTestAttack;
    private Button btnTestBoard;
    private Button btnTestPath;
    private Label labelCapture;
    private Label labelDepth;
    private PictureBox pictureBoxCapture;
    private PictureBox pictureBoxDepth;
    private Label labelYolo;
    private Panel panelYoloHost;
    private Label lblCaptureTime;
    private Label lblInferenceTime;
    private Label lblPassStatus;
    private Label lblStatus;
    private ListBox lstControlLog;
}
