using Mediator;
using Wavee.Spotify.Application.LegacyAuth.CommandHandlers;

namespace Wavee.Spotify.Infrastructure.LegacyAuth.CommandHandlers;

internal readonly ref struct SpotifyUnencryptedPackage
{
    public required SpotifyPacketType Type { get; init; }
    public required ReadOnlySpan<byte> Payload { get; init; }
}