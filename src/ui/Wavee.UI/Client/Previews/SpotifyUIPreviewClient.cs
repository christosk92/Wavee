using Eum.Spotify.context;
using Google.Protobuf;
using Org.BouncyCastle.Utilities;
using System.Text;
using LanguageExt;
using Wavee.Id;
using Wavee.Infrastructure;
using Wavee.Infrastructure.Public.Response;

namespace Wavee.UI.Client.Previews;

internal sealed class SpotifyUIPreviewClient : IWaveeUIPreviewClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;
    public SpotifyUIPreviewClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public async Task<Option<string>> GetPreviewStreamsForContext(string id, CancellationToken ct = default)
    {
        //do a context resolve
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        var spotifyId = SpotifyId.FromUri(id);
        var previewUrl = spotifyId.Type switch
        {
            AudioItemType.Album => await spotifyClient.Public.GetAlbumTracks(spotifyId, 0, 1, ct).Map(x =>
            {
                var firstTrack = x.Items.FirstOrDefault();
                switch (firstTrack)
                {
                    case { } track:
                        return track.PreviewUrl;
                    default:
                        return Option<string>.None;
                }
            }),
            AudioItemType.Playlist => await spotifyClient.Public.GetPlaylistTracks(spotifyId, 0, 1, Option<AudioItemType[]>.None, ct).Map(f =>
            {
                var firstTrack = f.Items.FirstOrDefault();
                switch (firstTrack)
                {
                    case SpotifyTrackPlaylistItem track:
                        return track.Track.PreviewUrl;
                    case SpotifyEpisodePlaylistItem episode:
                        return episode.Episode.PreviewUrl;
                    default:
                        return Option<string>.None;
                }
            }),
            AudioItemType.Track => await spotifyClient.Public.GetTrack(spotifyId, ct).Map(f => f.PreviewUrl),
            AudioItemType.Artist => await spotifyClient.Public.GetArtistTopTracks(spotifyId, "US", ct).Map(f =>
            {
                var firstTrack = f.FirstOrDefault();
                switch (firstTrack)
                {
                    case { } track:
                        return track.PreviewUrl;
                    default:
                        return Option<string>.None;
                }
            }),
            _ when id.Contains("collection") => await spotifyClient.Public.GetMyTracks(0, 1, ct).Map(
                f =>
                {
                    var firstTrack = f.Items.FirstOrDefault();
                    switch (firstTrack)
                    {
                        case { } track:
                            return track.PreviewUrl;
                        default:
                            return Option<string>.None;
                    }
                }),
            _ => throw new InvalidOperationException("Invalid SpotifyId")
        };
        return previewUrl;
    }

    // private IEnumerable<Task<string>> CreateLazyEnumerator(SpotifyClient client, Seq<string> urls,
    //     CancellationToken cancellationToken)
    // {
    //     foreach (var track in firstPage.Tracks)
    //     {
    //         var uri = SpotifyId.FromUri(track.Uri);
    //
    //         yield return GetPreviewStream(client, uri, cancellationToken);
    //     }
    // }
    //
    // private async Task<string> GetPreviewStream(SpotifyClient client, SpotifyId uri,
    //     CancellationToken cancellationToken)
    // {
    //     var metadata = await client.Metadata.GetTrack(uri, cancellationToken);
    //     var previewId = GetBase16String(metadata.Preview.First().FileId);
    //     var url = $"https://p.scdn.co/mp3-preview/{previewId}?cid=d8a5ed958d274c2e8ee717e6a4b0971d";
    //     return url;
    //     // using var request = new HttpRequestMessage(HttpMethod.Get, url);
    //     // var response = await HttpIO.Get(url, new Dictionary<string, string>(), null, cancellationToken);
    //     // var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    //     // return stream;
    // }

    private static string GetBase16String(ByteString bs)
    {
        var bytes = bs.Span;
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }
}