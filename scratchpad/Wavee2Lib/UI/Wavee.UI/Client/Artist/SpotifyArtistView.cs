using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using System.Windows.Input;
using Wavee.Core.Ids;

namespace Wavee.UI.Client.Artist;
public record SpotifyArtistView(
    AudioId Id,
    string Name, string ProfilePicture, string HeaderImage,
    ulong MonthlyListeners,
    IReadOnlyCollection<ArtistTopTrackView> TopTracks,
    IReadOnlyCollection<ArtistDiscographyGroupView> Discography);
public class ArtistTopTrackView
{
    public required string Uri { get; set; }
    public required Option<ulong> Playcount { get; set; }
    public required string ReleaseImage { get; set; }
    public required string ReleaseName { get; set; }
    public required string ReleaseUri { get; set; }
    public required string Title { get; set; }
    public required AudioId Id { get; set; }
    public required int Index { get; set; }
    public required ICommand PlayCommand { get; set; }

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N0")
            : "< 1,000";
    }
}
public class ArtistDiscographyGroupView
{
    public required string GroupName { get; set; }
    public required List<ArtistDiscographyItem> Views { get; set; }
    public required bool CanSwitchTemplates { get; set; }
    public required bool AlwaysHorizontal { get; set; }
}
public class ArtistDiscographyItem
{
    public string Title { get; set; }
    public string Image { get; set; }
    public AudioId Id { get; set; }

    public ArtistDiscographyTracksHolder Tracks { get; set; }
    public string ReleaseDateAsStr { get; set; }
}

public class ArtistDiscographyTracksHolder
{

    public List<ArtistDiscographyTrack> Tracks { get; set; }
    public AudioId AlbumId { get; set; }
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
    public required ICommand PlayCommand { get; set; }

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
public class SpotifyAlbumArtistView
{
    public string Name { get; set; }
    public AudioId Id { get; set; }
    public string Image { get; set; }
}