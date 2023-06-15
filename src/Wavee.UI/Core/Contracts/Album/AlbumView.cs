using CommunityToolkit.Mvvm.Input;
using LanguageExt;
using System.Text.Json;
using Wavee.Core.Ids;
using Wavee.UI.Core.Contracts.Artist;
using Wavee.UI.Core.Contracts.Common;

namespace Wavee.UI.Core.Contracts.Album;

public sealed record AlbumView(
    AudioId Id,
    string Name,
    string Cover,
    ushort Year,
    Option<ushort> Month,
    Option<ushort> Day,
    ushort TrackCount,
    string[] CopyRights,
    SpotifyAlbumArtistView[] Artists,
    string Type,
    CardItem[] Related,
    IReadOnlyList<SpotifyDiscView> Discs)
{
    public static AlbumView From(ReadOnlyMemory<byte> payload, AudioId id)
    {
        using var jsonDocument = JsonDocument.Parse(payload);

        var name = jsonDocument.RootElement.GetProperty("name").GetString();
        var cover = jsonDocument.RootElement.GetProperty("cover").GetProperty("uri").ToString();
        var year = jsonDocument.RootElement.GetProperty("year").GetUInt16();
        var trackCount = jsonDocument.RootElement.GetProperty("track_count").GetUInt16();
        var copyrights =
            jsonDocument.RootElement.GetProperty("copyrights").EnumerateArray()
                .Select(x => x.GetString())
                .ToArray();

        Seq<SpotifyAlbumArtistView> artistsRes = LanguageExt.Seq<SpotifyAlbumArtistView>.Empty;
        using var artists = jsonDocument.RootElement.GetProperty("artists").EnumerateArray();
        foreach (var artist in artists)
        {
            var artistName = artist.GetProperty("name").GetString();
            var artistId = artist.GetProperty("uri").GetString();
            var artistImage = artist.TryGetProperty("image", out var img)
                ? img.GetProperty("uri").GetString()
                : null;
            artistsRes = artistsRes.Add(new SpotifyAlbumArtistView
            {
                Name = artistName,
                Id = AudioId.FromUri(artistId),
                Image = artistImage
            });
        }

        var month = jsonDocument.RootElement.TryGetProperty("month", out var m) ? m.GetUInt16() : Option<ushort>.None;
        var day = jsonDocument.RootElement.TryGetProperty("day", out var d) ? d.GetUInt16() : Option<ushort>.None;
        var type = jsonDocument.RootElement.GetProperty("type").GetString();

        Seq<CardItem> related = LanguageExt.Seq<CardItem>.Empty;
        if (jsonDocument.RootElement
            .TryGetProperty("related", out var rl))
        {
            using var relatedAlbums = rl.GetProperty("releases").EnumerateArray();
            foreach (var relatedAlbum in relatedAlbums)
            {
                var relatedAlbumName = relatedAlbum.GetProperty("name").GetString();
                var relatedAlbumUri = relatedAlbum.GetProperty("uri").GetString();
                var relatedAlbumImage = relatedAlbum.GetProperty("cover").GetProperty("uri").GetString();
                var year2 = relatedAlbum.GetProperty("year").GetUInt16();
                related = related.Add(new CardItem
                {
                    Id = AudioId.FromUri(relatedAlbumUri),
                    Title = relatedAlbumName,
                    ImageUrl = relatedAlbumImage,
                    Subtitle = year2.ToString()
                });
            }
        }

        var numbOfDiscs = jsonDocument.RootElement.GetProperty("discs").GetArrayLength();
        using var discs = jsonDocument.RootElement.GetProperty("discs").EnumerateArray();
        var discsRes = new List<SpotifyDiscView>();
        foreach (var disc in discs)
        {
            var number = disc.GetProperty("number").GetUInt16();
            using var tracks = disc.GetProperty("tracks").EnumerateArray();
            var resultOfDiscItem = LanguageExt.Seq<ArtistDiscographyTrack>.Empty;
            foreach (var track in tracks)
            {
                var arr = track.GetProperty("artists");
                using var artistssInTrack = arr.EnumerateArray();
                var artistsResults = artistssInTrack.Select(artistInTracki => new SpotifyAlbumArtistView
                {
                    Name = artistInTracki.GetProperty("name").GetString(),
                    Id = AudioId.FromUri(artistInTracki.GetProperty("uri").GetString()),
                    Image = artistInTracki.TryGetProperty("image", out var img)
                        ? img.GetProperty("uri").GetString()
                        : null
                }).ToList();
                resultOfDiscItem = resultOfDiscItem.Add(new ArtistDiscographyTrack
                {
                    Title = track.GetProperty("name")
                        .GetString(),
                    Id = AudioId.FromUri(track.GetProperty("uri")
                        .GetString()),
                    Duration = TimeSpan.FromMilliseconds(track.GetProperty("duration")
                        .GetUInt32()),
                    Number = track.GetProperty("number")
                        .GetUInt16(),
                    IsExplicit = track.GetProperty("explicit")
                        .GetBoolean(),
                    Playcount = track.GetProperty("playcount") is
                    {
                        ValueKind: JsonValueKind.Number
                    } p
                        ? p.GetUInt64()
                        : Option<ulong>.None,
                    Artists = artistsResults
                });
            }

            discsRes.Add(new SpotifyDiscView
            {
                Number = number,
                Tracks = resultOfDiscItem.ToArray(),
                HasMultipleDiscs = numbOfDiscs > 1
            });
        }

        return new AlbumView(
            Id: id,
            Name: name,
            Cover: cover,
            Year: year,
            Month: month,
            Day: day,
            Type: type,
            Artists: artistsRes.ToArray(),
            TrackCount: trackCount,
            Discs: discsRes.ToArray(),
            Related: related.ToArray(),
            CopyRights: copyrights);
    }
}