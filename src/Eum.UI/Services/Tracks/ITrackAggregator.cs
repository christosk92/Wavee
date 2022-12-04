using Eum.Spotify.metadata;
using Eum.UI.Items;

namespace Eum.UI.Services.Tracks
{
    public interface ITrackAggregator
    {
        Task<IEnumerable<EumTrack>> GetTracks(ItemId[] ids, CancellationToken ct = default);
        ValueTask<EumTrack> GetTrack(ItemId itemId,
            CancellationToken ct = default);
    }
}
