using OpenCvSharp;
using System.Globalization;
using System.Runtime.InteropServices;


/*
自动瞄准，基于 20 小时训练。
开始：2025-11-15
训练完成与模型测试完成：2025-12-06（耗时约 21 天，中间有几天未训练）

特别说明：分辨率 1080P，缩放 100%。
游戏设置：
    显示：
        显示模式 = 全屏无边框模式
        渲染模式 = 性能
    图像：
        材质质量
        全局照明 = 低
        漫反射贴图 = 低
        高光贴图 = 中
        小地图尺寸 = 1.2
        小地图透明度 = 1

单一目标：训练 yolo11 模型。
使用 autoLabel.vmp 训练数据并转换为 onnx 格式，训练时需要执行
model.export(format='onnx', simplify=True)，否则无法识别。

双头鼠标：游戏视角和鼠标指针分别控制，避免使用单一鼠标控制。

坐标示例：
    comm.精准移动(622, 344);  // 开始
    comm.精准移动(393, 559);  // 结束
    comm.精准移动(824, 345);  // 开始
    comm.精准移动(1048, 558); // 结束
 */



namespace whatlanCar;

public partial class MainWindow : Form
{
    private const int WalkabilityHistorySize = 6;
    private const int FullTurnUnits = 2064;
    private const int InitialScanSteps = 8;
    private const int LocalTurnScanSteps = 8;
    private const int LocalTurnMaxUnits = 720;
    private const int PathLoopDelayMs = 260;
    private const int PostTurnSettleMs = 760;
    private const int ScanSettleMs = 420;
    private const int AttackCheckIntervalMs = 300;
    private const int AttackRecoveryDelayMs = 260;
    private const double StopThresholdMargin = 10.0;
    private const int HomeHotKeyId = 101;
    private const int EndHotKeyId = 102;
    private const int WmHotKey = 0x0312;
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int YoloDebugWindowWidth = 1000;
    private const int YoloDebugWindowHeight = 450;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Queue<double> _walkabilityHistory = new();
    private readonly UnifyControlBridge _unifyBridge = new();
    private readonly DevBoardController _devBoard = new();
    private readonly object _depthInferenceLock = new();
    private DepthAnythingInference? _depthInference;
    private CancellationTokenSource? _attackLoopCts;
    private CancellationTokenSource? _pathTestCts;
    private Mat? _lastCapture;
    private Mat? _lastDepth;
    private LowLevelKeyboardProc? _keyboardProc;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private IntPtr _embeddedYoloWindow = IntPtr.Zero;
    private DateTime _lastHookHotKeyTime = DateTime.MinValue;
    private bool _isRunning;
    private bool _pathYoloDebugWindowOpened;
    private bool _attackYoloDebugWindowOpened;
    private const string YoloDebugWindowTitle = "自动瞄准Vmware";

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public MainWindow()
    {
        InitializeComponent();

        _timer = new System.Windows.Forms.Timer { Interval = 100 };
        _timer.Tick += Timer_Tick;
        panelYoloHost.SizeChanged += (_, _) => ResizeEmbeddedYoloWindow();
        btnStart.Text = "检测手动深度推理";

        LoadDepthModel();
        TryAutoFindWindowHandle();
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
            MessageBox.Show($"自动加载模型失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnInitControl_Click(object? sender, EventArgs e)
    {
        try
        {
            btnInitControl.Enabled = false;
            AppendLog("开始初始化 UNIFY、双头鼠标、VMware 和开发板。");

            var unifyInfo = _unifyBridge.InitializeYolo(GetAttackModelPath());
            AppendLog(unifyInfo);

            if (_unifyBridge.IsDualMouseOpened)
            {
                AppendLog("双头鼠标已打开，跳过重复初始化。");
            }
            else
            {
                _unifyBridge.OpenDualMouse();
                AppendLog("双头鼠标打开成功。");
            }

            if (!int.TryParse(txtVmwarePort.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var vmwarePort))
            {
                throw new InvalidOperationException("VMware 端口必须是整数。");
            }

            TryAutoFindWindowHandle();
            var movedVmware = TryParseWindowHandle(txtWindowHandle.Text, out var embeddedHandle)
                ? VmwareWindowHelper.MoveRootWindowToOriginFromEmbeddedMks(embeddedHandle)
                : VmwareWindowHelper.MoveVmwareToOrigin();

            if (movedVmware)
            {
                AppendLog("VMware 窗口已移动到左上角，匹配 YOLO 检测区域。");
            }
            else
            {
                AppendLog("未找到 VMware Workstation 窗口，继续按当前窗口位置检测。");
            }

            if (!_unifyBridge.ConnectVmware(vmwarePort))
            {
                throw new InvalidOperationException($"VMware 连接失败：端口 {vmwarePort}");
            }

            AppendLog($"VMware 连接成功：端口 {vmwarePort}");

            await TryOpenDevBoardAsync(showMessage: true);
        }
        catch (Exception ex)
        {
            AppendLog($"初始化失败：{ex.Message}");
            MessageBox.Show(ex.Message, "初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnInitControl.Enabled = true;
        }
    }

    private void BtnTestAttack_Click(object? sender, EventArgs e)
    {
        if (_attackLoopCts != null)
        {
            StopAttackLoop();
            return;
        }

        try
        {
            if (!File.Exists(GetAttackModelPath()))
            {
                throw new FileNotFoundException("测试攻击需要 YOLO 模型文件。", GetAttackModelPath());
            }

            if (!_unifyBridge.IsReady)
            {
                throw new InvalidOperationException("UNIFY/YOLO/VMware 尚未准备好，请先点击初始化控制并确认 VMware 连接成功。");
            }

            if (!TryOpenYoloDebugWindowForAttack())
            {
                throw new InvalidOperationException("未能打开或嵌入 YOLO 推理窗口。");
            }
            _attackLoopCts = new CancellationTokenSource();
            btnTestAttack.Text = "停止推理攻击";
            AppendLog("YOLO 攻击循环已启动，推理窗口会同步显示在主界面。");

            _ = Task.Run(() => AttackLoop(_attackLoopCts.Token));
        }
        catch (Exception ex)
        {
            AppendLog($"启动攻击循环失败：{ex.Message}");
            MessageBox.Show(ex.Message, "测试攻击失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            StopAttackLoop();
        }
    }

    private void AttackLoop(CancellationToken token)
    {
        var frameCount = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var found = _unifyBridge.DetectAndAttackOnce(out var message);
                sw.Stop();

                frameCount++;
                if (found || frameCount % 30 == 0)
                {
                    AppendLogThreadSafe($"{message}，YOLO耗时 {sw.ElapsedMilliseconds} ms");
                }
            }
            catch (Exception ex)
            {
                AppendLogThreadSafe($"攻击循环错误：{ex.Message}");
                Thread.Sleep(300);
            }
        }
    }

    private void StopAttackLoop()
    {
        _attackLoopCts?.Cancel();
        _attackLoopCts?.Dispose();
        _attackLoopCts = null;
        btnTestAttack.Text = "测试推理攻击";
        AppendLog("YOLO 攻击循环已停止。");
    }

    private async void BtnTestBoard_Click(object? sender, EventArgs e)
    {
        try
        {
            btnTestBoard.Enabled = false;
            AppendLog("开始测试开发板：5 秒后依次短按 W、按住 W 2 秒、长按 D 1 秒。");

            if (!_devBoard.IsOpen)
            {
                if (!await TryOpenDevBoardAsync(showMessage: true))
                {
                    return;
                }
            }

            await Task.Delay(5000);
            await Task.Run(() =>
            {
                _devBoard.KeyTap('w');
                _devBoard.KeyTapJump();
                _devBoard.KeyDown('w');
                Thread.Sleep(2000);
                _devBoard.KeyUp('w');
                _devBoard.KeyHoldJump(1000);
                _devBoard.KeyHold('d', 1000);
            });

            AppendLog("开发板测试完成。");
        }
        catch (Exception ex)
        {
            AppendLog($"开发板测试失败：{ex.Message}");
            MessageBox.Show(ex.Message, "开发板测试失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (_devBoard.IsOpen)
            {
                _devBoard.ReleaseAll();
            }

            btnTestBoard.Enabled = true;
        }
    }

    private async void BtnTestPath_Click(object? sender, EventArgs e)
    {
        if (_pathTestCts != null)
        {
            StopPathTest();
            return;
        }

        try
        {
            if (_depthInference == null)
            {
                throw new InvalidOperationException("深度模型尚未加载成功。");
            }

            if (!TryParseWindowHandle(txtWindowHandle.Text, out _))
            {
                throw new InvalidOperationException("请输入有效的游戏窗口句柄。");
            }

            if (!TryGetPassThreshold(out _) || !TryGetDarkThreshold(out _) || !TryGetPathForwardThreshold(out _) || !TryGetPathRotateThreshold(out _))
            {
                throw new InvalidOperationException("障碍阈值、前方暗区阈值和寻路阈值必须是 0 到 100 之间的数字。");
            }

            if (!_devBoard.IsOpen)
            {
                if (!await TryOpenDevBoardAsync(showMessage: true))
                {
                    StopPathTest();
                    return;
                }
            }

            if (!_unifyBridge.IsDualMouseOpened)
            {
                _unifyBridge.OpenDualMouse();
                AppendLog("双头鼠标打开成功。");
            }

            if (!TryOpenYoloDebugWindowForPath())
            {
                throw new InvalidOperationException("未能打开或嵌入 YOLO 推理窗口，无法启动寻路测试。");
            }

            _pathTestCts = new CancellationTokenSource();
            btnTestPath.Text = "停止寻路测试";
            AppendLog("寻路测试将在 5 秒后开始，请把鼠标移动到游戏窗口内部。");

            await Task.Delay(5000, _pathTestCts.Token);
            AppendLog("寻路测试已开始：先 360 度扫描最顺方向，然后前进；卡住后重新扫描。");
            _ = Task.Run(() => PathTestLoop(_pathTestCts.Token));
        }
        catch (OperationCanceledException)
        {
            StopPathTest();
        }
        catch (Exception ex)
        {
            AppendLog($"寻路测试启动失败：{ex.Message}");
            MessageBox.Show(ex.Message, "寻路测试失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            StopPathTest();
        }
    }

    private void PathTestLoop(CancellationToken token)
    {
        var holdingForward = false;
        var lastForwardObstaclePercent = double.NaN;
        var stableForwardFrames = 0;
        var lastJumpTime = DateTime.MinValue;
        var jumpRetryCount = 0;
        var needsDirectionScan = true;
        var lastAttackCheckTime = DateTime.MinValue;

        while (!token.IsCancellationRequested)
        {
            try
            {
                var directForwardObstacleThreshold = InvokeUi(() => TryGetPathForwardThreshold(out var value) ? value : 45.0);
                var rotateObstacleThreshold = InvokeUi(() => TryGetPathRotateThreshold(out var value) ? value : 55.0);

                rotateObstacleThreshold = Math.Max(rotateObstacleThreshold, directForwardObstacleThreshold);

                if (TryHandlePathAttack(ref holdingForward, ref lastAttackCheckTime, token))
                {
                    Thread.Sleep(AttackRecoveryDelayMs);
                    continue;
                }

                if (needsDirectionScan)
                {
                    if (holdingForward)
                    {
                        _devBoard.KeyUp('w');
                        holdingForward = false;
                    }

                    _devBoard.ReleaseAll();
                    var bestDirection = ScanBestDirection(token);
                    if (bestDirection == null)
                    {
                        break;
                    }

                    var bestTurnUnits = NormalizeTurnOffset(bestDirection.Value.MouseUnitsFromScanStart);
                    SmoothTurnMouse(bestTurnUnits, token, steps: 18, stepDelayMs: 14);
                    AppendLogThreadSafe($"360度扫描完成：选择最低障碍方向 {bestTurnUnits}，中心障碍 {bestDirection.Value.CenterPathObstaclePercent:F1}%，全图障碍 {bestDirection.Value.ObstaclePercent:F1}%，前方暗区 {bestDirection.Value.FocusDarkPercent:F1}%");
                    stableForwardFrames = 0;
                    lastForwardObstaclePercent = double.NaN;
                    jumpRetryCount = 0;
                    needsDirectionScan = false;
                }

                var sample = EvaluateCurrentDirection();
                var canMoveForward = IsPathPassable(sample, directForwardObstacleThreshold);
                var stopObstacleThreshold = Math.Min(100.0, rotateObstacleThreshold + StopThresholdMargin);
                var shouldStopForward = sample.CenterPathObstaclePercent > stopObstacleThreshold
                    || sample.SceneStats.LikelyInvalidPassScene;

                if (canMoveForward || !shouldStopForward)
                {
                    if (!holdingForward)
                    {
                        _devBoard.KeyDown('w');
                        holdingForward = true;
                    }

                    if (!double.IsNaN(lastForwardObstaclePercent)
                        && Math.Abs(sample.CenterPathObstaclePercent - lastForwardObstaclePercent) <= 0.8)
                    {
                        stableForwardFrames++;
                    }
                    else
                    {
                        stableForwardFrames = 0;
                    }

                    lastForwardObstaclePercent = sample.CenterPathObstaclePercent;

                    if (stableForwardFrames >= 4 && DateTime.Now - lastJumpTime > TimeSpan.FromSeconds(1.0))
                    {
                        _devBoard.KeyTapJump();
                        lastJumpTime = DateTime.Now;
                        stableForwardFrames = 0;
                        jumpRetryCount++;
                        BeginInvoke(() => lblStatus.Text = $"疑似卡住，已跳跃（中心障碍 {sample.CenterPathObstaclePercent:F1}%）");
                        AppendLogThreadSafe($"寻路疑似卡住，已按跳跃：中心障碍 {sample.CenterPathObstaclePercent:F1}%");

                        if (jumpRetryCount >= 3)
                        {
                            AppendLogThreadSafe("寻路在同一方向多次疑似卡住，准备重新 360 度扫描。");
                            needsDirectionScan = true;
                        }
                    }

                    Thread.Sleep(PathLoopDelayMs);
                    continue;
                }

                stableForwardFrames = 0;
                lastForwardObstaclePercent = double.NaN;

                if (holdingForward)
                {
                    _devBoard.KeyUp('w');
                    holdingForward = false;
                }

                _devBoard.ReleaseAll();
                AppendLogThreadSafe($"前方超过停止阈值，左右找路：中心障碍 {sample.CenterPathObstaclePercent:F1}% > {stopObstacleThreshold:F1}%，前方暗区 {sample.SceneStats.FocusDarkPercent:F1}%");
                var turnSample = ScanBestLocalTurn(directForwardObstacleThreshold, token);
                if (turnSample == null)
                {
                    needsDirectionScan = true;
                    continue;
                }

                SmoothTurnMouse(turnSample.Value.MouseUnitsFromScanStart, token, steps: 8, stepDelayMs: 14);
                AppendLogThreadSafe($"局部转向完成：方向 {turnSample.Value.MouseUnitsFromScanStart}，中心障碍 {turnSample.Value.CenterPathObstaclePercent:F1}%，全图障碍 {turnSample.Value.ObstaclePercent:F1}%");
                Thread.Sleep(PostTurnSettleMs);
            }
            catch (Exception ex)
            {
                if (holdingForward)
                {
                    _devBoard.KeyUp('w');
                    holdingForward = false;
                }

                AppendLogThreadSafe($"寻路测试错误：{ex.Message}");
                Thread.Sleep(300);
            }
        }

        if (holdingForward)
        {
            _devBoard.KeyUp('w');
        }
    }

    private bool TryHandlePathAttack(ref bool holdingForward, ref DateTime lastAttackCheckTime, CancellationToken token)
    {
        if (!_unifyBridge.IsReady || token.IsCancellationRequested)
        {
            return false;
        }

        if (DateTime.Now - lastAttackCheckTime < TimeSpan.FromMilliseconds(AttackCheckIntervalMs))
        {
            return false;
        }

        lastAttackCheckTime = DateTime.Now;
        if (!_unifyBridge.DetectAndAttackOnce(out var message))
        {
            return false;
        }

        if (holdingForward)
        {
            _devBoard.KeyUp('w');
            holdingForward = false;
        }

        _devBoard.ReleaseAll();
        AppendLogThreadSafe($"寻路中发现目标，暂停移动并攻击：{message}");
        BeginInvoke(() => lblStatus.Text = "发现目标，攻击中");
        return true;
    }

    private PathDirectionSample? ScanBestLocalTurn(double passableThreshold, CancellationToken token)
    {
        var stepUnits = LocalTurnMaxUnits / LocalTurnScanSteps;
        var currentOffset = 0;
        var bestSample = EvaluateCurrentDirection(0, updatePreview: true);

        for (var i = 1; i <= LocalTurnScanSteps && !token.IsCancellationRequested; i++)
        {
            var rightOffset = i * stepUnits;
            SmoothTurnMouse(rightOffset - currentOffset, token, steps: 6, stepDelayMs: 22);
            currentOffset = rightOffset;
            Thread.Sleep(ScanSettleMs);
            var rightSample = EvaluateCurrentDirection(rightOffset, updatePreview: true);
            if (rightSample.Score < bestSample.Score)
            {
                bestSample = rightSample;
            }

            if (IsPathPassable(rightSample, passableThreshold))
            {
                SmoothTurnMouse(-currentOffset, token, steps: 7, stepDelayMs: 20);
                return rightSample;
            }

            var leftOffset = -i * stepUnits;
            SmoothTurnMouse(leftOffset - currentOffset, token, steps: 10, stepDelayMs: 22);
            currentOffset = leftOffset;
            Thread.Sleep(ScanSettleMs);
            var leftSample = EvaluateCurrentDirection(leftOffset, updatePreview: true);
            if (leftSample.Score < bestSample.Score)
            {
                bestSample = leftSample;
            }

            if (IsPathPassable(leftSample, passableThreshold))
            {
                SmoothTurnMouse(-currentOffset, token, steps: 7, stepDelayMs: 20);
                return leftSample;
            }
        }

        SmoothTurnMouse(-currentOffset, token, steps: 10, stepDelayMs: 20);
        AppendLogThreadSafe($"左右局部扫描未达到可通行阈值，选择最低障碍方向：{bestSample.MouseUnitsFromScanStart}，中心障碍 {bestSample.CenterPathObstaclePercent:F1}%");
        return bestSample.CenterPathObstaclePercent <= Math.Min(100.0, passableThreshold + StopThresholdMargin)
            ? bestSample
            : null;
    }

    private static bool IsPathPassable(PathDirectionSample sample, double passableThreshold)
    {
        return sample.CenterPathObstaclePercent <= passableThreshold
            && !sample.SceneStats.LikelyInvalidPassScene;
    }

    private static int NormalizeTurnOffset(int mouseUnits)
    {
        var halfTurnUnits = FullTurnUnits / 2;
        while (mouseUnits > halfTurnUnits)
        {
            mouseUnits -= FullTurnUnits;
        }

        while (mouseUnits < -halfTurnUnits)
        {
            mouseUnits += FullTurnUnits;
        }

        return mouseUnits;
    }

    private PathDirectionSample? ScanBestDirection(CancellationToken token)
    {
        var stepUnits = FullTurnUnits / InitialScanSteps;
        var bestSample = EvaluateCurrentDirection(0, updatePreview: true);

        AppendLogThreadSafe("开始 360 度扫描，寻找最顺方向。");

        for (var i = 0; i < InitialScanSteps && !token.IsCancellationRequested; i++)
        {
            SmoothTurnMouse(stepUnits, token, steps: 5, stepDelayMs: 16);
            Thread.Sleep(ScanSettleMs);

            var sample = EvaluateCurrentDirection((i + 1) * stepUnits, updatePreview: true);
            if (sample.Score < bestSample.Score)
            {
                bestSample = sample;
            }
        }

        return token.IsCancellationRequested ? null : bestSample;
    }

    private PathDirectionSample EvaluateCurrentDirection(int mouseUnitsFromScanStart = 0, bool updatePreview = true)
    {
        var windowHandleText = InvokeUi(() => txtWindowHandle.Text);
        if (!TryParseWindowHandle(windowHandleText, out var handle))
        {
            throw new InvalidOperationException("窗口句柄无效。");
        }

        var captureWatch = System.Diagnostics.Stopwatch.StartNew();
        using var rawCapture = WindowCapture.CaptureWindow(handle);
        using var captured = CropCenterSquare(rawCapture);
        captureWatch.Stop();

        using var depthMap = PredictDepthThreadSafe(captured, out var inferenceMs);

        var obstaclePercent = DepthAnythingInference.CalculateObstaclePercent(depthMap);
        var centerPathObstaclePercent = DepthAnythingInference.CalculateCenterPathObstaclePercent(depthMap);
        var darkThreshold = InvokeUi(GetDarkThresholdOrDefault);
        var sceneStats = DepthAnythingInference.AnalyzeScene(depthMap, darkThreshold);
        var score = centerPathObstaclePercent * 2.0
            + obstaclePercent * 0.2
            + (sceneStats.LikelyInvalidPassScene ? 45.0 : 0.0)
            + Math.Max(0.0, sceneStats.FocusDarkPercent - darkThreshold) * 0.35;

        if (updatePreview)
        {
            using var depthPreview = DepthAnythingInference.CreateVisualization(depthMap);
            UpdatePathPreview(captured, depthPreview, obstaclePercent, sceneStats, captureWatch.ElapsedMilliseconds, inferenceMs);
        }

        return new PathDirectionSample(mouseUnitsFromScanStart, obstaclePercent, centerPathObstaclePercent, sceneStats.FocusDarkPercent, score, sceneStats, captureWatch.ElapsedMilliseconds, inferenceMs);
    }

    private void SmoothTurnMouse(int totalX, CancellationToken token, int steps = 7, int stepDelayMs = 16)
    {
        var moved = 0;
        for (var i = 0; i < steps && !token.IsCancellationRequested; i++)
        {
            var target = (int)Math.Round(totalX * (i + 1) / (double)steps, MidpointRounding.AwayFromZero);
            var delta = target - moved;
            moved += delta;

            _unifyBridge.MoveMouseRelative(delta, 0);
            Thread.Sleep(stepDelayMs);
        }
    }

    private void UpdatePathPreview(Mat capture, Mat depthPreview, double obstaclePercent, DepthSceneStats sceneStats, long captureMs, long inferenceMs)
    {
        var captureForUi = capture.Clone();
        var depthForUi = depthPreview.Clone();

        BeginInvoke(() =>
        {
            _lastCapture?.Dispose();
            _lastCapture = captureForUi;
            ShowMat(_lastCapture, pictureBoxCapture);

            using (depthForUi)
            {
                ShowMat(depthForUi, pictureBoxDepth);
            }

            UpdatePassStatus(obstaclePercent, sceneStats);
            lblCaptureTime.Text = $"截图时间：{captureMs} ms";
            lblInferenceTime.Text = $"推理时间：{inferenceMs} ms（寻路）";
            lblStatus.Text = "寻路测试中";
        });
    }

    private void StopPathTest()
    {
        _pathTestCts?.Cancel();
        _pathTestCts?.Dispose();
        _pathTestCts = null;

        if (_devBoard.IsOpen)
        {
            _devBoard.ReleaseAll();
        }

        btnTestPath.Text = "寻路测试";
        AppendLog("寻路测试已停止。");
    }

    private bool TryOpenYoloDebugWindowForPath()
    {
        if (_pathYoloDebugWindowOpened)
        {
            return EnsureYoloDebugWindowEmbedded();
        }

        if (!_unifyBridge.IsReady)
        {
            AppendLog("UNIFY/YOLO/VMware 尚未完全初始化，寻路中暂不启用推理攻击窗口。");
            return false;
        }

        try
        {
            _pathYoloDebugWindowOpened = EnsureYoloDebugWindowEmbedded();
            AppendLog("YOLO 推理窗口已打开，寻路时会发现目标并暂停移动攻击。");
            return _pathYoloDebugWindowOpened;
        }
        catch (Exception ex)
        {
            AppendLog($"打开 YOLO 推理窗口失败，寻路仍会继续：{ex.Message}");
            return false;
        }
    }

    private bool TryOpenYoloDebugWindowForAttack()
    {
        if (_attackYoloDebugWindowOpened)
        {
            return EnsureYoloDebugWindowEmbedded();
        }

        _attackYoloDebugWindowOpened = EnsureYoloDebugWindowEmbedded();
        return _attackYoloDebugWindowOpened;
    }

    private bool EnsureYoloDebugWindowEmbedded()
    {
        if (TryEmbedYoloDebugWindow())
        {
            return true;
        }

        _unifyBridge.OpenYoloDebugWindow();
        return TryEmbedYoloDebugWindow();
    }

    private bool TryEmbedYoloDebugWindow()
    {
        if (_embeddedYoloWindow != IntPtr.Zero && VmwareWindowHelper.IsValidWindow(_embeddedYoloWindow))
        {
            ResizeEmbeddedYoloWindow();
            return true;
        }

        _embeddedYoloWindow = IntPtr.Zero;
        for (var i = 0; i < 20; i++)
        {
            var yoloWindow = VmwareWindowHelper.FindWindowByTitle(YoloDebugWindowTitle);
            if (yoloWindow != IntPtr.Zero
                && VmwareWindowHelper.EmbedWindow(
                    yoloWindow,
                    panelYoloHost.Handle,
                    panelYoloHost.ClientSize.Width,
                    panelYoloHost.ClientSize.Height,
                    YoloDebugWindowWidth,
                    YoloDebugWindowHeight))
            {
                _embeddedYoloWindow = yoloWindow;
                lblStatus.Text = "YOLO窗口已嵌入";
                return true;
            }

            Application.DoEvents();
            Thread.Sleep(180);
        }

        AppendLog("未找到 YOLO 推理窗口，暂时无法嵌入到主界面。");
        return false;
    }

    private void ResizeEmbeddedYoloWindow()
    {
        if (_embeddedYoloWindow == IntPtr.Zero)
        {
            return;
        }

        VmwareWindowHelper.ResizeEmbeddedWindow(
            _embeddedYoloWindow,
            panelYoloHost.ClientSize.Width,
            panelYoloHost.ClientSize.Height,
            YoloDebugWindowWidth,
            YoloDebugWindowHeight);
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_isRunning)
        {
            StopCapture();
            lblStatus.Text = "已停止";
            return;
        }

        if (_depthInference == null)
        {
            MessageBox.Show("模型尚未加载成功。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryParseWindowHandle(txtWindowHandle.Text, out _))
        {
            MessageBox.Show("请输入有效的窗口句柄，支持十进制或 0x 开头的十六进制。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryGetPassThreshold(out _))
        {
            MessageBox.Show("请输入 0 到 100 之间的障碍比例阈值。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryGetDarkThreshold(out _))
        {
            MessageBox.Show("请输入 0 到 100 之间的前方暗区阈值。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _isRunning = true;
        _walkabilityHistory.Clear();
        btnStart.Text = "停止手动深度推理";
        lblStatus.Text = "运行中";
        _timer.Start();
    }

    private void BtnFindWindowHandle_Click(object? sender, EventArgs e)
    {
        if (!TryAutoFindWindowHandle())
        {
            MessageBox.Show("未找到 VMware 画面窗口：类名 MKSEmbedded，窗口名 MKSWindow#0。", "获取窗口句柄", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool TryAutoFindWindowHandle()
    {
        var handle = VmwareWindowHelper.FindEmbeddedMksWindow();
        if (handle == IntPtr.Zero)
        {
            AppendLog("未找到窗口：类名 MKSEmbedded，窗口名 MKSWindow#0。");
            return false;
        }

        txtWindowHandle.Text = handle.ToInt64().ToString(CultureInfo.InvariantCulture);
        AppendLog($"已获取窗口句柄：{txtWindowHandle.Text}");
        return true;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_isRunning || _depthInference == null)
        {
            return;
        }

        if (!TryParseWindowHandle(txtWindowHandle.Text, out var handle))
        {
            StopCapture();
            lblStatus.Text = "句柄无效，已停止";
            return;
        }

        try
        {
            var captureWatch = System.Diagnostics.Stopwatch.StartNew();
            using var rawCapture = WindowCapture.CaptureWindow(handle);
            using var captured = CropCenterSquare(rawCapture);
            captureWatch.Stop();

            _lastCapture?.Dispose();
            _lastCapture = captured.Clone();
            ShowMat(_lastCapture, pictureBoxCapture);

            _lastDepth?.Dispose();
            _lastDepth = PredictDepthThreadSafe(_lastCapture, out var inferenceMs);

            using var depthPreview = DepthAnythingInference.CreateVisualization(_lastDepth);
            ShowMat(depthPreview, pictureBoxDepth);

            var obstaclePercent = GetSmoothedObstaclePercent(_lastDepth);
            var sceneStats = DepthAnythingInference.AnalyzeScene(_lastDepth, GetDarkThresholdOrDefault());
            UpdatePassStatus(obstaclePercent, sceneStats);

            lblCaptureTime.Text = $"截图时间：{captureWatch.ElapsedMilliseconds} ms";
            lblInferenceTime.Text = $"推理时间：{inferenceMs} ms（平均 {_depthInference.AverageInferenceMs:F1} ms，{_depthInference.ExecutionProvider}）";
            lblStatus.Text = "运行中";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"运行错误：{ex.Message}";
        }
    }

    private double GetSmoothedObstaclePercent(Mat depthMap)
    {
        var obstaclePercent = DepthAnythingInference.CalculateObstaclePercent(depthMap);
        var walkability = 1.0 - obstaclePercent / 100.0;

        _walkabilityHistory.Enqueue(walkability);
        while (_walkabilityHistory.Count > WalkabilityHistorySize)
        {
            _walkabilityHistory.Dequeue();
        }

        var smoothedWalkability = _walkabilityHistory.Average();
        return (1.0 - smoothedWalkability) * 100.0;
    }

    private void UpdatePassStatus(double obstaclePercent, DepthSceneStats sceneStats)
    {
        if (!TryGetPassThreshold(out var passThreshold))
        {
            lblPassStatus.Text = $"阈值无效（障碍 {obstaclePercent:F1}%）";
            lblPassStatus.ForeColor = Color.Gray;
            return;
        }

        if (obstaclePercent > passThreshold)
        {
            lblPassStatus.Text = $"无法通过（障碍 {obstaclePercent:F1}% > 阈值 {passThreshold:F1}%）";
            lblPassStatus.ForeColor = Color.Red;
        }
        else if (sceneStats.LikelyInvalidPassScene)
        {
            lblPassStatus.Text = $"可能无法通过（暗区 {sceneStats.FocusDarkPercent:F1}% >= 阈值 {GetDarkThresholdOrDefault():F1}%）";
            lblPassStatus.ForeColor = Color.DarkOrange;
        }
        else if (obstaclePercent >= passThreshold - 10.0)
        {
            lblPassStatus.Text = $"谨慎通过（障碍 {obstaclePercent:F1}% 接近阈值 {passThreshold:F1}%）";
            lblPassStatus.ForeColor = Color.DarkOrange;
        }
        else
        {
            lblPassStatus.Text = $"可以通过（障碍 {obstaclePercent:F1}% <= 阈值 {passThreshold:F1}%）";
            lblPassStatus.ForeColor = Color.Green;
        }
    }

    private bool TryGetPassThreshold(out double threshold)
    {
        return double.TryParse(txtPassThreshold.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)
            && threshold >= 0
            && threshold <= 100;
    }

    private bool TryGetDarkThreshold(out double threshold)
    {
        return double.TryParse(txtDarkThreshold.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)
            && threshold >= 0
            && threshold <= 100;
    }

    private bool TryGetPathForwardThreshold(out double threshold)
    {
        return double.TryParse(txtPathForwardThreshold.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)
            && threshold >= 0
            && threshold <= 100;
    }

    private bool TryGetPathRotateThreshold(out double threshold)
    {
        return double.TryParse(txtPathRotateThreshold.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)
            && threshold >= 0
            && threshold <= 100;
    }

    private double GetDarkThresholdOrDefault()
    {
        return TryGetDarkThreshold(out var threshold) ? threshold : 78.0;
    }

    private static bool TryParseWindowHandle(string text, out IntPtr handle)
    {
        text = text.Trim();
        var style = NumberStyles.Integer;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
            style = NumberStyles.HexNumber;
        }

        if (long.TryParse(text, style, CultureInfo.InvariantCulture, out var value) && value != 0)
        {
            handle = new IntPtr(value);
            return true;
        }

        handle = IntPtr.Zero;
        return false;
    }

    private static string NormalizeComPort(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("COM 口不能为空。");
        }

        return text.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
            ? text.ToUpperInvariant()
            : "COM" + text;
    }

    private async Task<bool> TryOpenDevBoardAsync(bool showMessage)
    {
        var comPort = NormalizeComPort(txtComPort.Text);
        if (_devBoard.IsOpen)
        {
            AppendLog($"开发板串口 {comPort} 已打开，跳过重复初始化。");
            return true;
        }

        try
        {
            var result = await Task.Run(() => _devBoard.TryOpen(comPort, out var openMessage)
                ? (Success: true, Message: openMessage)
                : (Success: false, Message: openMessage));

            if (result.Success)
            {
                AppendLog(result.Message);
                return true;
            }

            var message = $"{result.Message}。请插上开发板或修改 COM 口后，再次点击“初始化控制”。";
            AppendLog(message);
            if (showMessage)
            {
                MessageBox.Show(message, "开发板未连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return false;
        }
        catch (Exception ex)
        {
            AppendLog($"打开开发板串口失败：{ex.Message}");
            if (showMessage)
            {
                MessageBox.Show(ex.Message, "开发板串口错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }
    }

    private static Mat CropCenterSquare(Mat source)
    {
        var squareSize = Math.Min(source.Width, source.Height);
        var cropX = (source.Width - squareSize) / 2;
        var cropY = (source.Height - squareSize) / 2;
        var roi = new Rect(cropX, cropY, squareSize, squareSize);
        return new Mat(source, roi).Clone();
    }

    private Mat PredictDepthThreadSafe(Mat image, out long inferenceMs)
    {
        lock (_depthInferenceLock)
        {
            return _depthInference!.PredictDepth(image, out inferenceMs);
        }
    }

    private T InvokeUi<T>(Func<T> action)
    {
        return InvokeRequired ? (T)Invoke(action) : action();
    }

    private static string GetAttackModelPath()
    {
        return Path.Combine(NativeDllPath.DataDirectory, "whatlan.onnx");
    }

    private void AppendLog(string message)
    {
        lstControlLog.Items.Add($"{DateTime.Now:T} {message}");
        lstControlLog.TopIndex = Math.Max(0, lstControlLog.Items.Count - 1);
    }

    private void AppendLogThreadSafe(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        AppendLog(message);
    }

    private void StopCapture()
    {
        _isRunning = false;
        _timer.Stop();
        btnStart.Text = "检测手动深度推理";
    }

    private static void ShowMat(Mat mat, PictureBox pictureBox)
    {
        using var displayMat = new Mat();
        Cv2.Resize(mat, displayMat, new OpenCvSharp.Size(pictureBox.ClientSize.Width, pictureBox.ClientSize.Height), interpolation: InterpolationFlags.Area);

        var image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(displayMat);
        var previous = pictureBox.Image;
        pictureBox.Image = image;
        previous?.Dispose();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!RegisterHotKey(Handle, HomeHotKeyId, 0, (uint)Keys.Home))
        {
            AppendLog("Home 全局热键注册失败，启用键盘钩子兜底。");
        }

        if (!RegisterHotKey(Handle, EndHotKeyId, 0, (uint)Keys.End))
        {
            AppendLog("End 全局热键注册失败，启用键盘钩子兜底。");
        }

        InstallKeyboardHook();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UninstallKeyboardHook();
        UnregisterHotKey(Handle, HomeHotKeyId);
        UnregisterHotKey(Handle, EndHotKeyId);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotKey)
        {
            var id = m.WParam.ToInt32();
            if (id == HomeHotKeyId)
            {
                TriggerPathStartHotKey();
                return;
            }

            if (id == EndHotKeyId)
            {
                TriggerPathStopHotKey();
                return;
            }
        }

        base.WndProc(ref m);
    }

    private void InstallKeyboardHook()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            return;
        }

        _keyboardProc = LowLevelKeyboardHookCallback;
        var moduleName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.ModuleName;
        var moduleHandle = GetModuleHandle(moduleName);
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, moduleHandle, 0);
        if (_keyboardHook == IntPtr.Zero)
        {
            AppendLog("键盘钩子安装失败，Home/End 可能只能在本程序内响应。");
        }
        else
        {
            AppendLog("键盘钩子已安装，Home/End 可全局控制寻路。");
        }
    }

    private void UninstallKeyboardHook()
    {
        if (_keyboardHook == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_keyboardHook);
        _keyboardHook = IntPtr.Zero;
        _keyboardProc = null;
    }

    private IntPtr LowLevelKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        var message = wParam.ToInt32();
        if (nCode >= 0 && (message == WmKeyDown || message == WmSysKeyDown))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == (int)Keys.Home)
            {
                TriggerPathStartHotKey();
            }
            else if (vkCode == (int)Keys.End)
            {
                TriggerPathStopHotKey();
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private void TriggerPathStartHotKey()
    {
        if (DateTime.Now - _lastHookHotKeyTime < TimeSpan.FromMilliseconds(250))
        {
            return;
        }

        _lastHookHotKeyTime = DateTime.Now;
        if (_pathTestCts == null)
        {
            BeginInvoke(() => BtnTestPath_Click(this, EventArgs.Empty));
        }
    }

    private void TriggerPathStopHotKey()
    {
        if (DateTime.Now - _lastHookHotKeyTime < TimeSpan.FromMilliseconds(250))
        {
            return;
        }

        _lastHookHotKeyTime = DateTime.Now;
        if (_pathTestCts != null)
        {
            BeginInvoke((Action)StopPathTest);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopAttackLoop();
        StopPathTest();
        StopCapture();
        _embeddedYoloWindow = IntPtr.Zero;
        pictureBoxCapture.Image?.Dispose();
        pictureBoxDepth.Image?.Dispose();
        _lastCapture?.Dispose();
        _lastDepth?.Dispose();
        _depthInference?.Dispose();
        _devBoard.Dispose();
        _unifyBridge.Dispose();
        WindowCapture.Cleanup();
        base.OnFormClosing(e);
    }

    private readonly record struct PathDirectionSample(
        int MouseUnitsFromScanStart,
        double ObstaclePercent,
        double CenterPathObstaclePercent,
        double FocusDarkPercent,
        double Score,
        DepthSceneStats SceneStats,
        long CaptureMs,
        long InferenceMs);
}

