using System.Net.Http.Headers;
using Mediator;
using Spotify.Metadata;
using Wavee.Spotify.Application.Tracks.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.Persistent;

namespace Wavee.Spotify.Application.Tracks.QueryHandlers;

public sealed class SpotifyGetTrackQueryHandler : IQueryHandler<SpotifyGetTrackQuery, Track>
{
    private readonly ISpotifyGenericRepository _spotifyTrackRepository;
    private readonly HttpClient _httpClient;

    public SpotifyGetTrackQueryHandler(ISpotifyGenericRepository spotifyTrackRepository,
        IHttpClientFactory httpClientFactory)
    {
        _spotifyTrackRepository = spotifyTrackRepository;
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public ValueTask<Track> Handle(SpotifyGetTrackQuery query, CancellationToken cancellationToken)
    {
        if (_spotifyTrackRepository.TryGetTrack(query.TrackId, out var track))
        {
            return new ValueTask<Track>(track);
        }

        return new ValueTask<Track>(FetchTrack(query.TrackId, cancellationToken)
            .ContinueWith(task =>
            {
                var x = task.Result;
                _spotifyTrackRepository.AddTrack(x);
                return x;
            }, cancellationToken));
    }

    private async Task<Track> FetchTrack(SpotifyId id, CancellationToken cancellationToken)
    {
        //https://spclient.com/metadata/4/track/b11e015bb3044b06a8208ee9453d375c?market=from_token
        const string url = "https://spclient.com/metadata/4/track/{0}?market=from_token";
        var hexId = id.ToBase16();
        using var request = new HttpRequestMessage(HttpMethod.Get, string.Format(url, hexId));
        //accept protobuf
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/protobuf"));
        using var response = await _httpClient.GetAsync(string.Format(url, hexId), cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = Track.Parser.ParseFrom(stream);
        return result;
    }
}