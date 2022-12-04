using Eum.Connections.Spotify.Models.Artists;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.UI.Items;

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
        }

        public string Name { get; }
        public string? Avatar { get; }
        public string? Header { get; }
        public IDictionary<DiscographyType, IList<DiscographyRelease>> DiscographyReleases { get; set; }
    }
}
