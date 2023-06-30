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
