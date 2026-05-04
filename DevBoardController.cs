using System.IO.Ports;

namespace whatlanCar;

public sealed class DevBoardController : IDisposable
{
    public const char JumpKey = 'j';

    private SerialPort? _serialPort;

    public bool IsOpen => _serialPort?.IsOpen == true;

    public void Open(string comPort, int baudRate = 115200)
    {
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
}
