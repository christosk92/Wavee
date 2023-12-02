using System.Text.Json;
using Mediator;
using Wavee.Spotify.Application.Library.Query;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Library;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify.Application.Library.QueryHandler;

public sealed class FetchArtistCollectionQueryHandler : IQueryHandler<FetchArtistCollectionQuery, IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>>>
{
    private readonly SpotifyTcpHolder _tcpHolder;

    public FetchArtistCollectionQueryHandler(SpotifyTcpHolder tcpHolder)
    {
        _tcpHolder = tcpHolder;
    }

    public async ValueTask<IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>>> Handle(FetchArtistCollectionQuery query, CancellationToken cancellationToken)
    {
        const string mercuryUrl = "hm://collection/artist/{0}?allowonlytracks=false&format=json";
        var forUser = string.Format(mercuryUrl, query.User);

        var response = await _tcpHolder.GetMercury(forUser);
        using var jsondoc = JsonDocument.Parse(response.Payload);
        var root = jsondoc.RootElement.GetProperty("item");
        var output = new SpotifyLibraryItem<SpotifyId>[root.GetArrayLength()];
        int i = 0;
        using var enumerator = root.EnumerateArray();
        while (enumerator.MoveNext())
        {
            var item = enumerator.Current;
            output[i++] = Parse(item);
        }

        return output;
    }

    private SpotifyLibraryItem<SpotifyId> Parse(JsonElement item)
    {
        Span<byte> id = item.GetProperty("identifier").GetBytesFromBase64();
        var spotifyId = SpotifyId.FromRaw(id, SpotifyItemType.Artist);

        var addedAtSeconds = item.GetProperty("added_at").GetInt64();
        var addedAt = DateTimeOffset.FromUnixTimeSeconds(addedAtSeconds);
        return new SpotifyLibraryItem<SpotifyId>
        {
            Item = spotifyId,
            AddedAt = addedAt,
            LastPlayedAt = null
        };
    }
}