using OpenCvSharp;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/*
 * 自动瞄准 基于20小时训练
开始:2025-11-15
训练完成模型测试完成:2025-12-6   (耗时21天,中间有几天没写)

特别说明 分辨率 1080P 缩放100%
游戏设置  显示:
                        显示模式=全屏无边框模式
                        渲染模式=性能
                图像:
                        材质质量
                                全局照明=低
                        贴图
                                 漫反射贴图=低
                                 高光贴图=中
                                 小地图尺寸=1.2
                                 小地图透明度=1

单一目标: 训练yolo11模型 (使用autoLabel.vmp训练数据转换为onnx格式,训练时一定要加model.export(format='onnx', simplify=True)否则无法识别)
双头鼠标: 控制游戏视角和鼠标分别控制,避免使用单一鼠标控制

 坐标:
                        comm.精准移动(622, 344); //开始
                        comm.精准移动(393, 559);//结束
                        comm.精准移动(824, 345);//开始
                        comm.精准移动(1048, 558);//结束

 https://github.com/myaoao/whatlanCar
 */
namespace whatlanCar;

public static class WindowCapture
{
    private const int Srccopy = 0x00CC0020;
    private const uint PwRenderFullContent = 0x00000002;
    private static IntPtr _cachedScreenDc = IntPtr.Zero;
    private static IntPtr _cachedMemoryDc = IntPtr.Zero;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static Mat CaptureWindow(IntPtr windowHandle)
    {
        if (!IsWindow(windowHandle) || !IsWindowVisible(windowHandle))
        {
            throw new InvalidOperationException("窗口句柄无效，或窗口不可见。");
        }

        if (!GetWindowRect(windowHandle, out var rect))
        {
            throw new InvalidOperationException("无法读取窗口位置。");
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("窗口尺寸无效。");
        }

        return CaptureScreenArea(rect.Left, rect.Top, width, height);
    }

    public static Mat CaptureWindowContent(IntPtr windowHandle)
    {
        if (!IsWindow(windowHandle))
        {
            throw new InvalidOperationException("窗口句柄无效。");
        }

        if (!GetWindowRect(windowHandle, out var rect))
        {
            throw new InvalidOperationException("无法读取窗口位置。");
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("窗口尺寸无效。");
        }

        var screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法获取屏幕 DC。");
        }

        var memoryDc = CreateCompatibleDC(screenDc);
        var bitmapHandle = CreateCompatibleBitmap(screenDc, width, height);
        var oldObject = SelectObject(memoryDc, bitmapHandle);

        try
        {
            if (!PrintWindow(windowHandle, memoryDc, PwRenderFullContent))
            {
                throw new InvalidOperationException("窗口内容抓取失败。");
            }

            using var bitmap = Bitmap.FromHbitmap(bitmapHandle);
            return BitmapToMat(bitmap);
        }
        finally
        {
            SelectObject(memoryDc, oldObject);
            DeleteObject(bitmapHandle);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static Mat CaptureScreenArea(int left, int top, int width, int height)
    {
        if (_cachedScreenDc == IntPtr.Zero)
        {
            _cachedScreenDc = GetDC(IntPtr.Zero);
        }

        if (_cachedMemoryDc == IntPtr.Zero)
        {
            _cachedMemoryDc = CreateCompatibleDC(_cachedScreenDc);
        }

        var bitmapHandle = CreateCompatibleBitmap(_cachedScreenDc, width, height);
        var oldObject = SelectObject(_cachedMemoryDc, bitmapHandle);

        var success = BitBlt(_cachedMemoryDc, 0, 0, width, height, _cachedScreenDc, left, top, Srccopy);
        SelectObject(_cachedMemoryDc, oldObject);

        if (!success)
        {
            DeleteObject(bitmapHandle);
            throw new InvalidOperationException("窗口截图失败。");
        }

        using var bitmap = Bitmap.FromHbitmap(bitmapHandle);
        DeleteObject(bitmapHandle);
        return BitmapToMat(bitmap);
    }

    private static Mat BitmapToMat(Bitmap bitmap)
    {
        var mat = new Mat(bitmap.Height, bitmap.Width, MatType.CV_8UC3);
        var data = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            unsafe
            {
                var srcBase = (byte*)data.Scan0;
                var dstBase = (byte*)mat.DataPointer;
                var srcStride = data.Stride;
                var dstStride = (int)mat.Step();
                var rowBytes = bitmap.Width * 3;

                if (srcStride < 0)
                {
                    srcBase += (bitmap.Height - 1) * srcStride;
                }

                for (var y = 0; y < bitmap.Height; y++)
                {
                    var srcRow = srcBase + y * srcStride;
                    var dstRow = dstBase + y * dstStride;
                    Buffer.MemoryCopy(srcRow, dstRow, rowBytes, rowBytes);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return mat;
    }

    public static void Cleanup()
    {
        if (_cachedMemoryDc != IntPtr.Zero)
        {
            DeleteDC(_cachedMemoryDc);
            _cachedMemoryDc = IntPtr.Zero;
        }

        if (_cachedScreenDc != IntPtr.Zero)
        {
            ReleaseDC(IntPtr.Zero, _cachedScreenDc);
            _cachedScreenDc = IntPtr.Zero;
        }
    }
}
