using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Infrastructure.Remote;
using Wavee.Playback;

namespace Wavee.Remote.Live;

internal readonly struct LiveSpotifyRemoteClient : ISpotifyRemoteClient
{
    private readonly Guid _mainConnectionId;
    private readonly TaskCompletionSource<Unit> _waitForConnectionTask;

    public LiveSpotifyRemoteClient(Guid mainConnectionId, TaskCompletionSource<Unit> waitForConnectionTask)
    {
        _mainConnectionId = mainConnectionId;
        _waitForConnectionTask = waitForConnectionTask;
    }

    public IObservable<SpotifyRemoteState> CreateListener()
    {
        _waitForConnectionTask.Task.Wait();

        var reader = _mainConnectionId.CreateListener(RemoteStateListenerCondition);
        return Observable.Create<SpotifyRemoteState>(o =>
        {
            var cancel = new CancellationDisposable();
            Task.Run(async () =>
            {
                try
                {
                    await foreach (var package in reader.Reader.ReadAllAsync(cancel.Token))
                    {
                        var clusterUpdate = ClusterUpdate.Parser.ParseFrom(package.Payload.Span);
                        if (clusterUpdate.Cluster is null)
                        {
                            var state = SpotifyRemoteState.ParseFrom(Option<Cluster>.None);
                            o.OnNext(state ?? new SpotifyRemoteState());
                        }
                        else
                        {
                            var state = SpotifyRemoteState.ParseFrom(clusterUpdate.Cluster);
                            o.OnNext(state.Value);
                        }
                    }
                }
                finally
                {
                    reader.onDone();
                }
            });
            return cancel;
        }).StartWith(SpotifyRemoteState.ParseFrom(SpotifyRemoteConnection.GetInitialRemoteState(_mainConnectionId)) ??
                     new SpotifyRemoteState());
    }

    public Option<SpotifyRemoteState> LatestState
    {
        get
        {
            var parsed = SpotifyRemoteState.ParseFrom(SpotifyRemoteConnection.GetInitialRemoteState(_mainConnectionId));
            if (parsed.HasValue)
                return parsed.Value;
            return Option<SpotifyRemoteState>.None;
        }
    }

    private static bool RemoteStateListenerCondition(SpotifyRemoteMessage packagetocheck)
    {
        return packagetocheck.Uri.StartsWith("hm://connect-state/v1/cluster");
    }

    internal async Task PlaybackStateUpdated(SpotifyLocalPlaybackState spotifyLocalPlaybackState)
    {
        await SpotifyRemoteConnection.PutState(_mainConnectionId, spotifyLocalPlaybackState);
    }

    public void Dispose()
    {
        SpotifyRemoteConnection.Dispose(_mainConnectionId);
    }
}

internal readonly record struct SpotifyCommand(uint MessageId, string SentByDeviceId, string Endpoint,
    ReadOnlyMemory<byte> Data);