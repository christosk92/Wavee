using Eum.Spotify;
using Wavee.Spotify.Infrastructure.Connection.Crypto;

namespace Wavee.Spotify.Infrastructure.Authentication;

public readonly record struct SpotifyAuthenticationResult(APWelcome WelcomeMessage,
    SpotifyConnectionRecord ConnectionRecord
);