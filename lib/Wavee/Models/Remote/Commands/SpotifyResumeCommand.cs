using Wavee.Interfaces;

namespace Wavee.Models.Remote.Commands;

internal sealed class SpotifyResumeCommand : ISpotifyRemoteCommand
{
    private const string JsonFormat = "{\"command\":{\"endpoint\":\"resume\"}}";
    public static readonly SpotifyResumeCommand Instance = new();
    public string ToJson() => JsonFormat;
    public string Describe()
    {
        return "Resume";
    }
}