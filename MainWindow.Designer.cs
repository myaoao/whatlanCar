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
                labelComPort = new Label();
                txtComPort = new TextBox();
                labelVmwarePort = new Label();
                txtVmwarePort = new TextBox();
                btnInitControl = new Button();
                btnTestAttack = new Button();
                btnTestBoard = new Button();
                btnCollectData = new Button();
                btnAiDrive = new Button();
                cmbPolicyModel = new ComboBox();
                labelAiPreview = new Label();
                pictureBoxAiPreview = new PictureBox();
                labelYolo = new Label();
                panelYoloHost = new Panel();
                lblStatus = new Label();
                lstControlLog = new ListBox();
                label1 = new Label();
                ((System.ComponentModel.ISupportInitialize)pictureBoxAiPreview).BeginInit();
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
                // labelComPort
                // 
                labelComPort.AutoSize = true;
                labelComPort.Location = new Point(221, 17);
                labelComPort.Name = "labelComPort";
                labelComPort.Size = new Size(62, 17);
                labelComPort.TabIndex = 10;
                labelComPort.Text = "COM口：";
                // 
                // txtComPort
                // 
                txtComPort.Location = new Point(282, 14);
                txtComPort.Name = "txtComPort";
                txtComPort.Size = new Size(60, 23);
                txtComPort.TabIndex = 11;
                txtComPort.Text = "COM5";
                // 
                // labelVmwarePort
                // 
                labelVmwarePort.AutoSize = true;
                labelVmwarePort.Location = new Point(356, 17);
                labelVmwarePort.Name = "labelVmwarePort";
                labelVmwarePort.Size = new Size(92, 17);
                labelVmwarePort.TabIndex = 12;
                labelVmwarePort.Text = "VMware端口：";
                // 
                // txtVmwarePort
                // 
                txtVmwarePort.Location = new Point(449, 14);
                txtVmwarePort.Name = "txtVmwarePort";
                txtVmwarePort.Size = new Size(60, 23);
                txtVmwarePort.TabIndex = 13;
                txtVmwarePort.Text = "5901";
                // 
                // btnInitControl
                // 
                btnInitControl.Location = new Point(17, 47);
                btnInitControl.Name = "btnInitControl";
                btnInitControl.Size = new Size(104, 32);
                btnInitControl.TabIndex = 16;
                btnInitControl.Text = "初始化控制";
                btnInitControl.UseVisualStyleBackColor = true;
                btnInitControl.Click += BtnInitControl_Click;
                // 
                // btnTestAttack
                // 
                btnTestAttack.Location = new Point(119, 47);
                btnTestAttack.Name = "btnTestAttack";
                btnTestAttack.Size = new Size(104, 32);
                btnTestAttack.TabIndex = 17;
                btnTestAttack.Text = "测试推理攻击";
                btnTestAttack.UseVisualStyleBackColor = true;
                btnTestAttack.Click += BtnTestAttack_Click;
                // 
                // btnTestBoard
                // 
                btnTestBoard.Location = new Point(221, 47);
                btnTestBoard.Name = "btnTestBoard";
                btnTestBoard.Size = new Size(104, 32);
                btnTestBoard.TabIndex = 18;
                btnTestBoard.Text = "测试开发板";
                btnTestBoard.UseVisualStyleBackColor = true;
                btnTestBoard.Click += BtnTestBoard_Click;
                // 
                // btnCollectData
                // 
                btnCollectData.Location = new Point(322, 47);
                btnCollectData.Name = "btnCollectData";
                btnCollectData.Size = new Size(104, 32);
                btnCollectData.TabIndex = 19;
                btnCollectData.Text = "数据采集";
                btnCollectData.UseVisualStyleBackColor = true;
                btnCollectData.Click += BtnCollectData_Click;
                // 
                // btnAiDrive
                // 
                btnAiDrive.Location = new Point(425, 47);
                btnAiDrive.Name = "btnAiDrive";
                btnAiDrive.Size = new Size(104, 32);
                btnAiDrive.TabIndex = 20;
                btnAiDrive.Text = "AI驾驶";
                btnAiDrive.UseVisualStyleBackColor = true;
                btnAiDrive.Click += BtnAiDrive_Click;
                // 
                // cmbPolicyModel
                // 
                cmbPolicyModel.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbPolicyModel.FormattingEnabled = true;
                cmbPolicyModel.Items.AddRange(new object[] { "纯画面", "画面+深度", "画面+深度+小地图" });
                cmbPolicyModel.Location = new Point(17, 85);
                cmbPolicyModel.Name = "cmbPolicyModel";
                cmbPolicyModel.Size = new Size(170, 25);
                cmbPolicyModel.TabIndex = 21;
                // 
                // labelAiPreview
                // 
                labelAiPreview.AutoSize = true;
                labelAiPreview.Location = new Point(17, 116);
                labelAiPreview.Name = "labelAiPreview";
                labelAiPreview.Size = new Size(68, 17);
                labelAiPreview.TabIndex = 22;
                labelAiPreview.Text = "AI驾驶预览";
                // 
                // pictureBoxAiPreview
                // 
                pictureBoxAiPreview.BackColor = Color.Black;
                pictureBoxAiPreview.BorderStyle = BorderStyle.FixedSingle;
                pictureBoxAiPreview.Location = new Point(17, 136);
                pictureBoxAiPreview.Name = "pictureBoxAiPreview";
                pictureBoxAiPreview.Size = new Size(512, 259);
                pictureBoxAiPreview.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBoxAiPreview.TabIndex = 23;
                pictureBoxAiPreview.TabStop = false;
                // 
                // labelYolo
                // 
                labelYolo.AutoSize = true;
                labelYolo.Location = new Point(17, 398);
                labelYolo.Name = "labelYolo";
                labelYolo.Size = new Size(89, 17);
                labelYolo.TabIndex = 24;
                labelYolo.Text = "YOLO推理攻击";
                // 
                // panelYoloHost
                // 
                panelYoloHost.BackColor = SystemColors.ControlDark;
                panelYoloHost.BorderStyle = BorderStyle.FixedSingle;
                panelYoloHost.Location = new Point(17, 418);
                panelYoloHost.Name = "panelYoloHost";
                panelYoloHost.Size = new Size(512, 225);
                panelYoloHost.TabIndex = 25;
                // 
                // lblStatus
                // 
                lblStatus.AutoSize = true;
                lblStatus.Font = new Font("Microsoft YaHei UI", 9F);
                lblStatus.ForeColor = Color.Blue;
                lblStatus.Location = new Point(17, 646);
                lblStatus.Name = "lblStatus";
                lblStatus.Size = new Size(32, 17);
                lblStatus.TabIndex = 29;
                lblStatus.Text = "就绪";
                // 
                // lstControlLog
                // 
                lstControlLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                lstControlLog.FormattingEnabled = true;
                lstControlLog.Location = new Point(17, 666);
                lstControlLog.Name = "lstControlLog";
                lstControlLog.Size = new Size(516, 327);
                lstControlLog.TabIndex = 30;
                // 
                // label1
                // 
                label1.AutoSize = true;
                label1.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
                label1.ForeColor = SystemColors.AppWorkspace;
                label1.Location = new Point(425, 996);
                label1.Name = "label1";
                label1.Size = new Size(108, 21);
                label1.TabIndex = 31;
                label1.Text = "by:GINK1026";
                // 
                // MainWindow
                // 
                AutoScaleDimensions = new SizeF(7F, 17F);
                AutoScaleMode = AutoScaleMode.Font;
                ClientSize = new Size(541, 1021);
                Controls.Add(label1);
                Controls.Add(lstControlLog);
                Controls.Add(lblStatus);
                Controls.Add(panelYoloHost);
                Controls.Add(labelYolo);
                Controls.Add(pictureBoxAiPreview);
                Controls.Add(labelAiPreview);
                Controls.Add(cmbPolicyModel);
                Controls.Add(btnAiDrive);
                Controls.Add(btnCollectData);
                Controls.Add(btnTestBoard);
                Controls.Add(btnTestAttack);
                Controls.Add(btnInitControl);
                Controls.Add(txtVmwarePort);
                Controls.Add(labelVmwarePort);
                Controls.Add(txtComPort);
                Controls.Add(labelComPort);
                Controls.Add(txtWindowHandle);
                Controls.Add(btnFindWindowHandle);
                MaximizeBox = false;
                Name = "MainWindow";
                Text = "whatlanCar'Box";
                ((System.ComponentModel.ISupportInitialize)pictureBoxAiPreview).EndInit();
                ResumeLayout(false);
                PerformLayout();
        }

        #endregion

        private Button btnFindWindowHandle;
    private TextBox txtWindowHandle;
    private Label labelComPort;
    private TextBox txtComPort;
    private Label labelVmwarePort;
    private TextBox txtVmwarePort;
    private Button btnInitControl;
    private Button btnTestAttack;
    private Button btnTestBoard;
    private Button btnCollectData;
    private Button btnAiDrive;
    private ComboBox cmbPolicyModel;
    private Label labelAiPreview;
    private PictureBox pictureBoxAiPreview;
    private Label labelYolo;
    private Panel panelYoloHost;
    private Label lblStatus;
    private ListBox lstControlLog;
        private Label label1;
}
