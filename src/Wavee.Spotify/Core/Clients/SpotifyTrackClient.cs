using System.Net;
using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Spotify.Core.Exceptions;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;

namespace Wavee.Spotify.Core.Clients;

internal sealed class SpotifyTrackClient : ISpotifyTrackClient
{
    private readonly ISpotifyTokenService _spotifyTokenService;
    private readonly SpotifyInternalHttpClient _spotifyInternalHttpClient;

    public SpotifyTrackClient(ISpotifyTokenService spotifyTokenService,
        SpotifyInternalHttpClient spotifyInternalHttpClient)
    {
        _spotifyTokenService = spotifyTokenService;
        _spotifyInternalHttpClient = spotifyInternalHttpClient;
    }

    public async ValueTask<SpotifyTrack> Get(SpotifyId trackId, CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = "/metadata/4/track/{0}?market=from_token";
            using var response = await _spotifyInternalHttpClient.Get(string.Format(endpoint, trackId.ToBase16()),
                accept: "application/protobuf",
                cancellationToken);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = Track.Parser.ParseFrom(stream);
            var dto = result.MapToDto();
            return dto;
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode is HttpStatusCode.NotFound)
            {
                throw new SpotifyTrackNotFoundException(trackId.ToBase62(), e);
            }

            throw;
        }
    }
}