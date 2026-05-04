using System.Runtime.InteropServices;

namespace whatlanCar;

public static class NativeDllPath
{
    private static bool _initialized;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "data");

    public static void EnsureDataDirectory()
    {
        if (_initialized)
        {
            return;
        }

        Directory.CreateDirectory(DataDirectory);
        if (!SetDllDirectory(DataDirectory))
        {
            throw new InvalidOperationException($"无法设置 DLL 搜索目录：{DataDirectory}");
        }

        _initialized = true;
    }
}
