using System.Text.Json;
using Mediator;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Tracks;

namespace Wavee.Spotify.Application.Album;

internal sealed class SpotifyAlbumClient : ISpotifyAlbumClient
{
    private readonly IMediator _mediator;

    public SpotifyAlbumClient(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IReadOnlyCollection<SpotifyAlbumTrack>> GetTracks(SpotifyId album, CancellationToken cancellationToken)
    {
        const string operationname = "queryAlbumTracks";
        const string operationhash = "8f7ebdeb93b6df4c31e6005d9ac29cde13d7543ce14d173e5e5e9599aafbcb9a";

        var variables = new Dictionary<string, object>
        {
            ["uri"] = album.ToString(),
            ["offset"] = 0,
            ["limit"] = 300
        };
        using var response = await _mediator.Send(new GetSpotifyGraphQLQuery
        {
            OperationName = operationname,
            Variables = variables,
            Hash = operationhash
        }, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var actualItems = jsondoc.RootElement.GetProperty("data")
            .GetProperty("albumUnion")
            .GetProperty("tracks")
            .GetProperty("items");
        var output = new SpotifyAlbumTrack[actualItems.GetArrayLength()];
        int i = 0;
        using var enumrator = actualItems.EnumerateArray();
        while (enumrator.MoveNext())
        {
            var root = enumrator.Current;
            var item = root.GetProperty("track");
            var track = new SpotifyAlbumTrack
            {
                Duration = TimeSpan.FromMilliseconds(item.GetProperty("duration").GetProperty("totalMilliseconds").GetUInt32()),
                Name = item.GetProperty("name").GetString(),
                Uri = SpotifyId.FromUri(item.GetProperty("uri").GetString()),
                PlayCount = GetPlayCount(item)
            };
            output[i++] = track;
        }

        return output;
    }

    private static ulong? GetPlayCount(JsonElement item)
    {
        var playcount = item.GetProperty("playcount");
        if (playcount.ValueKind is JsonValueKind.Null) return null;
        var playcountvalue = playcount.GetString();
        if (ulong.TryParse(playcountvalue, out var result))
        {
            return result;
        }
        return null;
    }
}

public interface ISpotifyAlbumClient
{
    Task<IReadOnlyCollection<SpotifyAlbumTrack>> GetTracks(SpotifyId album, CancellationToken cancellationToken);
}