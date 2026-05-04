namespace whatlanCar;

public sealed class UnifyControlBridge : IDisposable
{
    private const string YoloDebugWindowTitle = "\u81ea\u52a8\u7784\u51c6Vmware";
    private const string YoloDebugWindowTitleFallback = "Vmware";
    private const double ScreenWidth = 1440.0;
    private const double ScreenHeight = 900.0;
    private const double HorizontalFov = 103.0;
    private const double VerticalFov = 70.0;
    private const double UnitAngle = 0.174;

    private dynamic? _unify;
    private bool _unifyLoaded;
    private bool _yoloInitialized;
    private bool _vmwareConnected;
    private bool _dualMouseOpened;
    private int? _vmwarePort;

    public bool IsReady => _unifyLoaded && _yoloInitialized && _vmwareConnected;

    public bool IsYoloInitialized => _unifyLoaded && _yoloInitialized;

    public bool IsVmwareConnected => _vmwareConnected;

    public bool IsDualMouseOpened => _dualMouseOpened;

    /// <summary>
    /// 加载 UNIFY 组件并初始化 YOLO 模型。这里保持和原 VALORANT 项目一致，使用 GPU=true 和 640 输入尺寸。
    /// </summary>
    public string InitializeYolo(string yoloModelPath)
    {
        if (_unifyLoaded && _yoloInitialized)
        {
            return "UNIFY/YOLO 已初始化，跳过重复初始化。";
        }

        NativeDllPath.EnsureDataDirectory();

        var unifyDllPath = Path.Combine(NativeDllPath.DataDirectory, "UNIFY.dll");
        if (!File.Exists(unifyDllPath))
        {
            throw new FileNotFoundException("找不到 UNIFY.dll。", unifyDllPath);
        }

        if (!File.Exists(yoloModelPath))
        {
            throw new FileNotFoundException("找不到 YOLO 模型。", yoloModelPath);
        }

        _unify ??= CreateUnifyComObject();
        _unify.bsLoadDLL(unifyDllPath);
        _unify.bsLogin("1612330", "T35F8HS22A623P1Q");
        _unify.SetPath(NativeDllPath.DataDirectory + Path.DirectorySeparatorChar);
        _unify.LoadAllBitMap();
        _unifyLoaded = true;

        _yoloInitialized = _unify.yoloInit(yoloModelPath, "v10", "", true, 640);
        if (!_yoloInitialized)
        {
            throw new InvalidOperationException($"YOLO 初始化失败：{yoloModelPath}");
        }

        return $"UNIFY 版本：{_unify.bsGetVer()}，可用点数：{_unify.bsGetPoints()}，YOLO 初始化成功(GPU)，模型：{Path.GetFileName(yoloModelPath)}";
    }

    private static dynamic CreateUnifyComObject()
    {
        var unifyType = Type.GetTypeFromProgID("ATLUnify.Unify")
            ?? throw new InvalidOperationException("未找到 ATLUnify.Unify COM 组件，请先注册 UNIFY 组件。");

        return Activator.CreateInstance(unifyType)
            ?? throw new InvalidOperationException("创建 ATLUnify.Unify COM 对象失败。");
    }

    /// <summary>
    /// 打开双头鼠标设备，并设置分辨率，供 FPS 相对角度移动换算使用。
    /// </summary>
    public void OpenDualMouse()
    {
        if (_dualMouseOpened)
        {
            return;
        }

        NativeDllPath.EnsureDataDirectory();

        var msdkPath = Path.Combine(NativeDllPath.DataDirectory, "msdk.dll");
        if (!File.Exists(msdkPath))
        {
            throw new FileNotFoundException("找不到 msdk.dll。", msdkPath);
        }

        MsdkNative.Handle = MsdkNative.M_Open(1);
        if ((long)MsdkNative.Handle == -1)
        {
            throw new InvalidOperationException("打开双头鼠标设备失败，请检查 USB 设备是否已插入。");
        }

        MsdkNative.M_ResolutionUsed(MsdkNative.Handle, (int)ScreenWidth, (int)ScreenHeight);
        _dualMouseOpened = true;
    }

    public void MoveMouseRelative(int x, int y)
    {
        if (!_dualMouseOpened || MsdkNative.Handle == IntPtr.Zero || (long)MsdkNative.Handle == -1)
        {
            throw new InvalidOperationException("双头鼠标尚未打开。");
        }

        MsdkNative.M_MoveR(MsdkNative.Handle, x, y);
    }

    public bool ConnectVmware(int port)
    {
        if (_vmwareConnected && _vmwarePort == port)
        {
            return true;
        }

        if (!_unifyLoaded)
        {
            throw new InvalidOperationException("UNIFY 尚未加载。");
        }

        var unify = _unify ?? throw new InvalidOperationException("UNIFY 尚未加载。");
        _vmwareConnected = unify.bsConnect(port);
        _vmwarePort = _vmwareConnected ? port : null;
        return _vmwareConnected;
    }

    public void OpenYoloDebugWindow()
    {
        EnsureReady();

        if (VmwareWindowHelper.FindWindowByTitle(YoloDebugWindowTitle) != IntPtr.Zero
            || VmwareWindowHelper.FindWindowByTitle(YoloDebugWindowTitleFallback) != IntPtr.Zero)
        {
            return;
        }

        var unify = _unify ?? throw new InvalidOperationException("UNIFY 尚未加载。");
        unify.vwCreateWindow(YoloDebugWindowTitle);
        unify.vwSetWindowSize(1000, 450);
    }

    /// <summary>
    /// 执行一次 YOLO 检测。检测区域和阈值按原 VALORANT 项目的 loadYolo_Click 保持一致。
    /// </summary>
    public bool DetectAndAttackOnce(out string result)
    {
        EnsureReady();

        var detectedX = 0;
        var detectedY = 0;
        var detectedWidth = 0;
        var detectedHeight = 0;
        const float confThreshold = 0.85f;
        const float iouThreshold = 0.45f;

        var unify = _unify ?? throw new InvalidOperationException("UNIFY 尚未加载。");
        var detected = unify.yoloDetect_Parsed(
            220,
            224,
            1220,
            674,
            true,
            1,
            ref detectedX,
            ref detectedY,
            out detectedWidth,
            out detectedHeight,
            0,
            confThreshold,
            iouThreshold);

        if (!detected)
        {
            result = "未找到目标";
            return false;
        }

        if (_dualMouseOpened)
        {
            MoveMouseToFpsTarget(detectedX + 20, detectedY + 20);
            result = $"找到目标 X={detectedX}, Y={detectedY}, W={detectedWidth}, H={detectedHeight}，已攻击";
        }
        else
        {
            result = $"找到目标 X={detectedX}, Y={detectedY}, W={detectedWidth}, H={detectedHeight}，双头鼠标未启用";
        }

        return true;
    }

    private void EnsureReady()
    {
        if (!_unifyLoaded)
        {
            throw new InvalidOperationException("UNIFY 尚未加载，请先点击初始化控制。");
        }

        if (!_yoloInitialized)
        {
            throw new InvalidOperationException("YOLO 尚未初始化成功。");
        }

        if (!_vmwareConnected)
        {
            throw new InvalidOperationException("VMware 尚未连接成功，请检查 VMware 端口。");
        }
    }

    /// <summary>
    /// 将屏幕目标点换算成 FPS 视角移动量，然后点击攻击。
    /// </summary>
    private static void MoveMouseToFpsTarget(int targetX, int targetY, int wait = 5)
    {
        var centerX = ScreenWidth / 2.0;
        var centerY = ScreenHeight / 2.0;
        var dx = targetX - centerX;
        var dy = targetY - centerY;

        var focalX = (ScreenWidth / 2.0) / Math.Tan(HorizontalFov * 0.5 * Math.PI / 180);
        var focalY = (ScreenHeight / 2.0) / Math.Tan(VerticalFov * 0.5 * Math.PI / 180);
        var yawDeg = Math.Atan2(dx, focalX) * 180.0 / Math.PI;
        var pitchDeg = Math.Atan2(dy, focalY) * 180.0 / Math.PI;

        var moveX = AngleToMouseUnit(yawDeg);
        var moveY = AngleToMouseUnit(pitchDeg);
        MsdkNative.M_MoveR(MsdkNative.Handle, moveX, moveY);
        Thread.Sleep(wait);
        MsdkNative.M_LeftClick(MsdkNative.Handle, 1);
    }

    private static int AngleToMouseUnit(double angle)
    {
        return (int)Math.Round(angle / UnitAngle, MidpointRounding.AwayFromZero);
    }

    public void Dispose()
    {
        if (_dualMouseOpened && MsdkNative.Handle != IntPtr.Zero && (long)MsdkNative.Handle != -1)
        {
            try
            {
                MsdkNative.M_ReleaseAllKey(MsdkNative.Handle);
                MsdkNative.M_ReleaseAllMouse(MsdkNative.Handle);
                MsdkNative.M_Close(MsdkNative.Handle);
            }
            catch
            {
            }
        }

        MsdkNative.Handle = IntPtr.Zero;
        _dualMouseOpened = false;
    }
}
