namespace whatlanCar;

internal static class GlobalKeyboardState
{
    private static readonly object Sync = new();
    private static readonly HashSet<Keys> KeysDown = new();
    private static long _eventCount;

    public static long EventCount
    {
        get
        {
            lock (Sync)
            {
                return _eventCount;
            }
        }
    }

    public static bool IsDown(Keys key)
    {
        lock (Sync)
        {
            return KeysDown.Contains(key);
        }
    }

    public static void Update(Keys key, bool isDown)
    {
        if (!IsTrackedActionKey(key))
        {
            return;
        }

        lock (Sync)
        {
            if (isDown)
            {
                KeysDown.Add(key);
            }
            else
            {
                KeysDown.Remove(key);
            }

            _eventCount++;
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            KeysDown.Clear();
        }
    }

    public static bool IsTrackedActionKey(Keys key)
    {
        return key is Keys.W or Keys.A or Keys.S or Keys.D or Keys.J or Keys.M or Keys.Space;
    }
}
