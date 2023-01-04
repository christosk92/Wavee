using System.Collections.Concurrent;
using Eum.Connections.Spotify.Models.Artists;
using Eum.UI.Items;
using Eum.UI.ViewModels.Playback;
using Eum.UI.ViewModels.Search.SearchItems;

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

            Id = new ItemId(cachedPlayItem.Uri.Uri);
        }

        public EumTrack(SpotifyTrackSearchItem searchTrackItem)
        {
            Name = searchTrackItem.Name;
            Group = new IdWithTitle
            {
                Id = new ItemId(searchTrackItem.Track.Album.Uri.Uri),
                Title = searchTrackItem.Track.Album.Name
            };

            Artists = searchTrackItem.Track.Artists
                .Select(a => new IdWithTitle
                {
                    Id = new ItemId(a.Uri.Uri),
                    Title = a.Name
                }).ToArray();
            Images = new CachedImage[]
            {
                new CachedImage
                {
                    Id = searchTrackItem.Track.Image
                }
            };
            Duration = (int) searchTrackItem.Track.Duration;
            Id = searchTrackItem.Id;
        }

        public ItemId Id { get; }
        public string Name { get; }
        public IdWithTitle[] Artists { get; }
        public IdWithTitle Group { get; }
        public CachedImage[] Images { get; }
        public int Duration { get; }
    }
}
