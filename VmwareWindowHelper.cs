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
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    private const uint GaRoot = 2;

    public static IntPtr FindVmwareWindow()
    {
        return FindWindow("VMUIFrame", "");
    }

    public static bool MoveVmwareToOrigin(int width = 1440, int height = 900)
    {
        var handle = FindVmwareWindow();
        if (handle == IntPtr.Zero)
        {
            var embedded = FindEmbeddedMksWindow();
            handle = GetRootWindow(embedded);
        }

        return handle != IntPtr.Zero && MoveWindow(handle, 0, 0, width, height, true);
    }

    public static bool MoveRootWindowToOriginFromEmbeddedMks(IntPtr embeddedHandle, int width = 1440, int height = 900)
    {
        var root = GetRootWindow(embeddedHandle);
        return root != IntPtr.Zero && MoveWindow(root, 0, 0, width, height, true);
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
