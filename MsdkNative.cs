using System.Runtime.InteropServices;

namespace whatlanCar;

public static class MsdkNative
{
    public static IntPtr Handle;

    [DllImport("msdk.dll", EntryPoint = "M_Open", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr M_Open(int port);

    [DllImport("msdk.dll", EntryPoint = "M_Close", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_Close(IntPtr handle);

    [DllImport("msdk.dll", EntryPoint = "M_ResolutionUsed", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_ResolutionUsed(IntPtr handle, int width, int height);

    [DllImport("msdk.dll", EntryPoint = "M_MoveR", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_MoveR(IntPtr handle, int x, int y);

    [DllImport("msdk.dll", EntryPoint = "M_LeftClick", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_LeftClick(IntPtr handle, int count);

    [DllImport("msdk.dll", EntryPoint = "M_LeftDoubleClick", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_LeftDoubleClick(IntPtr handle, int count);

    [DllImport("msdk.dll", EntryPoint = "M_RightClick", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_RightClick(IntPtr handle, int count);

    [DllImport("msdk.dll", EntryPoint = "M_ReleaseAllKey", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_ReleaseAllKey(IntPtr handle);

    [DllImport("msdk.dll", EntryPoint = "M_ReleaseAllMouse", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int M_ReleaseAllMouse(IntPtr handle);
}
