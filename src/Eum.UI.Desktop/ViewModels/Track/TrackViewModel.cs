using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.Models.Entities;

namespace Eum.UI.ViewModels.Track
{
    public class TrackViewModel
    {
        public string Title { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Image { get; set; }
        public TrackDetailRef[] Artists { get; set; }
        public TrackDetailRef Album { get; set; }
        public bool HasImage => !string.IsNullOrEmpty(Image);
        public Guid Id { get; set; }

        public static TrackViewModel Create(TrackCacheEntity cacheEntity)
        {
            return new TrackViewModel
            {
                Title = cacheEntity.Title,
                Album = cacheEntity.Album,
                Artists = cacheEntity.Artists,
                Duration = cacheEntity.Duration,
                Id = cacheEntity.Id,
                Image = cacheEntity.Image
            };
        }
    }
}
