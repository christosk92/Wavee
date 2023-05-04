using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Live;

namespace Wavee.Spotify;

public static class MercuryRuntime
{
    public static async ValueTask<MercuryResponse> Get(string url, Option<Guid> connectionId)
    {
        //if connectionId is none, return default   
        var connectionMaybe = connectionId.Match(
            Some: id =>
            {
                var k = SpotifyRuntime.Connections.Value.Find(id);
                return k;
            },
            None: () =>
            {
                var k = SpotifyRuntime.Connections.Value.Find(_ => true);
                return k.Match(
                    Some: z => z.Value,
                    None: () => throw new Exception("No connection found"));
            });
        var connection = connectionMaybe.Match(Some: r => r, None: () => throw new Exception("No connection found"));

        var listenerMaybe =
            SpotifyRuntime.SetupListener<WaveeRuntime>(connectionId.ValueUnsafe())
                .Run(WaveeCore.Runtime);
        var listener = listenerMaybe.Match(Succ: r => r, Fail: (e) => throw e);

        return default;
    }
}

public readonly record struct MercuryResponse;