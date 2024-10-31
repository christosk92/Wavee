using Wavee.Interfaces;

namespace Wavee.Models.Remote.Commands;

internal sealed class SpotifyPauseCommand : ISpotifyRemoteCommand
{
    //{"command":{,"endpoint":"pause"}}
    
    private const string JsonFormat = "{\"command\":{\"endpoint\":\"pause\"}}";
    public static readonly SpotifyPauseCommand Instance = new();
    public string ToJson() => JsonFormat;

    public string Describe()
    {
        return "Pause";
    }
}