using Eum.UI.Models.Entities;
using LiteDB;

namespace Eum.UI.Database
{
    public class TracksRepository
    {
        private readonly ILiteCollection<TrackCacheEntity> _tracks;
        public TracksRepository(ILiteDatabase liteDb)
        {
            _tracks = liteDb.GetCollection<TrackCacheEntity>("tracks");
        }


        public IEnumerable<TrackCacheEntity> GetTracks(IEnumerable<Guid> ids)
        {
            return _tracks
                .Find(a => ids.Contains(a.Id));
        }

        public void InsertTrack(TrackCacheEntity track)
        {
            _tracks.Insert(track);
        }
        public void UpsertTrack(TrackCacheEntity track)
        {
            _tracks.Upsert(track);
        }
    }
}
