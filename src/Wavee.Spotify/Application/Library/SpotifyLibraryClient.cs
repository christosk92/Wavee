using Eum.Spotify.transfer;
using Mediator;
using System.Buffers.Binary;
using System;
using Eum.Spotify;
using Google.Protobuf;
using Nito.AsyncEx;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify.Application.Library;

internal class SpotifyLibraryClient : ISpotifyLibraryClient
{
    private readonly IMediator _mediator;
    private readonly SpotifyTcpHolder _spotifyTcpHolder;
    public SpotifyLibraryClient(IMediator mediator, SpotifyTcpHolder spotifyTcpHolder)
    {
        _mediator = mediator;
        _spotifyTcpHolder = spotifyTcpHolder;
    }

    public async Task<SpotifyLibraryItem> GetArtists()
    {
        var userId = _spotifyTcpHolder.WelcomeMessage.Result.CanonicalUsername;

        var mercuryUri = $"hm://collection/artist/{userId}?allowonlytracks=false&format=json";
        var res = await _spotifyTcpHolder.GetMercury(mercuryUri);
        return null;
    }
}

public interface ISpotifyLibraryClient
{
    Task<SpotifyLibraryItem> GetArtists();
}

public sealed class SpotifyLibraryItem
{
    public required string Id { get; init; }
    public required SpotifyItemType Type { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
}