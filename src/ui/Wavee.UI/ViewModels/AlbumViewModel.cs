using ReactiveUI;
using System.Text.Json;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Models;

namespace Wavee.UI.ViewModels;

public sealed class AlbumViewModel<R> : ReactiveObject, INavigableViewModel where R : struct, HasSpotify<R>
{
    private bool _isBusy = true;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
    private readonly R _runtime;
    public AlbumViewModel(R runtime)
    {
        _runtime = runtime;
    }

    public TaskCompletionSource AlbumFetched = new TaskCompletionSource();

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is not AudioId id)
        {
            return;
        }

        //fetching the mobile version also gives us the artists image
        const string fetch_uri = "hm://album/v1/album-app/album/{0}/android?country={1}";
        var aff =
            from countryCode in Spotify<R>.CountryCode().Map(x => x.ValueUnsafe())
            let url = string.Format(fetch_uri, id.ToString(), countryCode)
            from mercuryClient in Spotify<R>.Mercury().Map(x => x)
            from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
            select response;
        var result = await aff.Run(runtime: _runtime);
        var r = result.ThrowIfFail();
        using var jsonDoc = JsonDocument.Parse(r.Payload);

        var name = jsonDoc.RootElement.GetProperty("name").GetString();
        var cover = jsonDoc.RootElement.GetProperty("cover").GetProperty("uri").ToString();

        var year = jsonDoc.RootElement.GetProperty("year").GetUInt16();
        var trackCount = jsonDoc.RootElement.GetProperty("track_count").GetUInt16();
        var copyrights =
            jsonDoc.RootElement.GetProperty("copyrights").EnumerateArray()
                .Select(x => x.GetString())
                .ToSeq();

        Seq<SpotifyAlbumArtistView> artistsRes = LanguageExt.Seq<SpotifyAlbumArtistView>.Empty;
        using var artists = jsonDoc.RootElement.GetProperty("artists").EnumerateArray();
        foreach (var artist in artists)
        {
            var artistName = artist.GetProperty("name").GetString();
            var artistId = artist.GetProperty("uri").GetString();
            var artistImage = artist.GetProperty("image").GetProperty("uri").GetString();
            artistsRes = artistsRes.Add(new SpotifyAlbumArtistView
            {
                Name = artistName,
                Id = AudioId.FromUri(artistId),
                Image = artistImage
            });
        }

        var month = jsonDoc.RootElement.GetProperty("month").GetUInt16();
        var day = jsonDoc.RootElement.GetProperty("day").GetUInt16();
        var type = jsonDoc.RootElement.GetProperty("type").GetString();


        Seq<SpotifyViewItem> related = LanguageExt.Seq<SpotifyViewItem>.Empty;
        using var relatedAlbums = jsonDoc.RootElement.GetProperty("related").GetProperty("releases").EnumerateArray();
        foreach (var relatedAlbum in relatedAlbums)
        {
            var relatedAlbumName = relatedAlbum.GetProperty("name").GetString();
            var relatedAlbumUri = relatedAlbum.GetProperty("uri").GetString();
            var relatedAlbumImage = relatedAlbum.GetProperty("cover").GetProperty("uri").GetString();
            var year2 = relatedAlbum.GetProperty("year").GetUInt16();
            related = related.Add(new SpotifyViewItem
            {
                Id = AudioId.FromUri(relatedAlbumUri),
                Title = relatedAlbumName,
                Image = relatedAlbumImage,
                Description = year2.ToString()
            });
        }

        var numbOfDiscs = jsonDoc.RootElement.GetProperty("discs").GetArrayLength();
        using var discs = jsonDoc.RootElement.GetProperty("discs").EnumerateArray();
        Seq<SpotifyDiscView> discsRes = LanguageExt.Seq<SpotifyDiscView>.Empty;
        foreach (var disc in discs)
        {
            var number = disc.GetProperty("number").GetUInt16();
            using var tracks = disc.GetProperty("tracks").EnumerateArray();
            var resultOfDiscItem = LanguageExt.Seq<ArtistDiscographyTrack>.Empty;
            foreach (var track in tracks)
            {
                Seq<SpotifyAlbumArtistView> artistsREsult = LanguageExt.Seq<SpotifyAlbumArtistView>.Empty;
                using var artistssInTrack = track.GetProperty("artists").EnumerateArray();
                foreach (var artistInTracki in artistssInTrack)
                {
                    artistsREsult = artistsREsult.Add(new SpotifyAlbumArtistView
                    {
                        Name = artistInTracki.GetProperty("name").GetString(),
                        Id = AudioId.FromUri(artistInTracki.GetProperty("uri").GetString()),
                        Image = artistInTracki.GetProperty("image").GetProperty("uri").GetString()
                    });
                }
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
                    Playcount = track.GetProperty("playcount") is { ValueKind: JsonValueKind.Number } p ? p.GetUInt64() : Option<ulong>.None,
                    Artists = artistsREsult
                });
            }

            discsRes = discsRes.Add(new SpotifyDiscView
            {
                Number = number,
                Tracks = resultOfDiscItem,
                HasMultipleDiscs = numbOfDiscs > 1
            });
        }

        Name = name;
        Image = cover;
        Year = year;
        Month = month;
        Day = day;
        TracksCount = trackCount;
        Artists = artistsRes;
        Related = related;
        Discs = discsRes;
        Type = type;

        AlbumFetched.SetResult();
        IsBusy = false;
    }

    public string Name { get; set; }
    public string Image { get; set; }
    public ushort Year { get; set; }
    public ushort Month { get; set; }
    public ushort Day { get; set; }
    public ushort TracksCount { get; set; }
    public Seq<SpotifyAlbumArtistView> Artists { get; set; }
    public Seq<SpotifyViewItem> Related { get; set; }
    public Seq<SpotifyDiscView> Discs { get; set; }
    public string Type { get; set; }
    public void OnNavigatedFrom()
    {

    }
}

public readonly struct SpotifyDiscView
{
    public required ushort Number { get; init; }
    public required Seq<ArtistDiscographyTrack> Tracks { get; init; }
    public required bool HasMultipleDiscs { get; init; }

    public string FormatDiscName(ushort numb)
    {
        return $"Disc {Number}";
    }
}
public readonly struct SpotifyAlbumArtistView
{
    public required string Name { get; init; }
    public required AudioId Id { get; init; }
    public required string Image { get; init; }
}