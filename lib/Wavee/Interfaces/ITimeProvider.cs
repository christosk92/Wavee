namespace Wavee.Interfaces;

public interface ITimeProvider
{
    ValueTask<DateTimeOffset> CurrentTime();
}