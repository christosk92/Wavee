using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.Interfaces.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Interfaces.Services;

namespace Wavee.UI.Playback.Contexts
{
    public class LocalFilesContext : IPlayContext
    {
        private readonly string? _filter;
        private readonly string _orderBy;
        private readonly int _offset;
        public LocalFilesContext(string orderBy, int offset, string? filter)
        {
            _orderBy = orderBy;
            _offset = offset;
            _filter = filter;
        }
        public IPlayableItem? GetTrack(int index)
        {
            var db = Ioc.Default.GetRequiredService<ILocalAudioDb>();
            //limit -1 offset _offset
            var query =
                _filter == null
                    ? $"{_orderBy} LIMIT -1 OFFSET {_offset + index}"
                    : $"{_filter} {_orderBy} LIMIT 1 OFFSET {_offset + index}";
            var fetch = db.ReadTrack(query);
            return fetch;
        }

        public int Length
        {
            get
            {
                var db = Ioc.Default.GetRequiredService<ILocalAudioDb>();
                var query = $"SELECT COUNT(*) FROM MediaItems {_orderBy}";
                return db.Count(query) - _offset;
            }
        }
    }
}
