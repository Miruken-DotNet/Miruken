namespace Miruken.Infrastructure;

using System;

public static class TimeSpanExtensions
{
    public static TimeSpan Millis(this int ms)
    {
        return TimeSpan.FromMilliseconds(ms);
    }

    public static TimeSpan Millis(this double ms)
    {
        return TimeSpan.FromMilliseconds(ms);
    }

    public static TimeSpan Sec(this int seconds)
    {
        return TimeSpan.FromSeconds(seconds);
    }

    public static TimeSpan Sec(this double seconds)
    {
        return TimeSpan.FromSeconds(seconds);
    }

    public static TimeSpan Min(this int minutes)
    {
        return TimeSpan.FromMinutes(minutes);
    }

    public static TimeSpan Min(this double minutes)
    {
        return TimeSpan.FromMinutes(minutes);
    }
}