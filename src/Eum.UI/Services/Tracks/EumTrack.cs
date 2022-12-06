using System.Collections.Concurrent;
using Eum.Connections.Spotify.Models.Artists;
using Eum.UI.Items;
using Eum.UI.ViewModels.Playback;

namespace Eum.UI.Services.Tracks
{
    public class EumTrack
    {
        public EumTrack(CachedPlayItem cachedPlayItem)
        {
            Id = new ItemId(cachedPlayItem.Id);
            Name = cachedPlayItem.Name;
            Artists = cachedPlayItem.Artists
                .Select(a => new IdWithTitle
                {
                    Title = a.Name,
                    Id = new ItemId(a.Id)
                }).ToArray();
            Group = new IdWithTitle
            {
                Id = new ItemId(cachedPlayItem.Album.Id),
                Title = cachedPlayItem.Album.Name
            };
            Images = cachedPlayItem.Album.Images;
            Duration = cachedPlayItem.Duration;
        }

        public EumTrack(MercuryArtistTopTrack cachedPlayItem)
        {
            Name = cachedPlayItem.Name;
            Group = new IdWithTitle
            {
                Id = new ItemId(cachedPlayItem.Release.Uri.Uri),
                Title = cachedPlayItem.Release.Name
            };

            Images = new CachedImage[]
            {
                new CachedImage
                {
                    Id = cachedPlayItem.Release.Cover.Uri
                }
            };
        }

        public ItemId Id { get; }
        public string Name { get; }
        public IdWithTitle[] Artists { get; }
        public IdWithTitle Group { get; }
        public CachedImage[] Images { get; }
        public int Duration { get; }
    }
}
