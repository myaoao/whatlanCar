using System.Runtime.InteropServices;
using System.Text;

namespace whatlanCar;

public static class VmwareWindowHelper
{
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

    private const uint GaRoot = 2;

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
}
