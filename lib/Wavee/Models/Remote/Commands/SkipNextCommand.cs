using Wavee.Interfaces;

namespace Wavee.Models.Remote.Commands;

internal sealed class SkipNextCommand : ISpotifyRemoteCommand
{
    private const string JsonFormat = "{\"command\":{\"endpoint\":\"skip_next\"}}";
    public static readonly SkipNextCommand Instance = new();
    public string ToJson() => JsonFormat;
    public string Describe()
    {
        return "Skip next";
    }
}