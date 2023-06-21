using LanguageExt.UnsafeValueAccess;
using Serilog;
using Wavee.Player;
using Wavee.Remote;
using Wavee.Remote.Live;

namespace Wavee.Playback.Live;

internal readonly struct LiveSpotifyPlaybackClient : ISpotifyPlaybackClient
{
    private readonly WeakReference<IWaveePlayer> _player;
    private readonly WeakReference<ISpotifyRemoteClient> _remoteClient;
    private readonly TaskCompletionSource _waitForConnectionTask;
    public LiveSpotifyPlaybackClient(Guid connectionId, WeakReference<IWaveePlayer> player, WeakReference<ISpotifyRemoteClient> remoteClient, TaskCompletionSource waitForConnectionTask)
    {
        _player = player;
        _remoteClient = remoteClient;
        _waitForConnectionTask = waitForConnectionTask;
    }
    
    public async Task<bool> Takeover()
    {
        await _waitForConnectionTask.Task;
        if (_player.TryGetTarget(out var player) && _remoteClient.TryGetTarget(out var remoteClient))
        {
            var currentRemoteState = remoteClient.LatestState;
            if (currentRemoteState.IsNone)
            {
                Log.Warning("No remote state available");
                return false;
            }

            Log.Information("Taking over playback");
            var val = currentRemoteState.ValueUnsafe();
        }
        
        Log.Warning("Player or remote client is no longer available (GC?). Try with a new instance of the playback client.");
        return false;
    }
    internal async Task RemoteCommand(SpotifyCommand remoteCommand)
    {
        throw new NotImplementedException();
    }
}