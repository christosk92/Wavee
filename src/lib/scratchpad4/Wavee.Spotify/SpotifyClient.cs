using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using Eum.Spotify;
using Wavee.Infrastructure.Live;
using Wavee.Spotify.Sys;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Connection.Contracts;

namespace Wavee.Spotify;

public static class SpotifyClient
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<SpotifyConnectionInfo> Authenticate(LoginCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        var deviceId = Guid.NewGuid().ToString();

        var connectionId = await SpotifyConnection<WaveeRuntime>.Authenticate(
            deviceId,
            credentials,
            cancellationToken).Run(WaveeCore.Runtime);

        return connectionId
            .Match(
                Succ: g => g,
                Fail: e => throw e
            );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<string> CountryCode(this SpotifyConnectionInfo connection)
    {
        var countryCode = await ConnectionListener<WaveeRuntime>.ConsumePacket(connection.ConnectionId,
                p => p.Command is SpotifyPacketType.CountryCode)
            .Run(WaveeCore.Runtime);
        return countryCode
            .Match(
                Succ: p => Encoding.UTF8.GetString(p.Data.Span),
                Fail: e => throw e
            );
    }
}