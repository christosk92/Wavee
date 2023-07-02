using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;
using Wavee.UI.Common;

namespace Wavee.UI.Client.Artist;

public sealed class WaveeUIArtistView
{
    public WaveeUIArtistView(string id, string name, ICoverImage avatarImage, Option<ICoverImage> headerImage,
        ulong monthlyListeners, ulong followers, ArtistTopTrackViewModel[] topTracks,
        PagedArtistDiscographyPage[] discographyPages, Option<IArtistOverviewPinnedItem> pinnedItem,
        Option<IArtistPreReleaseItem> preReleaseItem,
        Option<ICardViewModel>[] appearsOn,
        Option<ICardViewModel>[] relatedArtists,
        Option<ICardViewModel>[] artistPlaylists,
        Option<ICardViewModel>[] discoveredOn)
    {
        Id = id;
        Name = name;
        AvatarImage = avatarImage;
        HeaderImage = headerImage;
        MonthlyListeners = monthlyListeners;
        Followers = followers;
        TopTracks = topTracks;
        DiscographyPages = discographyPages;
        PinnedItem = pinnedItem;
        PreReleaseItem = preReleaseItem;
        AppearsOn = appearsOn;
        RelatedArtists = relatedArtists;
        ArtistPlaylists = artistPlaylists;
        DiscoveredOn = discoveredOn;
    }

    public string Id { get; }
    public string Name { get; }
    public ICoverImage AvatarImage { get; }
    public Option<ICoverImage> HeaderImage { get; }
    public Option<IArtistOverviewPinnedItem> PinnedItem { get; }
    public Option<IArtistPreReleaseItem> PreReleaseItem { get; }
    public Option<ICardViewModel>[] AppearsOn { get; }
    public ulong MonthlyListeners { get; }
    public ulong Followers { get; }
    public Option<ICardViewModel>[] RelatedArtists { get; }
    public Option<ICardViewModel>[] ArtistPlaylists { get; }
    public Option<ICardViewModel>[] DiscoveredOn { get; }
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
    private readonly GetReleases _getReleases;

    public PagedArtistDiscographyPage(GetReleases getReleases, ReleaseType type, bool canSwitchViews, bool hasSome)
    {
        _getReleases = getReleases;
        Type = type;
        CanSwitchViews = canSwitchViews;
        HasSome = hasSome;
    }
    public ReleaseType Type { get; }
    public bool CanSwitchViews { get; }

    public Task<IEnumerable<IArtistDiscographyRelease>> GetReleases(int offset, int limit, CancellationToken ct)
    {
        return _getReleases(offset, limit, ct);
    }
    public GetReleases GetReleasesFunc => GetReleases;
    public bool HasSome { get; }
}

public delegate Task<IEnumerable<IArtistDiscographyRelease>> GetReleases(int offset, int limit, CancellationToken ct);