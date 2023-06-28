using Eum.Spotify.context;
using Google.Protobuf;
using Org.BouncyCastle.Utilities;
using System.Text;
using Wavee.Id;
using Wavee.Infrastructure;

namespace Wavee.UI.Client.Previews;

internal sealed class SpotifyUIPreviewClient : IWaveeUIPreviewClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;
    public SpotifyUIPreviewClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public async Task<IEnumerable<Task<string>>> GetPreviewStreamsForContext(string id, CancellationToken ct = default)
    {
        //do a context resolve
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        var context = await spotifyClient.ContextResolver.Resolve(id, ct);
        var firstPage = context.Pages.First();
        var lazyEnumerator = CreateLazyEnumerator(spotifyClient, firstPage, ct);
        return lazyEnumerator;
    }

    private IEnumerable<Task<string>> CreateLazyEnumerator(SpotifyClient client, ContextPage firstPage,
        CancellationToken cancellationToken)
    {
        foreach (var track in firstPage.Tracks)
        {
            var uri = SpotifyId.FromUri(track.Uri);

            yield return GetPreviewStream(client, uri, cancellationToken);
        }
    }

    private async Task<string> GetPreviewStream(SpotifyClient client, SpotifyId uri,
        CancellationToken cancellationToken)
    {
        var metadata = await client.Metadata.GetTrack(uri, cancellationToken);
        var previewId = GetBase16String(metadata.Preview.First().FileId);
        var url = $"https://p.scdn.co/mp3-preview/{previewId}?cid=d8a5ed958d274c2e8ee717e6a4b0971d";
        return url;
        // using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // var response = await HttpIO.Get(url, new Dictionary<string, string>(), null, cancellationToken);
        // var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        // return stream;
    }

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