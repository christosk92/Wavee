using LanguageExt;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;

namespace Wavee.UI.Client.Artist;

public sealed class WaveeUIArtistView
{
    public WaveeUIArtistView(string id, string name, CoverImage avatarImage, Option<CoverImage> headerImage, ulong monthlyListeners, ulong followers, ArtistTopTrackViewModel[] topTracks, PagedArtistDiscographyPage[] discographyPages)
    {
        Id = id;
        Name = name;
        AvatarImage = avatarImage;
        HeaderImage = headerImage;
        MonthlyListeners = monthlyListeners;
        Followers = followers;
        TopTracks = topTracks;
        DiscographyPages = discographyPages;
    }

    public string Id { get; }
    public string Name { get; }
    public CoverImage AvatarImage { get; }
    public Option<CoverImage> HeaderImage { get; }
    public ulong MonthlyListeners { get; }
    public ulong Followers { get; }
    public ArtistTopTrackViewModel[] TopTracks { get; }
    public PagedArtistDiscographyPage[] DiscographyPages { get; }
}

public sealed class ArtistTopTrackViewModel
{
    public ArtistTopTrackViewModel(string id, Option<string> uid, string name, Option<ulong> playcount, TimeSpan duration, ContentRatingType contentRating, ITrackArtist[] artists, string albumId, ICoverImage[] albumImages, ushort index)
    {
        Id = id;
        Uid = uid;
        Name = name;
        Playcount = playcount;
        Duration = duration;
        ContentRating = contentRating;
        Artists = artists;
        AlbumId = albumId;
        AlbumImages = albumImages;
        Index = index;
    }

    public string Id { get; }
    public Option<string> Uid { get; }
    public string Name { get; }
    public Option<ulong> Playcount { get; }
    public TimeSpan Duration { get; }
    public ContentRatingType ContentRating { get; }
    public ITrackArtist[] Artists { get; }
    public string AlbumId { get; }
    public ICoverImage[] AlbumImages { get; }
    public ushort Index { get; }
}

public sealed class PagedArtistDiscographyPage
{
    private readonly Func<int, int, CancellationToken, Task<IEnumerable<ArtistDiscographyRelease>>> _getReleases;

    internal PagedArtistDiscographyPage(Func<int, int, CancellationToken, Task<IEnumerable<ArtistDiscographyRelease>>> getReleases)
    {
        _getReleases = getReleases;
    }

    public Task<IEnumerable<ArtistDiscographyRelease>> GetReleases(int offset, int limit, CancellationToken ct)
    {
        return _getReleases(offset, limit, ct);
    }
}
