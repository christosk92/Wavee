using Wavee.Interfaces;
using Wavee.Models.Playlist;
using Wavee.Services;

namespace Wavee.Models.Remote.Commands;

internal sealed class SeekToCommand : ISpotifyRemoteCommand
{
    private readonly TimeSpan _position;
    private const string JsonFormat = "{\"command\":{\"endpoint\":\"seek_to\",\"value\":{0}}}";

    public SeekToCommand(TimeSpan position)
    {
        _position = position;
    }

    public string ToJson() => string.Format(JsonFormat, _position.TotalMilliseconds);

    public string Describe()
    {
        return $"Seek to {_position}";
    }
}