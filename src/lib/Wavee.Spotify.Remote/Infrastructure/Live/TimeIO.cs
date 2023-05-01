namespace Wavee.Spotify.Remote.Infrastructure.Live;

internal readonly struct TimeIO : Traits.TimeIO
{
    public static readonly Traits.TimeIO Default = new TimeIO();

    //TODO: Handle offset (spotify latency)
    public ulong Timestamp => (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}