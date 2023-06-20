using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Mercury;

namespace Wavee.Spotify.Remote.Live;

internal sealed class SpotifyRemoteClient : ISpotifyRemoteClient
{
    private readonly SpotifyRemoteConnectionAccessor _conn;
    private readonly Subject<SpotifyRemoteState> _updates = new();
    private SpotifyRemoteState _state;

    public SpotifyRemoteClient(Func<IMercuryClient> mercuryFactory, SpotifyConnectionAccessor connectionFactory)
    {
        _conn = new SpotifyRemoteConnectionAccessor(mercuryFactory, connectionFactory);
        Task.Run(async () =>
        {
            IDisposable listener = null;
            bool connected = false;
            while (!connected)
            {
                try
                {
                    listener?.Dispose();
                    var connection = await _conn.Access();
                    connected = true;

                    //setup a loop to read messages
                    listener = connection.Cluster
                        .Select(s => SpotifyRemoteState.ParseFrom(s, connectionFactory.Access().DeviceId))
                        .Subscribe(x =>
                        {
                            _state = x;
                            _updates.OnNext(x);
                        });
                    var connectionLost = await connection.Closed;
                    if (connectionLost is null)
                    {
                        break;
                    }

                    Debug.WriteLine($"Lost connection to Spotify Remote: {connectionLost}");
                    await Task.Delay(3000);
                }
                catch (Exception x)
                {
                    Debug.WriteLine($"Failed to connect to Spotify Remote or lost connection: {x}");
                    Debugger.Break();
                    await Task.Delay(3000);
                }
            }
        });
    }

    public IObservable<SpotifyRemoteState> Updates => _updates;
    public SpotifyRemoteState State => _state;

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}