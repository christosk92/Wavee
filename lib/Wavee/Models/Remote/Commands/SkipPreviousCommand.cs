using Wavee.Interfaces;

namespace Wavee.Models.Remote.Commands;

internal sealed class SkipPreviousCommand : ISpotifyRemoteCommand
{
    private const string JsonFormat = "{\"command\":{\"endpoint\":\"skip_prev\"}}";
    public static readonly SkipPreviousCommand Instance = new();
    public string ToJson() => JsonFormat;
    public string Describe()
    {
        return "Skip previous";
    }
}