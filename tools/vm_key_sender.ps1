param(
    [string]$HostIp = "",
    [int]$Port = 47891,
    [int]$IntervalMs = 30
)

$ErrorActionPreference = "Stop"

Add-Type -ReferencedAssemblies System.Windows.Forms @"
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public static class KeyStateReader
{
    private static RawMouseWindow rawMouseWindow;
    private static Thread rawMouseThread;
    private static readonly ManualResetEventSlim rawMouseReady = new ManualResetEventSlim(false);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT point);

    public static int Down(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0 ? 1 : 0;
    }

    public static POINT Cursor()
    {
        POINT point;
        GetCursorPos(out point);
        return point;
    }

    public static bool StartRawMouse()
    {
        if (rawMouseThread != null) {
            return rawMouseWindow != null && rawMouseWindow.Registered;
        }

        rawMouseThread = new Thread(() => {
            rawMouseWindow = new RawMouseWindow(rawMouseReady);
            Application.Run(rawMouseWindow);
        });
        rawMouseThread.IsBackground = true;
        rawMouseThread.SetApartmentState(ApartmentState.STA);
        rawMouseThread.Start();
        rawMouseReady.Wait(3000);
        return rawMouseWindow != null && rawMouseWindow.Registered;
    }

    public static POINT DrainRawMouseDelta()
    {
        if (rawMouseWindow == null) {
            return new POINT();
        }

        return rawMouseWindow.DrainDelta();
    }
}

public struct POINT
{
    public int X;
    public int Y;
}

public sealed class RawMouseWindow : Form
{
    private const int WM_INPUT = 0x00FF;
    private const int RID_INPUT = 0x10000003;
    private const int RIM_TYPEMOUSE = 0;
    private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
    private const ushort HID_USAGE_MOUSE = 0x02;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private readonly object sync = new object();
    private int dx;
    private int dy;

    public bool Registered { get; private set; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    public RawMouseWindow(ManualResetEventSlim ready)
    {
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Load += (sender, args) => {
            var devices = new[] {
                new RAWINPUTDEVICE {
                    usUsagePage = HID_USAGE_PAGE_GENERIC,
                    usUsage = HID_USAGE_MOUSE,
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = Handle
                }
            };
            Registered = RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
            ready.Set();
            Hide();
        };
    }

    public POINT DrainDelta()
    {
        lock (sync)
        {
            var point = new POINT { X = dx, Y = dy };
            dx = 0;
            dy = 0;
            return point;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_INPUT)
        {
            uint size = 0;
            GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            if (size > 0)
            {
                var buffer = Marshal.AllocHGlobal((int)size);
                try
                {
                    if (GetRawInputData(m.LParam, RID_INPUT, buffer, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == size)
                    {
                        var header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
                        if (header.dwType == RIM_TYPEMOUSE)
                        {
                            var mousePtr = IntPtr.Add(buffer, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                            var mouse = (RAWMOUSE)Marshal.PtrToStructure(mousePtr, typeof(RAWMOUSE));
                            lock (sync)
                            {
                                dx += mouse.lLastX;
                                dy += mouse.lLastY;
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        base.WndProc(ref m);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public int dwType;
        public int dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWMOUSE
    {
        public ushort usFlags;
        public uint ulButtons;
        public uint ulRawButtons;
        public int lLastX;
        public int lLastY;
        public uint ulExtraInformation;
    }
}
"@

$udp = [System.Net.Sockets.UdpClient]::new()

if ([string]::IsNullOrWhiteSpace($HostIp)) {
    $localIp = Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object {
            $_.IPAddress -notlike "169.254.*" -and
            $_.IPAddress -ne "127.0.0.1" -and
            $_.PrefixOrigin -ne "WellKnown"
        } |
        Select-Object -First 1 -ExpandProperty IPAddress

    if ([string]::IsNullOrWhiteSpace($localIp)) {
        throw "Cannot detect VM IPv4 address. Pass -HostIp manually."
    }

    $parts = $localIp.Split(".")
    $HostIp = "$($parts[0]).$($parts[1]).$($parts[2]).1"
    Write-Host "Detected VM IP: $localIp"
    Write-Host "Guessed VMware host IP: $HostIp"
}

$endpoint = [System.Net.IPEndPoint]::new([System.Net.IPAddress]::Parse($HostIp), $Port)
$encoding = [System.Text.Encoding]::UTF8

Write-Host "Sending VM keyboard state to $HostIp`:$Port every $IntervalMs ms. Press Ctrl+C to stop."
Write-Host "If the host collector VM counter does not increase, check Windows Firewall UDP $Port inbound on the host."
$rawMouseReady = [KeyStateReader]::StartRawMouse()
if ($rawMouseReady) {
    Write-Host "Raw mouse capture is enabled."
} else {
    Write-Host "Raw mouse capture failed; falling back to cursor position deltas."
}

try {
    $lastStatus = [DateTime]::MinValue
    $lastMouse = [KeyStateReader]::Cursor()
    while ($true) {
        $w = [KeyStateReader]::Down(0x57)
        $a = [KeyStateReader]::Down(0x41)
        $s = [KeyStateReader]::Down(0x53)
        $d = [KeyStateReader]::Down(0x44)
        $j = [KeyStateReader]::Down(0x4A)
        $space = [KeyStateReader]::Down(0x20)
        $m = [KeyStateReader]::Down(0x4D)
        $jump = [Math]::Max($j, $space)
        $mouse = [KeyStateReader]::Cursor()
        if ($rawMouseReady) {
            $rawDelta = [KeyStateReader]::DrainRawMouseDelta()
            $mouseDx = $rawDelta.X
            $mouseDy = $rawDelta.Y
            $mouseSource = "raw"
        } else {
            $mouseDx = $mouse.X - $lastMouse.X
            $mouseDy = $mouse.Y - $lastMouse.Y
            $mouseSource = "cursor"
        }
        $lastMouse = $mouse

        $message = "w=$w,a=$a,s=$s,d=$d,jump=$jump,m=$m,mouse_dx=$mouseDx,mouse_dy=$mouseDy,mouse_x=$($mouse.X),mouse_y=$($mouse.Y),mouse_source=$mouseSource"
        $bytes = $encoding.GetBytes($message)
        [void]$udp.Send($bytes, $bytes.Length, $endpoint)
        if ((Get-Date) - $lastStatus -gt [TimeSpan]::FromSeconds(1)) {
            $lastStatus = Get-Date
            Write-Host "sent $message"
        }
        Start-Sleep -Milliseconds $IntervalMs
    }
}
finally {
    $udp.Dispose()
}
