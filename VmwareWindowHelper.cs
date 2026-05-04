using System.Runtime.InteropServices;
using System.Text;

namespace whatlanCar;

public static class VmwareWindowHelper
{
    [Flags]
    private enum WindowStyle : uint
    {
        Child = 0x40000000,
        Caption = 0x00C00000,
        ThickFrame = 0x00040000,
        MinimizeBox = 0x00020000,
        MaximizeBox = 0x00010000,
        SystemMenu = 0x00080000,
        Popup = 0x80000000
    }

    private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    private const uint GaRoot = 2;
    private const int GwlStyle = -16;
    private static readonly IntPtr HwndTop = IntPtr.Zero;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpShowWindow = 0x0040;

    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static IntPtr FindVmwareWindow()
    {
        return FindWindow("VMUIFrame", "");
    }

    public static IntPtr FindWindowByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return IntPtr.Zero;
        }

        var exactMatch = FindWindow(null, title);
        if (exactMatch != IntPtr.Zero)
        {
            return exactMatch;
        }

        var matched = IntPtr.Zero;
        EnumWindows((topLevel, _) =>
        {
            var windowTitle = GetWindowTitle(topLevel);
            if (windowTitle.Contains(title, StringComparison.Ordinal))
            {
                matched = topLevel;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return matched;
    }

    public static bool MoveVmwareToOrigin()
    {
        var handle = FindVmwareWindow();
        if (handle == IntPtr.Zero)
        {
            var embedded = FindEmbeddedMksWindow();
            handle = GetRootWindow(embedded);
        }

        return MoveWindowPreserveSize(handle, 0, 0);
    }

    public static bool MoveRootWindowToOriginFromEmbeddedMks(IntPtr embeddedHandle)
    {
        var root = GetRootWindow(embeddedHandle);
        return MoveWindowPreserveSize(root, 0, 0);
    }

    public static IntPtr GetRootWindow(IntPtr childHandle)
    {
        if (childHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var root = GetAncestor(childHandle, GaRoot);
        return root == IntPtr.Zero ? childHandle : root;
    }

    public static IntPtr FindEmbeddedMksWindow()
    {
        var matched = IntPtr.Zero;

        bool CheckWindow(IntPtr hWnd)
        {
            if (GetWindowClass(hWnd) == "MKSEmbedded" && GetWindowTitle(hWnd) == "MKSWindow#0")
            {
                matched = hWnd;
                return false;
            }

            return true;
        }

        EnumWindows((topLevel, _) =>
        {
            if (!CheckWindow(topLevel))
            {
                return false;
            }

            EnumChildWindows(topLevel, (child, _) => CheckWindow(child), IntPtr.Zero);
            return matched == IntPtr.Zero;
        }, IntPtr.Zero);

        return matched;
    }

    public static bool MoveWindowOffscreen(IntPtr hWnd, int width, int height)
    {
        return hWnd != IntPtr.Zero && MoveWindow(hWnd, -32000, -32000, width, height, true);
    }

    public static bool EmbedWindow(IntPtr childWindow, IntPtr hostWindow, int hostWidth, int hostHeight, int contentWidth, int contentHeight)
    {
        if (childWindow == IntPtr.Zero || hostWindow == IntPtr.Zero || hostWidth <= 0 || hostHeight <= 0)
        {
            return false;
        }

        var styleValue = GetWindowLongPtr(childWindow, GwlStyle).ToInt64();
        var style = (WindowStyle)(ulong)styleValue;
        style &= ~(WindowStyle.Caption | WindowStyle.ThickFrame | WindowStyle.MinimizeBox | WindowStyle.MaximizeBox | WindowStyle.SystemMenu | WindowStyle.Popup);
        style |= WindowStyle.Child;

        if (SetWindowLongPtr(childWindow, GwlStyle, new IntPtr((long)style)) == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
        {
            return false;
        }

        if (SetParent(childWindow, hostWindow) == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
        {
            return false;
        }

        var fitted = FitInto(hostWidth, hostHeight, contentWidth, contentHeight);
        var offsetX = Math.Max(0, (hostWidth - fitted.Width) / 2);
        var offsetY = Math.Max(0, (hostHeight - fitted.Height) / 2);

        return SetWindowPos(
            childWindow,
            HwndTop,
            offsetX,
            offsetY,
            fitted.Width,
            fitted.Height,
            SwpFrameChanged | SwpNoZOrder | SwpShowWindow);
    }

    public static bool ResizeEmbeddedWindow(IntPtr childWindow, int hostWidth, int hostHeight, int contentWidth, int contentHeight)
    {
        if (childWindow == IntPtr.Zero || hostWidth <= 0 || hostHeight <= 0)
        {
            return false;
        }

        var fitted = FitInto(hostWidth, hostHeight, contentWidth, contentHeight);
        var offsetX = Math.Max(0, (hostWidth - fitted.Width) / 2);
        var offsetY = Math.Max(0, (hostHeight - fitted.Height) / 2);

        return SetWindowPos(
            childWindow,
            HwndTop,
            offsetX,
            offsetY,
            fitted.Width,
            fitted.Height,
            SwpNoZOrder | SwpShowWindow);
    }

    private static bool MoveWindowPreserveSize(IntPtr hWnd, int x, int y)
    {
        if (hWnd == IntPtr.Zero || !GetWindowRect(hWnd, out var rect))
        {
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        return width > 0 && height > 0 && MoveWindow(hWnd, x, y, width, height, true);
    }

    private static string GetWindowClass(IntPtr hWnd)
    {
        var buffer = new StringBuilder(256);
        return GetClassName(hWnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : string.Empty;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var buffer = new StringBuilder(256);
        return GetWindowText(hWnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : string.Empty;
    }

    private static (int Width, int Height) FitInto(int hostWidth, int hostHeight, int contentWidth, int contentHeight)
    {
        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return (hostWidth, hostHeight);
        }

        var scale = Math.Min(hostWidth / (double)contentWidth, hostHeight / (double)contentHeight);
        var width = Math.Max(1, (int)Math.Round(contentWidth * scale));
        var height = Math.Max(1, (int)Math.Round(contentHeight * scale));
        return (width, height);
    }
}
