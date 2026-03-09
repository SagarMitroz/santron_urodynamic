using System;
using System.Collections.Concurrent;
using System.Diagnostics;

internal static class PerfTrace
{
    public static volatile bool Enabled = true;

    // per-tag throttle timestamps
    private static readonly ConcurrentDictionary<string, long> _lastByTag = new ConcurrentDictionary<string, long>();

    public static void EveryMs(string tag, int ms, Func<string> msg)
    {
        if (!Enabled) return;

        long now = Stopwatch.GetTimestamp();
        long last = _lastByTag.GetOrAdd(tag, 0);

        double elapsed = (now - last) * 1000.0 / Stopwatch.Frequency;
        if (elapsed < ms) return;

        _lastByTag[tag] = now;
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {tag} {msg()}");
    }

    public static void Log(string tag, string msg)
    {
        if (!Enabled) return;
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {tag} {msg}");
    }
}
