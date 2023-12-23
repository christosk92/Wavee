using System.Net;
using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Spotify.Common;
using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Interfaces.Clients;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Exceptions;
using Wavee.Spotify.Infrastructure.HttpClients;

namespace Wavee.Spotify.Core.Clients;

internal sealed class SpotifyTrackClient : ISpotifyTrackClient
{
    private readonly ISpotifyTokenService _spotifyTokenService;
    private readonly SpotifyInternalHttpClient _spotifyInternalHttpClient;

    public SpotifyTrackClient(ISpotifyTokenService spotifyTokenService, SpotifyInternalHttpClient spotifyInternalHttpClient)
    {
        _spotifyTokenService = spotifyTokenService;
        _spotifyInternalHttpClient = spotifyInternalHttpClient;
    }

    public async ValueTask<SpotifyTrack> GetTrackAsync(string trackId, bool allowCache = false, CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = "/metadata/4/track/{0}?market=from_token";
            var spotifyId = SpotifyId.FromBase62(trackId, AudioItemType.Track);
            using var response = await _spotifyInternalHttpClient.Get(string.Format(endpoint, spotifyId.ToBase16()),
                accept: "application/protobuf",
                cancellationToken);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = Track.Parser.ParseFrom(stream);
            return result.MapToDto();
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode is HttpStatusCode.NotFound)
            {
                throw new SpotifyTrackNotFoundException(trackId, e);
            }
            
            throw;
        }
    }
}