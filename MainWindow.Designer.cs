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
        pictureBoxCapture = new PictureBox();
        pictureBoxDepth = new PictureBox();
        pictureBoxYolo = new PictureBox();
        labelCapture = new Label();
        labelDepth = new Label();
        labelYolo = new Label();
        lblCaptureTime = new Label();
        lblInferenceTime = new Label();
        lblPassStatus = new Label();
        lblStatus = new Label();
        lstControlLog = new ListBox();
        ((System.ComponentModel.ISupportInitialize)pictureBoxCapture).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pictureBoxDepth).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pictureBoxYolo).BeginInit();
        SuspendLayout();
        // 
        // btnFindWindowHandle
        // 
        btnFindWindowHandle.Location = new Point(16, 9);
        btnFindWindowHandle.Name = "btnFindWindowHandle";
        btnFindWindowHandle.Size = new Size(104, 32);
        btnFindWindowHandle.TabIndex = 0;
        btnFindWindowHandle.Text = "获取窗口句柄";
        btnFindWindowHandle.UseVisualStyleBackColor = true;
        btnFindWindowHandle.Click += BtnFindWindowHandle_Click;
        // 
        // txtWindowHandle
        // 
        txtWindowHandle.Location = new Point(126, 14);
        txtWindowHandle.Name = "txtWindowHandle";
        txtWindowHandle.Size = new Size(88, 23);
        txtWindowHandle.TabIndex = 1;
        txtWindowHandle.Text = "329860";
        // 
        // labelPassThreshold
        // 
        labelPassThreshold.AutoSize = true;
        labelPassThreshold.Location = new Point(226, 18);
        labelPassThreshold.Name = "labelPassThreshold";
        labelPassThreshold.Size = new Size(90, 17);
        labelPassThreshold.TabIndex = 2;
        labelPassThreshold.Text = "障碍阈值(%)：";
        // 
        // txtPassThreshold
        // 
        txtPassThreshold.Location = new Point(315, 14);
        txtPassThreshold.Name = "txtPassThreshold";
        txtPassThreshold.Size = new Size(52, 23);
        txtPassThreshold.TabIndex = 3;
        txtPassThreshold.Text = "65";
        // 
        // labelDarkThreshold
        // 
        labelDarkThreshold.AutoSize = true;
        labelDarkThreshold.Location = new Point(376, 18);
        labelDarkThreshold.Name = "labelDarkThreshold";
        labelDarkThreshold.Size = new Size(104, 17);
        labelDarkThreshold.TabIndex = 4;
        labelDarkThreshold.Text = "前方暗区阈值：";
        // 
        // txtDarkThreshold
        // 
        txtDarkThreshold.Location = new Point(478, 14);
        txtDarkThreshold.Name = "txtDarkThreshold";
        txtDarkThreshold.Size = new Size(52, 23);
        txtDarkThreshold.TabIndex = 5;
        txtDarkThreshold.Text = "78";
        // 
        // labelPathForwardThreshold
        // 
        labelPathForwardThreshold.AutoSize = true;
        labelPathForwardThreshold.Location = new Point(545, 18);
        labelPathForwardThreshold.Name = "labelPathForwardThreshold";
        labelPathForwardThreshold.Size = new Size(56, 17);
        labelPathForwardThreshold.TabIndex = 6;
        labelPathForwardThreshold.Text = "直行<：";
        // 
        // txtPathForwardThreshold
        // 
        txtPathForwardThreshold.Location = new Point(602, 14);
        txtPathForwardThreshold.Name = "txtPathForwardThreshold";
        txtPathForwardThreshold.Size = new Size(44, 23);
        txtPathForwardThreshold.TabIndex = 7;
        txtPathForwardThreshold.Text = "45";
        // 
        // labelPathRotateThreshold
        // 
        labelPathRotateThreshold.AutoSize = true;
        labelPathRotateThreshold.Location = new Point(656, 18);
        labelPathRotateThreshold.Name = "labelPathRotateThreshold";
        labelPathRotateThreshold.Size = new Size(56, 17);
        labelPathRotateThreshold.TabIndex = 8;
        labelPathRotateThreshold.Text = "转向>：";
        // 
        // txtPathRotateThreshold
        // 
        txtPathRotateThreshold.Location = new Point(713, 14);
        txtPathRotateThreshold.Name = "txtPathRotateThreshold";
        txtPathRotateThreshold.Size = new Size(44, 23);
        txtPathRotateThreshold.TabIndex = 9;
        txtPathRotateThreshold.Text = "55";
        // 
        // labelComPort
        // 
        labelComPort.AutoSize = true;
        labelComPort.Location = new Point(768, 18);
        labelComPort.Name = "labelComPort";
        labelComPort.Size = new Size(56, 17);
        labelComPort.TabIndex = 10;
        labelComPort.Text = "COM口：";
        // 
        // txtComPort
        // 
        txtComPort.Location = new Point(825, 14);
        txtComPort.Name = "txtComPort";
        txtComPort.Size = new Size(60, 23);
        txtComPort.TabIndex = 11;
        txtComPort.Text = "COM5";
        // 
        // labelVmwarePort
        // 
        labelVmwarePort.AutoSize = true;
        labelVmwarePort.Location = new Point(895, 18);
        labelVmwarePort.Name = "labelVmwarePort";
        labelVmwarePort.Size = new Size(91, 17);
        labelVmwarePort.TabIndex = 12;
        labelVmwarePort.Text = "VMware端口：";
        // 
        // txtVmwarePort
        // 
        txtVmwarePort.Location = new Point(988, 14);
        txtVmwarePort.Name = "txtVmwarePort";
        txtVmwarePort.Size = new Size(60, 23);
        txtVmwarePort.TabIndex = 13;
        txtVmwarePort.Text = "5901";
        // 
        // btnStart
        // 
        btnStart.Location = new Point(1064, 9);
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
        btnStop.Location = new Point(1148, 9);
        btnStop.Name = "btnStop";
        btnStop.Size = new Size(78, 32);
        btnStop.TabIndex = 15;
        btnStop.Text = "停止";
        btnStop.UseVisualStyleBackColor = true;
        btnStop.Click += BtnStop_Click;
        // 
        // btnInitControl
        // 
        btnInitControl.Location = new Point(16, 43);
        btnInitControl.Name = "btnInitControl";
        btnInitControl.Size = new Size(96, 31);
        btnInitControl.TabIndex = 10;
        btnInitControl.Text = "初始化控制";
        btnInitControl.UseVisualStyleBackColor = true;
        btnInitControl.Click += BtnInitControl_Click;
        // 
        // btnTestAttack
        // 
        btnTestAttack.Location = new Point(122, 43);
        btnTestAttack.Name = "btnTestAttack";
        btnTestAttack.Size = new Size(104, 31);
        btnTestAttack.TabIndex = 11;
        btnTestAttack.Text = "测试推理攻击";
        btnTestAttack.UseVisualStyleBackColor = true;
        btnTestAttack.Click += BtnTestAttack_Click;
        // 
        // btnTestBoard
        // 
        btnTestBoard.Location = new Point(236, 43);
        btnTestBoard.Name = "btnTestBoard";
        btnTestBoard.Size = new Size(104, 31);
        btnTestBoard.TabIndex = 12;
        btnTestBoard.Text = "测试开发板";
        btnTestBoard.UseVisualStyleBackColor = true;
        btnTestBoard.Click += BtnTestBoard_Click;
        // 
        // btnTestPath
        // 
        btnTestPath.Location = new Point(350, 43);
        btnTestPath.Name = "btnTestPath";
        btnTestPath.Size = new Size(104, 31);
        btnTestPath.TabIndex = 13;
        btnTestPath.Text = "寻路测试";
        btnTestPath.UseVisualStyleBackColor = true;
        btnTestPath.Click += BtnTestPath_Click;
        // 
        // pictureBoxCapture
        // 
        pictureBoxCapture.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        pictureBoxCapture.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxCapture.Location = new Point(16, 106);
        pictureBoxCapture.Name = "pictureBoxCapture";
        pictureBoxCapture.Size = new Size(259, 259);
        pictureBoxCapture.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBoxCapture.TabIndex = 13;
        pictureBoxCapture.TabStop = false;
        // 
        // pictureBoxDepth
        // 
        pictureBoxDepth.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        pictureBoxDepth.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxDepth.Location = new Point(291, 106);
        pictureBoxDepth.Name = "pictureBoxDepth";
        pictureBoxDepth.Size = new Size(259, 259);
        pictureBoxDepth.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBoxDepth.TabIndex = 14;
        pictureBoxDepth.TabStop = false;
        // 
        // pictureBoxYolo
        // 
        pictureBoxYolo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        pictureBoxYolo.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxYolo.Location = new Point(16, 405);
        pictureBoxYolo.Name = "pictureBoxYolo";
        pictureBoxYolo.Size = new Size(500, 225);
        pictureBoxYolo.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBoxYolo.TabIndex = 22;
        pictureBoxYolo.TabStop = false;
        // 
        // labelCapture
        // 
        labelCapture.AutoSize = true;
        labelCapture.Location = new Point(16, 84);
        labelCapture.Name = "labelCapture";
        labelCapture.Size = new Size(32, 17);
        labelCapture.TabIndex = 15;
        labelCapture.Text = "截图";
        // 
        // labelDepth
        // 
        labelDepth.AutoSize = true;
        labelDepth.Location = new Point(291, 84);
        labelDepth.Name = "labelDepth";
        labelDepth.Size = new Size(56, 17);
        labelDepth.TabIndex = 16;
        labelDepth.Text = "深度推理";
        // 
        // labelYolo
        // 
        labelYolo.AutoSize = true;
        labelYolo.Location = new Point(16, 383);
        labelYolo.Name = "labelYolo";
        labelYolo.Size = new Size(91, 17);
        labelYolo.TabIndex = 23;
        labelYolo.Text = "YOLO推理攻击";
        // 
        // lblCaptureTime
        // 
        lblCaptureTime.AutoSize = true;
        lblCaptureTime.Font = new Font("Microsoft YaHei UI", 10F);
        lblCaptureTime.Location = new Point(16, 642);
        lblCaptureTime.Name = "lblCaptureTime";
        lblCaptureTime.Size = new Size(90, 20);
        lblCaptureTime.TabIndex = 17;
        lblCaptureTime.Text = "截图时间：-";
        // 
        // lblInferenceTime
        // 
        lblInferenceTime.AutoSize = true;
        lblInferenceTime.Font = new Font("Microsoft YaHei UI", 10F);
        lblInferenceTime.Location = new Point(16, 670);
        lblInferenceTime.Name = "lblInferenceTime";
        lblInferenceTime.Size = new Size(90, 20);
        lblInferenceTime.TabIndex = 18;
        lblInferenceTime.Text = "推理时间：-";
        // 
        // lblPassStatus
        // 
        lblPassStatus.AutoSize = true;
        lblPassStatus.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
        lblPassStatus.ForeColor = Color.Gray;
        lblPassStatus.Location = new Point(291, 638);
        lblPassStatus.Name = "lblPassStatus";
        lblPassStatus.Size = new Size(158, 31);
        lblPassStatus.TabIndex = 19;
        lblPassStatus.Text = "是否通过：-";
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.ForeColor = Color.Blue;
        lblStatus.Location = new Point(16, 698);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(32, 17);
        lblStatus.TabIndex = 20;
        lblStatus.Text = "就绪";
        // 
        // lstControlLog
        // 
        lstControlLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lstControlLog.FormattingEnabled = true;
        lstControlLog.ItemHeight = 17;
        lstControlLog.Location = new Point(16, 726);
        lstControlLog.Name = "lstControlLog";
        lstControlLog.Size = new Size(1256, 89);
        lstControlLog.TabIndex = 21;
        // 
        // MainWindow
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1288, 840);
        Controls.Add(lstControlLog);
        Controls.Add(lblStatus);
        Controls.Add(lblPassStatus);
        Controls.Add(lblInferenceTime);
        Controls.Add(lblCaptureTime);
        Controls.Add(labelYolo);
        Controls.Add(labelDepth);
        Controls.Add(labelCapture);
        Controls.Add(pictureBoxYolo);
        Controls.Add(pictureBoxDepth);
        Controls.Add(pictureBoxCapture);
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
        MinimumSize = new Size(1100, 760);
        Name = "MainWindow";
        Text = "whatlanCar 深度推理";
        ((System.ComponentModel.ISupportInitialize)pictureBoxCapture).EndInit();
        ((System.ComponentModel.ISupportInitialize)pictureBoxDepth).EndInit();
        ((System.ComponentModel.ISupportInitialize)pictureBoxYolo).EndInit();
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
    private PictureBox pictureBoxCapture;
    private PictureBox pictureBoxDepth;
    private PictureBox pictureBoxYolo;
    private Label labelCapture;
    private Label labelDepth;
    private Label labelYolo;
    private Label lblCaptureTime;
    private Label lblInferenceTime;
    private Label lblPassStatus;
    private Label lblStatus;
    private ListBox lstControlLog;
}
