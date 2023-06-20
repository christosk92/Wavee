using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Wavee.Infrastructure;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Mercury;

namespace Wavee.Spotify.Remote.Live;

internal sealed class SpotifyRemoteClient : ISpotifyRemoteClient
{
    private readonly string _deviceId;
    private readonly Func<CancellationToken, ValueTask<string>> _tokenFactory;
    private readonly SpotifyRemoteConnectionAccessor _conn;
    private readonly Subject<SpotifyRemoteState> _updates = new();
    private SpotifyRemoteState _state;

    public SpotifyRemoteClient(Func<IMercuryClient> mercuryFactory, SpotifyConnectionAccessor connectionFactory,
        string deviceId, Func<CancellationToken, ValueTask<string>> tokenFactory)
    {
        _deviceId = deviceId;
        _tokenFactory = tokenFactory;
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

    static Dictionary<string, object> empty = new();

    public Task Pause(CancellationToken cancellationToken = default)
    {
        const string commandName = "pause";
        return InvokeCommand(commandName, empty, cancellationToken);
    }

    public Task Resume(CancellationToken ct = default)
    {
        const string commandName = "resume";
        return InvokeCommand(commandName, empty, ct);
    }

    public Task SetShuffle(bool isShuffling, CancellationToken ct = default)
    {
        const string commandName = "set_shuffling_context";
        var value = new Dictionary<string, object>
        {
            { "value", isShuffling }
        };
        return InvokeCommand(commandName, value, ct);
    }

    public Task SkipNext(CancellationToken ct = default)
    {
        const string commandName = "skip_next";

        return InvokeCommand(commandName, empty, ct);
    }

    public Task SkipPrevious(CancellationToken ct = default)
    {
        const string commandName = "skip_prev";

        return InvokeCommand(commandName, empty, ct);
    }

    public Task SetRepeat(RepeatState next, CancellationToken ct = default)
    {
        // var options = new HashMap<string, object>()
        //     .Add("repeating_context", next >= RepeatState.Context)
        //     .Add("repeating_track", next is RepeatState.Track);
        var options = new Dictionary<string, object>
        {
            { "repeating_context", next >= RepeatState.Context },
            { "repeating_track", next is RepeatState.Track }
        };
        const string commandName = "set_options";

        return InvokeCommand(commandName, options, ct);
    }

    public Task SeekTo(TimeSpan to, CancellationToken ct = default)
    {
        const string commandName = "seek_to";
        var options = new Dictionary<string, object>
        {
            { "value", to.TotalMilliseconds }
        };
        return InvokeCommand(commandName, options, ct);
    }

    /// <summary>
    /// Invokes a command on the remote device
    /// </summary>
    /// <param name="commandName"></param>
    /// <param name="value"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NoActiveDeviceException"></exception>
    private async Task InvokeCommand(
        string commandName,
        Dictionary<string, object> value, CancellationToken ct)
    {
        //POSThttps://gae2-spclient.spotify.com/connect-state/v1/player/command/from/35ee833111913f567c4dc6ba7537cb6d089b130f/to/35ee833111913f567c4dc6ba7537cb6d089b130f
        var spClient = SpotifyConnectionAccessor.SpClient;
        var activeDevice = State.ActiveDeviceId;
        if (string.IsNullOrEmpty(activeDevice))
        {
            throw new NoActiveDeviceException();
        }

        var url = $"https://{spClient}/connect-state/v1/player/command/from/{_deviceId}/to/{activeDevice}";

        //{"command":{"repeating_context":true,"repeating_track":false,"endpoint":"set_options"}}
        //we need to concat the command name with the parameter (which is optional, and is just a dictionary 
        //of key value pairs)

        var command = new Dictionary<string, object>
        {
            { "endpoint", commandName }
        };

        //we need to flatten value
        static void Add(string key, object item, Dictionary<string, object> to)
        {
            //if item is a dictionary, add as nested dictionary
            if (item is Dictionary<string, object> dict)
            {
                var nested = new Dictionary<string, object>();
                foreach (var (k, v) in dict)
                {
                    Add(k, v, nested);
                }

                to.Add(key, nested);
            }
            else if (item is Dictionary<string, string> dictAsString)
            {
                var nested = new Dictionary<string, string>();
                foreach (var (k, v) in dictAsString)
                {
                    nested.Add(k, v);
                }

                to.Add(key, nested);
            }
            else
            {
                to.Add(key, item);
            }
        }

        foreach (var (key, val) in value)
        {
            Add(key, val, command);
        }

        // foreach (var (key, val) in value)
        //     command.Add(key, val);

        var serialized = JsonSerializer.Serialize(new
        {
            command = command
        });
        using var content = new StringContent(serialized, Encoding.UTF8, "application/json");
        var jwt = await _tokenFactory(ct);
        using var response = await HttpIO.Post(url,
            _empty,
            content,
            new AuthenticationHeaderValue("Bearer", jwt),
            ct);
        response.EnsureSuccessStatusCode();
    }

    private static Dictionary<string, string> _empty = new();
}