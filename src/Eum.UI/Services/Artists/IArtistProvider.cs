using Eum.Connections.Spotify.Models.Artists;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.UI.Items;
using Eum.UI.Services.Tracks;
using Humanizer;

namespace Eum.UI.Services.Artists
{
    public interface IArtistProvider
    {
        ValueTask<EumArtist> GetArtist(ItemId id, string locale, CancellationToken ct = default);
    }

    public class EumArtist
    {
        public EumArtist(MercuryArtist mercuryUrl)
        {
            Name = mercuryUrl.Info.Name;
            Avatar = mercuryUrl.Info.Portraits
                ?.FirstOrDefault()
                .Uri;
            Header = mercuryUrl.Header;
            DiscographyReleases = mercuryUrl.DiscographyReleases;
            TopTrack = mercuryUrl.TopTracks
                .Select((a,i) => new ArtistTopTrack(a, i));

            LatestRelease = mercuryUrl.LatestRelease != null ?
                new LatestReleaseWrapper(mercuryUrl.LatestRelease) : null;
        }

        public string Name { get; }
        public string? Avatar { get; }
        public string? Header { get; }
        public IDictionary<DiscographyType, IList<DiscographyRelease>> DiscographyReleases { get; set; }
        public IEnumerable<ArtistTopTrack> TopTrack { get; set; }
        public LatestReleaseWrapper? LatestRelease { get; }
    }

    public class LatestReleaseWrapper
    {
        public LatestReleaseWrapper(DiscographyRelease mercuryUrlLatestRelease)
        {
            ImageUrl = mercuryUrlLatestRelease.Cover.Uri;
            Title = mercuryUrlLatestRelease.Name;
            TrackCountString = $"{mercuryUrlLatestRelease.TrackCount} Songs";
            var dt = new DateTime(mercuryUrlLatestRelease.Year,
                mercuryUrlLatestRelease.Month ?? 1,
                mercuryUrlLatestRelease.Day ?? 1);
            if (mercuryUrlLatestRelease.Month.HasValue)
            {
                if (mercuryUrlLatestRelease.Day.HasValue)
                {
                    ReleaseDateString = $"{dt.Day} {dt.ToString("MMMM").ToUpper()} {dt.Year}";
                }
                else
                {
                    ReleaseDateString = $"{dt.ToString("MMMM").ToUpper()} {dt.Year}";
                }
            }
            else
            {
                ReleaseDateString = dt.Year.ToString();
            }
        }

        public string ImageUrl { get; }
        public string ReleaseDateString { get; }
        public string Title { get; }
        public string TrackCountString { get; }
    }

    public class ArtistTopTrack
    {
        public ArtistTopTrack(MercuryArtistTopTrack toptRack, int index)
        {
            Index = index;
            Track = new EumTrack(toptRack);
            Playcount = toptRack.Playcount;
        }

        public EumTrack Track { get; }
        public long Playcount { get; }
        public int Index { get; }
    }
}
