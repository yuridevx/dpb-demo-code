namespace GoBo.Infrastructure.Logic;

public static class TimeExtensions
{
    public static TimeSpan Seconds(this int n)
    {
        return TimeSpan.FromSeconds(n);
    }

    public static TimeSpan Ms(this int n)
    {
        return TimeSpan.FromMilliseconds(n);
    }

    public static TimeSpan Minutes(this int n)
    {
        return TimeSpan.FromMinutes(n);
    }
}