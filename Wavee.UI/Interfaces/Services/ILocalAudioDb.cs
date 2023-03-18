using System.Collections;
using Wavee.UI.Models.Local;

namespace Wavee.UI.Interfaces.Services
{
    public interface ILocalAudioDb
    {
        Task<IEnumerable<LocalTrack>> GetLatestImportsAsync(int count, CancellationToken ct = default);
        Task InsertTrackAsync(LocalTrack item, CancellationToken ct = default);
        Task UpdateTrackAsync(LocalTrack localTrack, CancellationToken ct = default);
        bool[] CheckIfAudioFilesExist(IList<string> paths);
        Task<IEnumerable<ShortLocalTrack>> GetAllForUpdateCheck();
        Task Remove(string id);
        LocalTrack? ReadTrack(string sql);
        Task<IEnumerable<LocalTrack>> ReadTracks(string sql,
            bool withJoinOnPlaycount,
            CancellationToken ct = default);
        int Count(string sql);
        Task<int> Count();
    }
}
