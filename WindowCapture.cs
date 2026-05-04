using OpenCvSharp;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace whatlanCar;

public static class WindowCapture
{
    private const int Srccopy = 0x00CC0020;
    private static IntPtr _cachedScreenDc = IntPtr.Zero;
    private static IntPtr _cachedMemoryDc = IntPtr.Zero;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

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
