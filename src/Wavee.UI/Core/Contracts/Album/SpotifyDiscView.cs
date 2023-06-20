using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.UI.Core.Contracts.Artist;

namespace Wavee.UI.Core.Contracts.Album;

public class SpotifyDiscView
{
    public ushort Number { get; set; }
    public ArtistDiscographyTrack[] Tracks { get; set; }
    public bool HasMultipleDiscs { get; set; }

    public string FormatDiscName(ushort numb)
    {
        return $"Disc {Number}";
    }
}
public class ArtistDiscographyTrack
{
    public Option<ulong> Playcount { get; set; }
    public string Title { get; set; }
    public ushort Number { get; set; }
    public List<SpotifyAlbumArtistView> Artists { get; set; }
    public bool IsLoaded => !string.IsNullOrEmpty(Title);
    public AudioId Id { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsExplicit { get; set; }
    public ushort MinusOne(ushort v)
    {
        return (ushort)(v - 1);
    }

    public bool Negate(bool b)
    {
        return !b;
    }

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N0")
            : "< 1,000";
    }

    public string FormatTimestamp(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"mm\:ss");
    }
}