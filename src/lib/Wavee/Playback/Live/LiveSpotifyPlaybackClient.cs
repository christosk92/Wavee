using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using Wavee.Infrastructure.Playback;
using Wavee.Playback.Command;
using Wavee.Player;
using Wavee.Remote;
using Wavee.Remote.Live;

namespace Wavee.Playback.Live;

internal readonly struct LiveSpotifyPlaybackClient : ISpotifyPlaybackClient
{
    private readonly Guid _connectionId;
    private readonly WeakReference<ISpotifyRemoteClient> _remoteClient;
    private readonly TaskCompletionSource<Unit> _waitForConnectionTask;

    public LiveSpotifyPlaybackClient(Guid connectionId,
        WeakReference<ISpotifyRemoteClient> remoteClient, TaskCompletionSource<Unit> waitForConnectionTask)
    {
        _connectionId = connectionId;
        _remoteClient = remoteClient;
        _waitForConnectionTask = waitForConnectionTask;
    }

    public async Task<bool> Takeover()
    {
        await _waitForConnectionTask.Task;
        if (_remoteClient.TryGetTarget(out var remoteClient))
        {
            var currentRemoteState = remoteClient.LatestState;
            if (currentRemoteState.IsNone)
            {
                Log.Warning("No remote state available");
                return false;
            }

            Log.Information("Taking over playback");
            var val = currentRemoteState.ValueUnsafe();
            var playbackEvent = SpotifyPlayCommand.From(val,
                SpotifyClient.Clients[_connectionId].Config.Playback.CrossfadeDurationRef);
            await SpotifyPlaybackHandler.Send(_connectionId, playbackEvent);
            return true;
            //await OnPlaybackEvent(playbackEvent, player);
        }

        Log.Warning(
            "Player or remote client is no longer available (GC?). Try with a new instance of the playback client.");
        return false;
    }

    internal async Task RemoteCommand(SpotifyCommand remoteCommand)
    {
        switch (remoteCommand.Endpoint)
        {
            case "transfer":
                break;
            case "skip_next":
                var skpxn = new SpotifySkipNextCommand(SpotifyClient.Clients[_connectionId].Config.Playback
                    .CrossfadeDurationRef);
                await SpotifyPlaybackHandler.Send(_connectionId, skpxn);
                break;
        }
    }
}