using Eum.Connections.Spotify.Models.Albums;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Connections.Spotify.Models.Artists;
using Eum.Connections.Spotify.Models.Users;
using Eum.UI.Items;
using Eum.UI.Services.Artists;
using Eum.UI.Services.Tracks;
using Eum.UI.ViewModels.Artists;
using Eum.Spotify.metadata;
using Eum.UI.ViewModels.Playback;
using MoreLinq.Extensions;

namespace Eum.UI.Services.Albums
{
    public interface IAlbumProvider
    {
        ValueTask<EumAlbum> GetAlbum(ItemId id, string locale, CancellationToken ct = default);
    }

    public class EumDisc : List<DiscographyTrackRelease>
    {
        public EumDisc(IEnumerable<DiscographyTrackRelease> discographyTrackReleases) : base(discographyTrackReleases)
        {
        }

        public int DiscNumber { get; init; }
    }

    public class EumAlbum
    {
        public EumAlbum(MercuryAlbum album, CachedImage[] uploadImages)
        {
            Name = album.Name;
            Discs = album.Discs?
                        .Select((a, i) => new EumDisc(a)
                        {
                            DiscNumber = i
                        })?.ToArray()
                    ?? Array.Empty<EumDisc>();

            Images = uploadImages;
            AlbumType = album.Type;
        }

        public EumAlbum(CachedAlbum cachedAlbum)
        {
            Name = cachedAlbum.Name;
            Images = cachedAlbum.Images;
            Discs = cachedAlbum.Tracks.GroupBy(a => a.DiscNumber)
                .Select(a => new EumDisc(a.Select(a => new DiscographyTrackRelease
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
                }))
                {
                    DiscNumber = a.Key
                }).ToArray();

            IsCache = true;
            AlbumType = cachedAlbum.AlbumType ?? "ALBUM";
        }

        public bool IsCache { get; }
        public string Name { get; }
        public CachedImage[] Images { get; }
        public EumDisc[] Discs { get; }

        public IdWithTitle[] Artists => Discs.SelectMany(a => a.SelectMany(z => z.Artists.Select(k => new IdWithTitle
            {
                Id = new ItemId(k.Uri.Uri),
                Title = k.Name
            })))
            .DistinctBy(a => a.Id.Uri).ToArray();

        public string AlbumType { get; }
    }
}