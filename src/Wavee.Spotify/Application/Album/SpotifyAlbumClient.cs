using System.Collections.Immutable;
using System.Text.Json;
using LanguageExt.Pipes;
using Mediator;
using Spotify.Metadata;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
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
            var uid = root.GetProperty("uid").GetString();
            var item = root.GetProperty("track");
            var track = new SpotifyAlbumTrack
            {
                Duration = TimeSpan.FromMilliseconds(item.GetProperty("duration")
                    .GetProperty("totalMilliseconds")
                    .GetUInt32()),
                Name = item.GetProperty("name")
                    .GetString(),
                Uri = SpotifyId.FromUri(item.GetProperty("uri")
                    .GetString()),
                PlayCount = GetPlayCount(item),
                UniqueItemId = uid
            };
            output[i++] = track;
        }

        return output;
    }

    public async Task<SpotifyAlbumView> GetAlbum(SpotifyId spotifyId, CancellationToken cancellationToken)
    {
        const string hash = "01c6295923a9603d5a97eb945fc7e54d6fb5129ea801b54321647abe0d423c25";
        const string opName = "getAlbum";
        var variables = new Dictionary<string, object>
        {
            ["uri"] = spotifyId.ToString(),
            ["offset"] = 0,
            ["limit"] = 300,
            ["locale"] = string.Empty
        };
        using var response = await _mediator.Send(new GetSpotifyGraphQLQuery
        {
            OperationName = opName,
            Variables = variables,
            Hash = hash
        }, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var data = jsondoc.RootElement.GetProperty("data")
            .GetProperty("albumUnion");

        var artists = ReadArtists(data.GetProperty("artists"));
        var copyright = ReadCopyRight(data.GetProperty("copyright"));
        var coverArt = ParseVisuals(data.GetProperty("coverArt"));
        var (date, precision)= ReadDate(data.GetProperty("date"));
        var discs = ReadDiscs(data.GetProperty("discs"), data.GetProperty("tracks"));
        var moreAlbumsByArtist = ReadMoreAlbumsByArtist(data.GetProperty("moreAlbumsByArtist"));
        var name = data.GetProperty("name").GetString();
        var label = data.GetProperty("label").GetString();
        var uri = SpotifyId.FromUri(data.GetProperty("uri").GetString());
        var type = data.GetProperty("type").GetString();


        return new SpotifyAlbumView
        {
            Artists = artists,
            Discs = discs,
            Label = label,
            Copyright = copyright,
            Images = coverArt,
            MoreAlbumsByArtist = moreAlbumsByArtist,
            Name = name,
            ReleaseDate = date,
            Type = type,
            Id = uri,
            ReleaseDatePrecision = precision
        };
    }

    private static IReadOnlyCollection<SpotifySimpleAlbum> ReadMoreAlbumsByArtist(JsonElement albums)
    {
        var items = albums.GetProperty("items");
        //Nested in items[] -> discography.popularReleasesAlbums.items[]
        using var itemsEnumrator = items.EnumerateArray();
        itemsEnumrator.MoveNext();
        var discography = itemsEnumrator.Current.GetProperty("discography")
            .GetProperty("popularReleasesAlbums")
            .GetProperty("items");
        Span<SpotifySimpleAlbum> output = new SpotifySimpleAlbum[discography.GetArrayLength()];
        using var enumrator = discography.EnumerateArray();
        int i = 0;
        while (enumrator.MoveNext())
        {
            var item = enumrator.Current;
            var name = item.GetProperty("name").GetString();
            var uri = SpotifyId.FromUri(item.GetProperty("uri").GetString());
            var visuals = ParseVisuals(item.GetProperty("coverArt"));
            var year = item.GetProperty("date").GetProperty("year").GetUInt16();
            var type = item.GetProperty("type").GetString();
            output[i++] = new SpotifySimpleAlbum
            {
                Name = name,
                Uri = uri,
                Images = visuals,
                ReleaseDate = new DateOnly(year, 1, 1),
                Type = type
            };
        }

        return ImmutableArray.Create(output);
    }

    private static IReadOnlyCollection<SpotifyAlbumDisc> ReadDiscs(JsonElement discs, JsonElement tracks)
    {
        var discsItems = discs.GetProperty("items");
        Span<SpotifyAlbumDisc> discsOutput = new SpotifyAlbumDisc[discsItems.GetArrayLength()];
        int discsIndex = 0;
        using var discsEnumrator = discsItems.EnumerateArray();

        var itemsTrack = tracks.GetProperty("items");
        using var trackEnumrator = itemsTrack.EnumerateArray();
        while (discsEnumrator.MoveNext())
        {
            var number = discsEnumrator.Current.GetProperty("number").GetUInt16();
            var tracksTotalCount = discsEnumrator.Current
                .GetProperty("tracks")
                .GetProperty("totalCount")
                .GetUInt16();

            int trackIndex = 0;
            Span<SpotifyAlbumTrack> currentDiscTracks = new SpotifyAlbumTrack[tracksTotalCount];
            while (trackIndex < tracksTotalCount)
            {
                trackEnumrator.MoveNext();
                var x = trackEnumrator.Current;
                var uid = x.GetProperty("uid").GetString();
                var item = x.GetProperty("track");
                var discNumber = item.GetProperty("discNumber").GetUInt16();
                var trackNumber = item.GetProperty("trackNumber").GetUInt16();

                //Make sure we are in the right disc
                if (discNumber != number)
                    continue;

                var track = new SpotifyAlbumTrack
                {
                    Duration = TimeSpan.FromMilliseconds(item.GetProperty("duration")
                        .GetProperty("totalMilliseconds")
                        .GetUInt32()),
                    Name = item.GetProperty("name")
                        .GetString(),
                    Uri = SpotifyId.FromUri(item.GetProperty("uri")
                        .GetString()),
                    PlayCount = GetPlayCount(item),
                    UniqueItemId = uid
                };
                currentDiscTracks[trackIndex++] = track;
            }

            discsOutput[discsIndex++] = new SpotifyAlbumDisc
            {
                Number = number,
                Tracks = ImmutableArray.Create(currentDiscTracks)
            };
        }

        return ImmutableArray.Create(discsOutput);
    }

    private static (DateOnly Date, ReleaseDatePrecision Precision) ReadDate(JsonElement date)
    {
        var precision = date.GetProperty("precision").GetString();
        switch (precision)
        {
            case "DAY":
                var parsed = DateTime.Parse(date.GetProperty("isoString").GetString()!);
                return (DateOnly.FromDateTime(parsed), ReleaseDatePrecision.Day);
            default:
                return (new DateOnly(1, 1, 1), ReleaseDatePrecision.Year);
        }
    }

    private static IReadOnlyCollection<Copyright> ReadCopyRight(JsonElement copyright)
    {
        var items = copyright.GetProperty("items");
        Span<Copyright> output = new Copyright[items.GetArrayLength()];
        int i = 0;
        using var enumrator = items.EnumerateArray();
        while (enumrator.MoveNext())
        {
            var item = enumrator.Current;
            var text = item.GetProperty("text").GetString();
            var type = item.GetProperty("type").GetString();
            output[i++] = new Copyright
            {
                Text = text,
                Type = type switch
                {
                    "C" => Copyright.Types.Type.C,
                    "P" => Copyright.Types.Type.P,
                }
            };
        }

        return ImmutableArray.Create(output);
    }

    private static IReadOnlyCollection<SpotifySimpleArtist> ReadArtists(JsonElement artists)
    {
        var items = artists.GetProperty("items");
        Span<SpotifySimpleArtist> output = new SpotifySimpleArtist[items.GetArrayLength()];
        int i = 0;
        using var enumrator = items.EnumerateArray();
        while (enumrator.MoveNext())
        {
            var item = enumrator.Current;
            var name = item.GetProperty("profile").GetProperty("name").GetString();
            var visuals = ParseVisuals(item.GetProperty("visuals").GetProperty("avatarImage"));
            var uri = SpotifyId.FromUri(item.GetProperty("uri").GetString());
            output[i++] = new SpotifySimpleArtist
            {
                Uri = uri,
                Name = name,
                Images = visuals
            };
        }

        return ImmutableArray.Create(output);
    }

    private static IReadOnlyCollection<SpotifyImage> ParseVisuals(JsonElement image)
    {
        var sources = image.GetProperty("sources");
        Span<SpotifyImage> output = new SpotifyImage[sources.GetArrayLength()];
        int i = 0;
        using var enumrator = sources.EnumerateArray();
        while (enumrator.MoveNext())
        {
            var item = enumrator.Current;
            var url = item.GetProperty("url").GetString();
            var width = item.GetProperty("width").GetUInt16();
            var height = item.GetProperty("height").GetUInt16();
            output[i++] = new SpotifyImage
            {
                Url = url,
                Height = height,
                Width = width
            };
        }

        return ImmutableArray.Create(output);
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
    Task<SpotifyAlbumView> GetAlbum(SpotifyId spotifyId, CancellationToken cancellationToken);
}