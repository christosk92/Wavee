using Wavee.Enums;
using Wavee.Interfaces;

namespace Wavee.Models.Remote.Commands;

internal sealed class SetShuffleCommand : ISpotifyRemoteCommand
{
    private readonly bool _shuffle;
    private const string JsonFormat = "{\"command\":{\"endpoint\":\"set_shuffling_context\",\"value\":{0}}}";

    public SetShuffleCommand(bool shuffle)
    {
        _shuffle = shuffle;
    }

    public string ToJson() => string.Format(JsonFormat, _shuffle.ToString().ToLower());
    public string Describe()
    {
        return $"Set shuffle to {_shuffle}";
    }
}
internal sealed class SetRepeatCommand : ISpotifyRemoteCommand
{
    //{"command":{"repeating_context":true,"repeating_track":false,"endpoint":"set_options","logging_params":{"command_id":"3df6051e3d187a0419ca4ab56d6d8526"}}}
    private const string JsonFormat = "{\"command\":{\"repeating_context\":{0},\"repeating_track\":{1},\"endpoint\":\"set_options\"}}";
    private readonly bool _repeatTrack;
    private readonly bool _repeatContext;
    
    public SetRepeatCommand(RepeatMode repeatMode)
    {
        _repeatTrack = repeatMode == RepeatMode.Track;
        _repeatContext = repeatMode >= RepeatMode.Context;
    }

    public string ToJson() => string.Format(JsonFormat, _repeatContext.ToString().ToLower(), _repeatTrack.ToString().ToLower());
    public string Describe()
    {
        return $"Set repeat to {(_repeatTrack ? "track" : "context")}";
    }
}