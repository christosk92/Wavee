namespace Wavee.Time;

public interface ITimeProvider
{
    long CurrentTimeMilliseconds { get; }
}