using Eum.Spotify;
using Mediator;
using Wavee.Spotify.Application.LegacyAuth.Commands;

namespace Wavee.Spotify.Application.LegacyAuth.CommandHandlers;

public sealed class
    SpotifyAuthenticateLegacyCommandHandler : ICommandHandler<SpotifyAuthenticateLegacyCommand, APWelcome>
{
    public ValueTask<APWelcome> Handle(SpotifyAuthenticateLegacyCommand command, CancellationToken cancellationToken)
    {
        return new ValueTask<APWelcome>(
            Task.Run(() => Create(command.Credentials, command.DeviceId), cancellationToken));
    }

    public static APWelcome Create(
        LoginCredentials credentials,
        string deviceId)
    {
        const string host = "ap-gae2.spotify.com";
        const ushort port = 4070;

        using var tcp = TcpIO.Connect(host, port);
        var stream = tcp.GetStream();
        var keys = Handshake.PerformHandshake(stream);
        var welcomeMessage = Auth.Authenticate(stream, keys, credentials, deviceId);

        return welcomeMessage;
    }
}

internal readonly ref struct SpotifyUnencryptedPackage
{
    public required SpotifyPacketType Type { get; init; }
    public required ReadOnlySpan<byte> Payload { get; init; }
}