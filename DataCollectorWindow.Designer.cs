namespace whatlanCar
{
    partial class DataCollectorWindow
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelSaveDir;
        private System.Windows.Forms.TextBox txtRootDir;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnOpenDir;
        private System.Windows.Forms.Label lblSession;
        private System.Windows.Forms.GroupBox groupBoxCaptureContent;
        private System.Windows.Forms.CheckBox chkCapture;
        private System.Windows.Forms.CheckBox chkDepth;
        private System.Windows.Forms.CheckBox chkMiniMap;
        private System.Windows.Forms.CheckBox chkActions;
        private System.Windows.Forms.Label lblKeyboard;
        private System.Windows.Forms.Label lblMouse;
        private System.Windows.Forms.Label lblFrame;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Button btnDepthInference;
        private System.Windows.Forms.Button btnTrainPolicy;
        private System.Windows.Forms.Button btnContinueTrainPolicy;
        private System.Windows.Forms.Label labelCaptureTime;
        private System.Windows.Forms.Label labelDepthTime;
        private System.Windows.Forms.Label labelMiniMapTime;
        private System.Windows.Forms.PictureBox pictureBoxCapture;
        private System.Windows.Forms.PictureBox pictureBoxDepth;
        private System.Windows.Forms.PictureBox pictureBoxMiniMap;
        private System.Windows.Forms.GroupBox groupBoxCapture;
        private System.Windows.Forms.GroupBox groupBoxDepth;
        private System.Windows.Forms.GroupBox groupBoxMiniMap;
        private System.Windows.Forms.ListBox lstLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

                private void InitializeComponent()
                {
                        labelSaveDir = new Label();
                        txtRootDir = new TextBox();
                        btnBrowse = new Button();
                        btnOpenDir = new Button();
                        lblSession = new Label();
                        groupBoxCaptureContent = new GroupBox();
                        chkCapture = new CheckBox();
                        chkDepth = new CheckBox();
                        chkMiniMap = new CheckBox();
                        chkActions = new CheckBox();
                        lblKeyboard = new Label();
                        lblMouse = new Label();
                        lblFrame = new Label();
                        lblStatus = new Label();
                        btnStartStop = new Button();
                        btnDepthInference = new Button();
                        btnTrainPolicy = new Button();
                        btnContinueTrainPolicy = new Button();
                        labelCaptureTime = new Label();
                        labelDepthTime = new Label();
                        labelMiniMapTime = new Label();
                        pictureBoxCapture = new PictureBox();
                        pictureBoxDepth = new PictureBox();
                        pictureBoxMiniMap = new PictureBox();
                        groupBoxCapture = new GroupBox();
                        groupBoxDepth = new GroupBox();
                        groupBoxMiniMap = new GroupBox();
                        lstLog = new ListBox();
                        groupBoxCaptureContent.SuspendLayout();
                        ((System.ComponentModel.ISupportInitialize)pictureBoxCapture).BeginInit();
                        ((System.ComponentModel.ISupportInitialize)pictureBoxDepth).BeginInit();
                        ((System.ComponentModel.ISupportInitialize)pictureBoxMiniMap).BeginInit();
                        groupBoxCapture.SuspendLayout();
                        groupBoxDepth.SuspendLayout();
                        groupBoxMiniMap.SuspendLayout();
                        SuspendLayout();
                        // 
                        // labelSaveDir
                        // 
                        labelSaveDir.AutoSize = true;
                        labelSaveDir.Location = new Point(12, 10);
                        labelSaveDir.Name = "labelSaveDir";
                        labelSaveDir.Size = new Size(56, 17);
                        labelSaveDir.TabIndex = 0;
                        labelSaveDir.Text = "保存目录";
                        // 
                        // txtRootDir
                        // 
                        txtRootDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        txtRootDir.Location = new Point(71, 7);
                        txtRootDir.Name = "txtRootDir";
                        txtRootDir.Size = new Size(736, 23);
                        txtRootDir.TabIndex = 1;
                        // 
                        // btnBrowse
                        // 
                        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                        btnBrowse.Location = new Point(813, 3);
                        btnBrowse.Name = "btnBrowse";
                        btnBrowse.Size = new Size(75, 31);
                        btnBrowse.TabIndex = 2;
                        btnBrowse.Text = "选择";
                        btnBrowse.UseVisualStyleBackColor = true;
                        btnBrowse.Click += BtnBrowse_Click;
                        // 
                        // btnOpenDir
                        // 
                        btnOpenDir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                        btnOpenDir.Location = new Point(894, 3);
                        btnOpenDir.Name = "btnOpenDir";
                        btnOpenDir.Size = new Size(75, 31);
                        btnOpenDir.TabIndex = 3;
                        btnOpenDir.Text = "打开";
                        btnOpenDir.UseVisualStyleBackColor = true;
                        btnOpenDir.Click += BtnOpenDir_Click;
                        // 
                        // lblSession
                        // 
                        lblSession.AutoSize = true;
                        lblSession.Location = new Point(12, 43);
                        lblSession.Name = "lblSession";
                        lblSession.Size = new Size(68, 17);
                        lblSession.TabIndex = 3;
                        lblSession.Text = "未开始采集";
                        // 
                        // groupBoxCaptureContent
                        // 
                        groupBoxCaptureContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        groupBoxCaptureContent.Controls.Add(chkCapture);
                        groupBoxCaptureContent.Controls.Add(chkDepth);
                        groupBoxCaptureContent.Controls.Add(chkMiniMap);
                        groupBoxCaptureContent.Controls.Add(chkActions);
                        groupBoxCaptureContent.Location = new Point(12, 74);
                        groupBoxCaptureContent.Name = "groupBoxCaptureContent";
                        groupBoxCaptureContent.Size = new Size(957, 57);
                        groupBoxCaptureContent.TabIndex = 4;
                        groupBoxCaptureContent.TabStop = false;
                        groupBoxCaptureContent.Text = "采集内容";
                        // 
                        // chkCapture
                        // 
                        chkCapture.AutoSize = true;
                        chkCapture.Checked = true;
                        chkCapture.CheckState = CheckState.Checked;
                        chkCapture.Location = new Point(15, 25);
                        chkCapture.Name = "chkCapture";
                        chkCapture.Size = new Size(63, 21);
                        chkCapture.TabIndex = 0;
                        chkCapture.Text = "截图帧";
                        chkCapture.UseVisualStyleBackColor = true;
                        // 
                        // chkDepth
                        // 
                        chkDepth.AutoSize = true;
                        chkDepth.Checked = true;
                        chkDepth.CheckState = CheckState.Checked;
                        chkDepth.Location = new Point(90, 25);
                        chkDepth.Name = "chkDepth";
                        chkDepth.Size = new Size(63, 21);
                        chkDepth.TabIndex = 1;
                        chkDepth.Text = "深度图";
                        chkDepth.UseVisualStyleBackColor = true;
                        // 
                        // chkMiniMap
                        // 
                        chkMiniMap.AutoSize = true;
                        chkMiniMap.Checked = true;
                        chkMiniMap.CheckState = CheckState.Checked;
                        chkMiniMap.Location = new Point(165, 25);
                        chkMiniMap.Name = "chkMiniMap";
                        chkMiniMap.Size = new Size(87, 21);
                        chkMiniMap.TabIndex = 2;
                        chkMiniMap.Text = "小地图裁剪";
                        chkMiniMap.UseVisualStyleBackColor = true;
                        // 
                        // chkActions
                        // 
                        chkActions.AutoSize = true;
                        chkActions.Checked = true;
                        chkActions.CheckState = CheckState.Checked;
                        chkActions.Location = new Point(254, 25);
                        chkActions.Name = "chkActions";
                        chkActions.Size = new Size(75, 21);
                        chkActions.TabIndex = 3;
                        chkActions.Text = "键盘动作";
                        chkActions.UseVisualStyleBackColor = true;
                        // 
                        // lblKeyboard
                        // 
                        lblKeyboard.AutoSize = true;
                        lblKeyboard.Location = new Point(15, 134);
                        lblKeyboard.Name = "lblKeyboard";
                        lblKeyboard.Size = new Size(49, 17);
                        lblKeyboard.TabIndex = 5;
                        lblKeyboard.Text = "键盘：-";
                        // 
                        // lblMouse
                        // 
                        lblMouse.AutoSize = true;
                        lblMouse.Location = new Point(15, 157);
                        lblMouse.Name = "lblMouse";
                        lblMouse.Size = new Size(49, 17);
                        lblMouse.TabIndex = 6;
                        lblMouse.Text = "鼠标：-";
                        // 
                        // lblFrame
                        // 
                        lblFrame.AutoSize = true;
                        lblFrame.Location = new Point(460, 134);
                        lblFrame.Name = "lblFrame";
                        lblFrame.Size = new Size(37, 17);
                        lblFrame.TabIndex = 7;
                        lblFrame.Text = "帧：-";
                        // 
                        // lblStatus
                        // 
                        lblStatus.AutoSize = true;
                        lblStatus.Location = new Point(460, 157);
                        lblStatus.Name = "lblStatus";
                        lblStatus.Size = new Size(49, 17);
                        lblStatus.TabIndex = 8;
                        lblStatus.Text = "状态：-";
                        // 
                        // btnStartStop
                        // 
                        btnStartStop.Location = new Point(12, 181);
                        btnStartStop.Name = "btnStartStop";
                        btnStartStop.Size = new Size(100, 41);
                        btnStartStop.TabIndex = 9;
                        btnStartStop.Text = "开始采集";
                        btnStartStop.UseVisualStyleBackColor = true;
                        btnStartStop.Click += BtnStartStop_Click;
                        // 
                        // btnDepthInference
                        // 
                        btnDepthInference.Location = new Point(130, 181);
                        btnDepthInference.Name = "btnDepthInference";
                        btnDepthInference.Size = new Size(140, 41);
                        btnDepthInference.TabIndex = 10;
                        btnDepthInference.Text = "检测手动深度推理";
                        btnDepthInference.UseVisualStyleBackColor = true;
                        btnDepthInference.Click += BtnDepthInference_Click;
                        // 
                        // btnTrainPolicy
                        // 
                        btnTrainPolicy.Location = new Point(290, 181);
                        btnTrainPolicy.Name = "btnTrainPolicy";
                        btnTrainPolicy.Size = new Size(100, 41);
                        btnTrainPolicy.TabIndex = 11;
                        btnTrainPolicy.Text = "训练模型";
                        btnTrainPolicy.UseVisualStyleBackColor = true;
                        btnTrainPolicy.Click += BtnTrainPolicy_Click;
                        // 
                        // btnContinueTrainPolicy
                        // 
                        btnContinueTrainPolicy.Location = new Point(406, 181);
                        btnContinueTrainPolicy.Name = "btnContinueTrainPolicy";
                        btnContinueTrainPolicy.Size = new Size(100, 41);
                        btnContinueTrainPolicy.TabIndex = 12;
                        btnContinueTrainPolicy.Text = "继续训练";
                        btnContinueTrainPolicy.UseVisualStyleBackColor = true;
                        btnContinueTrainPolicy.Click += BtnContinueTrainPolicy_Click;
                        // 
                        // labelCaptureTime
                        // 
                        labelCaptureTime.AutoSize = true;
                        labelCaptureTime.Location = new Point(6, -1);
                        labelCaptureTime.Name = "labelCaptureTime";
                        labelCaptureTime.Size = new Size(73, 17);
                        labelCaptureTime.TabIndex = 13;
                        labelCaptureTime.Text = "截图时间：-";
                        // 
                        // labelDepthTime
                        // 
                        labelDepthTime.AutoSize = true;
                        labelDepthTime.Location = new Point(6, 0);
                        labelDepthTime.Name = "labelDepthTime";
                        labelDepthTime.Size = new Size(73, 17);
                        labelDepthTime.TabIndex = 14;
                        labelDepthTime.Text = "推理时间：-";
                        // 
                        // labelMiniMapTime
                        // 
                        labelMiniMapTime.AutoSize = true;
                        labelMiniMapTime.Location = new Point(6, 0);
                        labelMiniMapTime.Name = "labelMiniMapTime";
                        labelMiniMapTime.Size = new Size(85, 17);
                        labelMiniMapTime.TabIndex = 15;
                        labelMiniMapTime.Text = "小地图时间：-";
                        // 
                        // pictureBoxCapture
                        // 
                        pictureBoxCapture.BorderStyle = BorderStyle.FixedSingle;
                        pictureBoxCapture.Dock = DockStyle.Fill;
                        pictureBoxCapture.Location = new Point(3, 19);
                        pictureBoxCapture.Name = "pictureBoxCapture";
                        pictureBoxCapture.Size = new Size(310, 354);
                        pictureBoxCapture.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBoxCapture.TabIndex = 0;
                        pictureBoxCapture.TabStop = false;
                        // 
                        // pictureBoxDepth
                        // 
                        pictureBoxDepth.BorderStyle = BorderStyle.FixedSingle;
                        pictureBoxDepth.Dock = DockStyle.Fill;
                        pictureBoxDepth.Location = new Point(3, 19);
                        pictureBoxDepth.Name = "pictureBoxDepth";
                        pictureBoxDepth.Size = new Size(310, 354);
                        pictureBoxDepth.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBoxDepth.TabIndex = 0;
                        pictureBoxDepth.TabStop = false;
                        // 
                        // pictureBoxMiniMap
                        // 
                        pictureBoxMiniMap.BorderStyle = BorderStyle.FixedSingle;
                        pictureBoxMiniMap.Dock = DockStyle.Fill;
                        pictureBoxMiniMap.Location = new Point(3, 19);
                        pictureBoxMiniMap.Name = "pictureBoxMiniMap";
                        pictureBoxMiniMap.Size = new Size(310, 354);
                        pictureBoxMiniMap.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBoxMiniMap.TabIndex = 0;
                        pictureBoxMiniMap.TabStop = false;
                        // 
                        // groupBoxCapture
                        // 
                        groupBoxCapture.Controls.Add(pictureBoxCapture);
                        groupBoxCapture.Controls.Add(labelCaptureTime);
                        groupBoxCapture.Location = new Point(12, 233);
                        groupBoxCapture.Name = "groupBoxCapture";
                        groupBoxCapture.Size = new Size(316, 376);
                        groupBoxCapture.TabIndex = 16;
                        groupBoxCapture.TabStop = false;
                        groupBoxCapture.Text = "截图";
                        // 
                        // groupBoxDepth
                        // 
                        groupBoxDepth.Controls.Add(pictureBoxDepth);
                        groupBoxDepth.Controls.Add(labelDepthTime);
                        groupBoxDepth.Location = new Point(334, 233);
                        groupBoxDepth.Name = "groupBoxDepth";
                        groupBoxDepth.Size = new Size(316, 376);
                        groupBoxDepth.TabIndex = 17;
                        groupBoxDepth.TabStop = false;
                        groupBoxDepth.Text = "深度图";
                        // 
                        // groupBoxMiniMap
                        // 
                        groupBoxMiniMap.Controls.Add(pictureBoxMiniMap);
                        groupBoxMiniMap.Controls.Add(labelMiniMapTime);
                        groupBoxMiniMap.Location = new Point(656, 233);
                        groupBoxMiniMap.Name = "groupBoxMiniMap";
                        groupBoxMiniMap.Size = new Size(316, 376);
                        groupBoxMiniMap.TabIndex = 18;
                        groupBoxMiniMap.TabStop = false;
                        groupBoxMiniMap.Text = "小地图";
                        // 
                        // lstLog
                        // 
                        lstLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                        lstLog.FormattingEnabled = true;
                        lstLog.Location = new Point(12, 613);
                        lstLog.Name = "lstLog";
                        lstLog.Size = new Size(960, 242);
                        lstLog.TabIndex = 19;
                        // 
                        // DataCollectorWindow
                        // 
                        AutoScaleDimensions = new SizeF(7F, 17F);
                        AutoScaleMode = AutoScaleMode.Font;
                        ClientSize = new Size(984, 869);
                        Controls.Add(lstLog);
                        Controls.Add(groupBoxMiniMap);
                        Controls.Add(groupBoxDepth);
                        Controls.Add(groupBoxCapture);
                        Controls.Add(btnContinueTrainPolicy);
                        Controls.Add(btnTrainPolicy);
                        Controls.Add(btnDepthInference);
                        Controls.Add(btnStartStop);
                        Controls.Add(lblStatus);
                        Controls.Add(lblFrame);
                        Controls.Add(lblMouse);
                        Controls.Add(lblKeyboard);
                        Controls.Add(groupBoxCaptureContent);
                        Controls.Add(lblSession);
                        Controls.Add(btnOpenDir);
                        Controls.Add(btnBrowse);
                        Controls.Add(txtRootDir);
                        Controls.Add(labelSaveDir);
                        FormBorderStyle = FormBorderStyle.FixedSingle;
                        MaximizeBox = false;
                        Name = "DataCollectorWindow";
                        Text = "数据采集器";
                        FormClosing += DataCollectorWindow_FormClosing;
                        groupBoxCaptureContent.ResumeLayout(false);
                        groupBoxCaptureContent.PerformLayout();
                        ((System.ComponentModel.ISupportInitialize)pictureBoxCapture).EndInit();
                        ((System.ComponentModel.ISupportInitialize)pictureBoxDepth).EndInit();
                        ((System.ComponentModel.ISupportInitialize)pictureBoxMiniMap).EndInit();
                        groupBoxCapture.ResumeLayout(false);
                        groupBoxCapture.PerformLayout();
                        groupBoxDepth.ResumeLayout(false);
                        groupBoxDepth.PerformLayout();
                        groupBoxMiniMap.ResumeLayout(false);
                        groupBoxMiniMap.PerformLayout();
                        ResumeLayout(false);
                        PerformLayout();
                }
        }
}