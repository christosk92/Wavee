using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Infrastructure.Remote;

namespace Wavee.Remote.Live;

internal readonly struct LiveSpotifyRemoteClient : ISpotifyRemoteClient
{
    private readonly Guid _mainConnectionId;
    private readonly TaskCompletionSource _waitForConnectionTask;

    public LiveSpotifyRemoteClient(Guid mainConnectionId, TaskCompletionSource waitForConnectionTask)
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

    private static bool RemoteStateListenerCondition(SpotifyRemoteMessage packagetocheck)
    {
        return packagetocheck.Uri.StartsWith("hm://connect-state/v1/cluster");
    }
}