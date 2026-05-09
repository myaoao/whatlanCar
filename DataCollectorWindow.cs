using OpenCvSharp;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace whatlanCar;

public sealed partial class DataCollectorWindow : Form
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int VmKeyboardUdpPort = 47891;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 50 };
    private readonly System.Windows.Forms.Timer _depthTimer = new() { Interval = 100 };
    private readonly object _saveLock = new();
    private readonly object _keyStateLock = new();
    private readonly object _depthInferenceLock = new();
    private readonly HashSet<Keys> _hookKeysDown = new();

    private StreamWriter? _actionWriter;
    private StreamWriter? _manifestWriter;
    private string? _sessionDir;
    private System.Drawing.Point _lastMousePosition;
    private KeySnapshot _lastKeys;
    private int _lastMouseDx;
    private int _lastMouseDy;
    private long _sampleIndex;
    private long _frameIndex;
    private long _lastLoggedFrameIndex;
    private long _keyboardEventCount;
    private long _vmKeyboardPacketCount;
    private KeySnapshot _vmKeys;
    private int _vmMouseDx;
    private int _vmMouseDy;
    private int _vmMouseX;
    private int _vmMouseY;
    private DateTime _lastVmKeyboardPacketUtc = DateTime.MinValue;
    private bool _isCollecting;
    private bool _isDepthRunning;
    private LowLevelKeyboardProc? _keyboardProc;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private UdpClient? _vmKeyboardUdp;
    private CancellationTokenSource? _vmKeyboardCts;
    private DepthAnythingInference? _depthInference;
    private Mat? _lastCapture;
    private Mat? _lastDepth;
    private string _windowHandleText = string.Empty;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(Keys vKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public DataCollectorWindow()
    {
        InitializeComponent();
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new System.Drawing.Size(820, 500);

        _timer.Tick += Timer_Tick;
        _depthTimer.Tick += DepthTimer_Tick;
        chkDepth.CheckedChanged += (_, _) => UpdatePreviewPanelVisibility();
        chkMiniMap.CheckedChanged += (_, _) => UpdatePreviewPanelVisibility();
        txtRootDir.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "whatlanCar-dataset");
        LoadDepthModel();
        UpdatePreviewPanelVisibility();
    }

    private void LoadDepthModel()
    {
        try
        {
            var modelPath = Path.Combine(AppContext.BaseDirectory, "data", "depth_anything_vits14.onnx");
            _depthInference = new DepthAnythingInference(modelPath);
            lblStatus.Text = $"模型已加载：{_depthInference.ExecutionProvider}";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "模型加载失败";
            MessageBox.Show($"加载深度模型失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void CaptureFrame(Mat capture, Mat? depthMap)
    {
        if (!_isCollecting || _sessionDir == null)
        {
            return;
        }

        lock (_saveLock)
        {
            var frameId = _frameIndex++;
            var stem = frameId.ToString("D8", CultureInfo.InvariantCulture);

            try
            {
                if (chkCapture.Checked)
                {
                    SaveMat(Path.Combine(_sessionDir, "frames", $"{stem}.png"), capture);
                }

                if (chkDepth.Checked && depthMap != null)
                {
                    using var depthPreview = CreateDepthPreview(depthMap);
                    SaveMat(Path.Combine(_sessionDir, "depth", $"{stem}.png"), depthPreview);
                }

                if (chkMiniMap.Checked)
                {
                    using var miniMap = CropMiniMapRegion(capture);
                    SaveMat(Path.Combine(_sessionDir, "minimap", $"{stem}.png"), miniMap);
                }

                WriteManifest(frameId, stem);

                lblFrame.Text = $"帧保存：{frameId}";
                if (frameId == 0 || frameId - _lastLoggedFrameIndex >= 30)
                {
                    _lastLoggedFrameIndex = frameId;
                    AppendLog($"图像帧已保存：{frameId}，目录：{_sessionDir}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"图像保存失败：{ex.Message}");
            }
        }
    }

    private static Mat CreateDepthPreview(Mat depthMap)
    {
        return DepthAnythingInference.CreateVisualization(depthMap);
    }

    private void WriteManifest(long frameId, string stem)
    {
        if (_manifestWriter == null) return;

        var framePath = chkCapture.Checked ? $"frames/{stem}.png" : string.Empty;
        var depthPath = chkDepth.Checked ? $"depth/{stem}.png" : string.Empty;
        var minimapPath = chkMiniMap.Checked ? $"minimap/{stem}.png" : string.Empty;

        _manifestWriter.WriteLine(string.Join(',',
            frameId.ToString(CultureInfo.InvariantCulture),
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            QuoteCsv(framePath),
            QuoteCsv(depthPath),
            QuoteCsv(minimapPath),
            BoolToInt(_lastKeys.W),
            BoolToInt(_lastKeys.A),
            BoolToInt(_lastKeys.S),
            BoolToInt(_lastKeys.D),
            BoolToInt(_lastKeys.Jump),
            BoolToInt(_lastKeys.M),
            _lastMouseDx.ToString(CultureInfo.InvariantCulture),
            _lastMouseDy.ToString(CultureInfo.InvariantCulture)));
        _manifestWriter.Flush();
    }

    private static string QuoteCsv(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static int BoolToInt(bool value) => value ? 1 : 0;

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择数据集保存目录",
            SelectedPath = Directory.Exists(txtRootDir.Text)
            ? txtRootDir.Text
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtRootDir.Text = dialog.SelectedPath;
        }
    }

    private void BtnOpenDir_Click(object? sender, EventArgs e)
    {
        var dir = txtRootDir.Text.Trim();
        if (!Directory.Exists(dir))
        {
            MessageBox.Show("鐩綍涓嶅瓨鍦細" + dir, "閿欒", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"\"{dir}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开目录失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnStartStop_Click(object? sender, EventArgs e)
    {
        if (_isCollecting)
        {
            StopCollecting();
        }
        else
        {
            StartCollecting();
        }
    }

    private void StartCollecting()
    {
        var rootDir = string.IsNullOrWhiteSpace(txtRootDir.Text)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "whatlanCar-dataset")
            : txtRootDir.Text.Trim();

        _sessionDir = Path.Combine(rootDir, DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(_sessionDir);
        Directory.CreateDirectory(Path.Combine(_sessionDir, "frames"));
        Directory.CreateDirectory(Path.Combine(_sessionDir, "depth"));
        Directory.CreateDirectory(Path.Combine(_sessionDir, "minimap"));

        _actionWriter = new StreamWriter(Path.Combine(_sessionDir, "actions.csv"), false, new UTF8Encoding(false));
        _actionWriter.WriteLine("sample,time_utc,w,a,s,d,jump,m,mouse_dx,mouse_dy,mouse_x,mouse_y");
        _actionWriter.Flush();
        _manifestWriter = new StreamWriter(Path.Combine(_sessionDir, "frame_manifest.csv"), false, new UTF8Encoding(false));
        _manifestWriter.WriteLine("frame_id,time_utc,frame_path,depth_path,minimap_path,w,a,s,d,jump,m,mouse_dx,mouse_dy");
        _manifestWriter.Flush();

        _sampleIndex = 0;
        _frameIndex = 0;
        _lastLoggedFrameIndex = -1;
        _keyboardEventCount = 0;
        _vmKeyboardPacketCount = 0;
        _vmKeys = default;
        _vmMouseDx = 0;
        _vmMouseDy = 0;
        _vmMouseX = 0;
        _vmMouseY = 0;
        _lastVmKeyboardPacketUtc = DateTime.MinValue;
        GlobalKeyboardState.Clear();
        _lastMousePosition = Cursor.Position;
        _lastKeys = default;
        _lastMouseDx = 0;
        _lastMouseDy = 0;
        _isCollecting = true;
        btnStartStop.Text = "鍋滄";
        lblSession.Text = $"采集会话：{_sessionDir}";
        AppendLog("数据采集已开始。键盘采集版本：VM UDP + 采集器Hook + 主窗口Hook + 轮询。");
        StartVmKeyboardReceiver();
        InstallKeyboardHook();
        UpdatePreviewPanelVisibility();
        _timer.Start();

        if (chkDepth.Checked && !_isDepthRunning && _depthInference == null)
        {
            LoadDepthModel();
        }
        if (!_isDepthRunning)
        {
            _isDepthRunning = true;
            btnDepthInference.Text = "鍋滄娣卞害鎺ㄧ悊";
            lblStatus.Text = chkDepth.Checked ? "深度推理运行中" : "采集预览运行中";
            _depthTimer.Start();
            AppendLog(chkDepth.Checked ? "深度推理已自动开启" : "采集预览已开启");
        }
    }

    private void StopCollecting()
    {
        _timer.Stop();
        _actionWriter?.Dispose();
        _actionWriter = null;
        _manifestWriter?.Dispose();
        _manifestWriter = null;
        _isCollecting = false;
        btnStartStop.Text = "开始采集";
        UninstallKeyboardHook();
        StopVmKeyboardReceiver();
        AppendLog("数据采集已停止。");

        if (_isDepthRunning)
        {
            StopDepthInference();
            AppendLog("深度推理已自动停止");
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_isCollecting)
        {
            return;
        }

        var keys = ReadKeyState(true);
        var vmFresh = DateTime.UtcNow - _lastVmKeyboardPacketUtc < TimeSpan.FromMilliseconds(500);
        var mouse = Cursor.Position;
        var hostDx = mouse.X - _lastMousePosition.X;
        var hostDy = mouse.Y - _lastMousePosition.Y;
        _lastMousePosition = mouse;
        var dx = vmFresh ? _vmMouseDx : hostDx;
        var dy = vmFresh ? _vmMouseDy : hostDy;
        var mouseX = vmFresh ? _vmMouseX : mouse.X;
        var mouseY = vmFresh ? _vmMouseY : mouse.Y;

        _lastKeys = keys;
        _lastMouseDx = dx;
        _lastMouseDy = dy;
        var vmAgeMs = _lastVmKeyboardPacketUtc == DateTime.MinValue
            ? -1
            : (int)(DateTime.UtcNow - _lastVmKeyboardPacketUtc).TotalMilliseconds;
        lblKeyboard.Text = $"閿洏锛歐={keys.W} A={keys.A} S={keys.S} D={keys.D} 璺?{keys.Jump} M={keys.M} 浜嬩欢={_keyboardEventCount}+{GlobalKeyboardState.EventCount} VM={_vmKeyboardPacketCount}/{vmAgeMs}ms";
        lblMouse.Text = $"榧犳爣锛歞x={dx}, dy={dy}, x={mouseX}, y={mouseY} 鏉ユ簮={(vmFresh ? "VM" : "Host")}";

        if (chkActions.Checked && _actionWriter != null)
        {
            _actionWriter.WriteLine(string.Join(
                ',',
                _sampleIndex.ToString(CultureInfo.InvariantCulture),
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                BoolToInt(keys.W),
                BoolToInt(keys.A),
                BoolToInt(keys.S),
                BoolToInt(keys.D),
                BoolToInt(keys.Jump),
                BoolToInt(keys.M),
                dx.ToString(CultureInfo.InvariantCulture),
                dy.ToString(CultureInfo.InvariantCulture),
                mouseX.ToString(CultureInfo.InvariantCulture),
                mouseY.ToString(CultureInfo.InvariantCulture)));
            _actionWriter.Flush();
        }

        _sampleIndex++;
    }

    private KeySnapshot ReadKeyState(bool preferVm = false)
    {
        var vmFresh = DateTime.UtcNow - _lastVmKeyboardPacketUtc < TimeSpan.FromMilliseconds(500);
        if (preferVm && vmFresh && _vmKeys != default)
        {
            return _vmKeys;
        }
        
        return new KeySnapshot
        {
            W = (GetAsyncKeyState(Keys.W) & 0x8000) != 0,
            A = (GetAsyncKeyState(Keys.A) & 0x8000) != 0,
            S = (GetAsyncKeyState(Keys.S) & 0x8000) != 0,
            D = (GetAsyncKeyState(Keys.D) & 0x8000) != 0,
            Jump = (GetAsyncKeyState(Keys.Space) & 0x8000) != 0,
            M = (GetAsyncKeyState(Keys.M) & 0x8000) != 0
        };
    }

    private readonly record struct KeySnapshot(bool W, bool A, bool S, bool D, bool Jump, bool M);

    private void AppendLog(string message)
    {
        if (lstLog.InvokeRequired)
        {
            lstLog.Invoke(new Action<string>(AppendLog), message);
            return;
        }

        var line = $"{DateTime.Now:HH:mm:ss} {message}";
        lstLog.Items.Add(line);
        if (lstLog.Items.Count > 200)
        {
            lstLog.Items.RemoveAt(0);
        }
        lstLog.SelectedIndex = lstLog.Items.Count - 1;
    }

    private void InstallKeyboardHook()
    {
        if (_keyboardProc != null) return;

        _keyboardProc = KeyboardHookCallback;
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, GetModuleHandle(null), 0);
        if (_keyboardHook == IntPtr.Zero)
        {
            AppendLog($"安装键盘Hook失败：{Marshal.GetLastWin32Error()}");
        }
    }

    private void UninstallKeyboardHook()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
        _keyboardProc = null;
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;
            lock (_keyStateLock)
            {
                _hookKeysDown.Add(key);
            }
            _keyboardEventCount++;
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private void StartVmKeyboardReceiver()
    {
        if (_vmKeyboardUdp != null) return;

        try
        {
            _vmKeyboardUdp = new UdpClient(VmKeyboardUdpPort);
            _vmKeyboardCts = new CancellationTokenSource();
            _ = Task.Run(VmKeyboardReceiveLoop);
            AppendLog($"VM键盘接收已启动，端口 {VmKeyboardUdpPort}");
        }
        catch (Exception ex)
        {
            AppendLog($"启动VM键盘接收失败：{ex.Message}");
        }
    }

    private void StopVmKeyboardReceiver()
    {
        _vmKeyboardCts?.Cancel();
        _vmKeyboardCts?.Dispose();
        _vmKeyboardCts = null;
        _vmKeyboardUdp?.Close();
        _vmKeyboardUdp?.Dispose();
        _vmKeyboardUdp = null;
    }

    private async Task VmKeyboardReceiveLoop()
    {
        var token = _vmKeyboardCts!.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var result = await _vmKeyboardUdp!.ReceiveAsync(token);
                var data = result.Buffer;
                try
                {
                    var message = System.Text.Encoding.UTF8.GetString(data);
                    var parts = message.Split(',');
                    var keyValues = new Dictionary<string, string>();
                    foreach (var part in parts)
                    {
                        var kv = part.Split('=');
                        if (kv.Length == 2)
                        {
                            keyValues[kv[0].Trim()] = kv[1].Trim();
                        }
                    }

                    _vmKeys = new KeySnapshot
                    {
                        W = GetIntValue(keyValues, "w") != 0,
                        A = GetIntValue(keyValues, "a") != 0,
                        S = GetIntValue(keyValues, "s") != 0,
                        D = GetIntValue(keyValues, "d") != 0,
                        Jump = GetIntValue(keyValues, "jump") != 0,
                        M = GetIntValue(keyValues, "m") != 0
                    };
                    _vmMouseDx = GetIntValue(keyValues, "mouse_dx");
                    _vmMouseDy = GetIntValue(keyValues, "mouse_dy");
                    _vmMouseX = GetIntValue(keyValues, "mouse_x");
                    _vmMouseY = GetIntValue(keyValues, "mouse_y");
                    _vmKeyboardPacketCount++;
                    _lastVmKeyboardPacketUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    AppendLog($"解析VM键盘数据失败：{ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppendLog($"VM键盘接收错误：{ex.Message}");
        }
    }

    private int GetIntValue(Dictionary<string, string> keyValues, string key)
    {
        if (keyValues.TryGetValue(key, out var value) && int.TryParse(value, out var result))
        {
            return result;
        }
        return 0;
    }

    private static void SaveMat(string path, Mat image)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!Cv2.ImWrite(path, image))
        {
            throw new IOException($"OpenCV write failed: {path}");
        }
    }

    private static Mat CropMiniMapRegion(Mat capture)
    {
        const double baseWidth = 1440.0;
        const double baseHeight = 900.0;
        var x1 = ScaleX(26, capture.Width, baseWidth);
        var y1 = ScaleY(25, capture.Height, baseHeight);
        var x2 = ScaleX(389, capture.Width, baseWidth);
        var y2 = ScaleY(420, capture.Height, baseHeight);

        x1 = Math.Clamp(x1, 0, capture.Width - 1);
        y1 = Math.Clamp(y1, 0, capture.Height - 1);
        x2 = Math.Clamp(x2, x1 + 1, capture.Width);
        y2 = Math.Clamp(y2, y1 + 1, capture.Height);

        return new Mat(capture, new Rect(x1, y1, x2 - x1, y2 - y1)).Clone();
    }

    private static int ScaleX(int x, int actualWidth, double baseWidth) => (int)(x * actualWidth / baseWidth);
    private static int ScaleY(int y, int actualHeight, double baseHeight) => (int)(y * actualHeight / baseHeight);

    private string? FindTrainingScript()
    {
        var projectDirPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "training", "train_policy.py");
        projectDirPath = Path.GetFullPath(projectDirPath);
        if (File.Exists(projectDirPath))
        {
            return projectDirPath;
        }

        return null;
    }

    private async void BtnTrainPolicy_Click(object? sender, EventArgs e)
    {
        var scriptPath = FindTrainingScript();
        if (scriptPath == null)
        {
            MessageBox.Show("训练脚本不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await StartTrainingScriptAsync(scriptPath, string.Empty);
    }

    private async void BtnContinueTrainPolicy_Click(object? sender, EventArgs e)
    {
        var scriptPath = FindTrainingScript();
        if (scriptPath == null)
        {
            MessageBox.Show("训练脚本不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await StartTrainingScriptAsync(scriptPath, "--resume --lr 0.0002");
    }

    private async Task StartTrainingScriptAsync(string scriptPath, string arguments)
    {
        var dataDir = string.IsNullOrWhiteSpace(txtRootDir.Text)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : txtRootDir.Text.Trim();

        var trainingDir = Path.GetDirectoryName(scriptPath)!;
        var outputDir = Path.Combine(AppContext.BaseDirectory, "data");
        var outFilePath = Path.Combine(outputDir, GetPolicyOutputFileName());

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var args = $"\"{scriptPath}\" --data \"{dataDir}\" --out \"{outFilePath}\" --batch-size 32 --workers 2";
        if (!chkDepth.Checked)
        {
            args += " --no-depth";
        }
        if (!chkDepth.Checked || !chkMiniMap.Checked)
        {
            args += " --no-minimap";
        }
        if (!string.IsNullOrEmpty(arguments))
        {
            args += " " + arguments;
        }

        var psi = new System.Diagnostics.ProcessStartInfo(GetTrainingPythonPath(scriptPath), args)
        {
            WorkingDirectory = trainingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        try
        {
            using var process = new System.Diagnostics.Process { StartInfo = psi };
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    AppendLog(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    AppendLog(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            AppendLog($"璁粌鑴氭湰宸插惎鍔? {scriptPath}");
            AppendLog($"鏁版嵁鐩綍: {dataDir}");
            AppendLog($"杈撳嚭鏂囦欢: {outFilePath}");

            await process.WaitForExitAsync();
            AppendLog($"训练脚本结束，退出代码 {process.ExitCode}");
        }
        catch (Exception ex)
        {
            AppendLog($"启动训练脚本失败：{ex.Message}");
            MessageBox.Show($"启动训练脚本失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string GetPolicyOutputFileName()
    {
        if (chkDepth.Checked && chkMiniMap.Checked)
        {
            return "policy_rgb_depth_minimap.onnx";
        }

        return chkDepth.Checked
            ? "policy_rgb_depth.onnx"
            : "policy_rgb.onnx";
    }

    private static string GetTrainingPythonPath(string scriptPath)
    {
        var trainingDir = Path.GetDirectoryName(scriptPath)!;
        var venvPython = Path.Combine(trainingDir, ".venv", "Scripts", "python.exe");
        return File.Exists(venvPython) ? venvPython : "python";
    }

    private void BtnDepthInference_Click(object? sender, EventArgs e)
    {
        if (_isDepthRunning)
        {
            StopDepthInference();
            lblStatus.Text = "已停止深度推理";
            return;
        }

        if (_depthInference == null)
        {
            MessageBox.Show("深度模型尚未加载成功。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _isDepthRunning = true;
        btnDepthInference.Text = "停止深度推理";
        lblStatus.Text = "深度推理运行中";
        _depthTimer.Start();
        AppendLog("开始手动深度推理");
    }

    private void StopDepthInference()
    {
        _isDepthRunning = false;
        _depthTimer.Stop();
        btnDepthInference.Text = "检测手动深度推理";
    }

    private void UpdatePreviewPanelVisibility()
    {
        groupBoxDepth.Visible = chkDepth.Checked;
        groupBoxMiniMap.Visible = chkMiniMap.Checked;
        if (!chkDepth.Checked)
        {
            pictureBoxDepth.Image?.Dispose();
            pictureBoxDepth.Image = null;
            labelDepthTime.Text = "推理时间：-";
        }
        if (!chkMiniMap.Checked)
        {
            pictureBoxMiniMap.Image?.Dispose();
            pictureBoxMiniMap.Image = null;
            labelMiniMapTime.Text = "小地图时间：-";
        }

        var visibleGroups = new[] { groupBoxCapture, groupBoxDepth, groupBoxMiniMap }
            .Where(group => group.Visible)
            .ToArray();
        if (visibleGroups.Length == 0)
        {
            return;
        }

        const int gap = 6;
        var left = 12;
        var top = groupBoxCapture.Top;
        var availableWidth = ClientSize.Width - 24 - gap * (visibleGroups.Length - 1);
        var width = Math.Max(240, availableWidth / visibleGroups.Length);
        foreach (var group in visibleGroups)
        {
            group.SetBounds(left, top, width, groupBoxCapture.Height);
            left += width + gap;
        }
    }

    private void DepthTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isDepthRunning)
        {
            return;
        }

        try
        {
            var handle = VmwareWindowHelper.FindEmbeddedMksWindow();
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var captureWatch = System.Diagnostics.Stopwatch.StartNew();
            using var rawCapture = WindowCapture.CaptureWindow(handle);
            using var captured = CropCenterSquare(rawCapture);
            captureWatch.Stop();

            _lastCapture?.Dispose();
            _lastCapture = captured.Clone();
            ShowMat(_lastCapture, pictureBoxCapture);

            long inferenceMs = 0;
            _lastDepth?.Dispose();
            _lastDepth = null;
            if (chkDepth.Checked && _depthInference != null)
            {
                _lastDepth = PredictDepthThreadSafe(_lastCapture, out inferenceMs);
                using var depthPreview = DepthAnythingInference.CreateVisualization(_lastDepth);
                ShowMat(depthPreview, pictureBoxDepth);
            }

            var miniMapWatch = System.Diagnostics.Stopwatch.StartNew();
            if (chkMiniMap.Checked)
            {
                using var miniMap = CropMiniMapRegion(rawCapture);
                ShowMat(miniMap, pictureBoxMiniMap);
            }
            miniMapWatch.Stop();

            if (_isCollecting && _sessionDir != null)
            {
                CaptureFrame(rawCapture, _lastDepth);
            }

            labelCaptureTime.Text = $"截图时间：{captureWatch.ElapsedMilliseconds} ms";
            if (chkDepth.Checked && _depthInference != null)
            {
                labelDepthTime.Text = $"推理时间：{inferenceMs} ms（平均 {_depthInference.AverageInferenceMs:F1} ms，{_depthInference.ExecutionProvider}）";
            }
            if (chkMiniMap.Checked)
            {
                labelMiniMapTime.Text = $"小地图时间：{miniMapWatch.Elapsed.TotalMilliseconds:F2} ms";
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"深度推理错误：{ex.Message}";
        }
    }

    private static Mat CropCenterSquare(Mat source)
    {
        var size = Math.Min(source.Width, source.Height);
        var x = (source.Width - size) / 2;
        var y = (source.Height - size) / 2;
        return source.SubMat(y, y + size, x, x + size);
    }

    private Mat PredictDepthThreadSafe(Mat image, out long inferenceMs)
    {
        lock (_depthInferenceLock)
        {
            return _depthInference!.PredictDepth(image, out inferenceMs);
        }
    }

    private static void ShowMat(Mat mat, PictureBox pictureBox)
    {
        using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        pictureBox.Image?.Dispose();
        pictureBox.Image = (System.Drawing.Bitmap)bitmap.Clone();
    }

    private void DataCollectorWindow_FormClosing(object? sender, FormClosingEventArgs e)
    {
        StopCollecting();
        UninstallKeyboardHook();
        StopVmKeyboardReceiver();
        _timer.Dispose();
        _depthTimer.Dispose();
        _lastCapture?.Dispose();
        _lastDepth?.Dispose();
        _depthInference?.Dispose();
    }
}
