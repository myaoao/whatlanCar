using System.IO.Ports;

namespace whatlanCar;

public sealed class DevBoardController : IDisposable
{
    public const char JumpKey = 'j';

    private SerialPort? _serialPort;

    public bool IsOpen => _serialPort?.IsOpen == true;

    public bool TryOpen(string comPort, out string message, int baudRate = 115200)
    {
        var availablePorts = SerialPort.GetPortNames();
        if (!availablePorts.Any(port => string.Equals(port, comPort, StringComparison.OrdinalIgnoreCase)))
        {
            message = $"未找到开发板串口 {comPort}。当前可用串口：{FormatAvailablePorts(availablePorts)}";
            return false;
        }

        try
        {
            Open(comPort, baudRate);
            message = $"开发板串口 {comPort} 打开成功。";
            return true;
        }
        catch (Exception ex) when (ex is IOException
            || ex is UnauthorizedAccessException
            || ex is ArgumentException
            || ex is InvalidOperationException)
        {
            message = $"无法打开开发板串口 {comPort}：{ex.Message}";
            return false;
        }
    }

    public void Open(string comPort, int baudRate = 115200)
    {
        var availablePorts = SerialPort.GetPortNames();
        if (!availablePorts.Any(port => string.Equals(port, comPort, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"未找到开发板串口 {comPort}。当前可用串口：{FormatAvailablePorts(availablePorts)}");
        }

        if (_serialPort?.IsOpen == true
            && string.Equals(_serialPort.PortName, comPort, StringComparison.OrdinalIgnoreCase)
            && _serialPort.BaudRate == baudRate)
        {
            return;
        }

        Close();

        _serialPort = new SerialPort(comPort, baudRate)
        {
            NewLine = "\n",
            DtrEnable = true,
            RtsEnable = true,
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };

        _serialPort.Open();
        Thread.Sleep(1500);
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
    }

    public void MoveForwardTap()
    {
        KeyTap('w');
    }

    public void PreciseMove(int x, int y)
    {
        Write($"*{x},{y}\n");
    }

    public void KeyDown(char key)
    {
        Write($"^{key}");
    }

    public void KeyUp(char key)
    {
        Write($"_{key}");
    }

    public void KeyTap(char key, int pressMs = 50)
    {
        KeyDown(key);
        Thread.Sleep(pressMs);
        KeyUp(key);
    }

    public void KeyHold(char key, int holdMs)
    {
        KeyDown(key);
        Thread.Sleep(holdMs);
        KeyUp(key);
    }

    public void KeyTapJump(int pressMs = 80)
    {
        KeyTap(JumpKey, pressMs);
    }

    public void KeyHoldJump(int holdMs = 1000)
    {
        KeyHold(JumpKey, holdMs);
    }

    public void ReleaseAll()
    {
        foreach (var key in new[] { 'w', 'a', 's', 'd', 'j', 'm' })
        {
            KeyUp(key);
        }

        Write("!");
    }

    private void Write(string command)
    {
        if (_serialPort?.IsOpen != true)
        {
            throw new InvalidOperationException("开发板串口尚未打开。");
        }

        _serialPort.Write(command);
    }

    public void Close()
    {
        if (_serialPort == null)
        {
            return;
        }

        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }

        _serialPort.Dispose();
        _serialPort = null;
    }

    public void Dispose()
    {
        Close();
    }

    private static string FormatAvailablePorts(string[] ports)
    {
        return ports.Length == 0 ? "无" : string.Join(", ", ports.OrderBy(port => port));
    }
}
