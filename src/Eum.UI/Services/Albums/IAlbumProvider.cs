using Eum.Connections.Spotify.Models.Albums;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Connections.Spotify.Models.Artists;
using Eum.Connections.Spotify.Models.Users;
using Eum.UI.Items;
using Eum.UI.Services.Artists;
using Eum.UI.Services.Tracks;
using Eum.UI.ViewModels.Artists;
using Eum.Spotify.metadata;

namespace Eum.UI.Services.Albums
{
    public interface IAlbumProvider
    {
        ValueTask<EumAlbum> GetAlbum(ItemId id, string locale, CancellationToken ct = default);
    }

    public class EumAlbum
    {
        public EumAlbum(MercuryAlbum album)
        {
            Name = album.Name;
            Tracks = album.Discs?
                         .SelectMany(a => a)
                     ?? Enumerable.Empty<DiscographyTrackRelease>();
        }

        public EumAlbum(CachedAlbum cachedAlbum)
        {
            Name = cachedAlbum.Name;
            Tracks = cachedAlbum.Tracks.Select(a => new DiscographyTrackRelease
            {
                Artists = a.Artists
                    .Select(k => new DiscographyTrackArtist
                    {
                        Name = k.Name,
                        Uri = new SpotifyId(k.Id)
                    }).ToArray(),
                Duration = a.Duration,
                Explicit = bool.Parse(a.ExtraMetadata["explicit"]),
                Name = a.Name,
                Number = 0,
                Playable = bool.Parse(a.ExtraMetadata["playable"]),
                PlayCount = ulong.Parse(a.ExtraMetadata["playcount"]),
                Popularity = int.Parse(a.ExtraMetadata["popularity"]),
                Uri = new SpotifyId(a.Id)
            });
            IsCache = true;
        }
        public bool IsCache { get; }
        public string Name { get; }
        public IEnumerable<DiscographyTrackRelease> Tracks { get; }
    }
}
