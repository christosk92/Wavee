using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.Interfaces.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Local;
using Wavee.UI.ViewModels.Libray;

namespace Wavee.UI.Playback.Contexts
{
    public class LocalFilesContext : IPlayContext
    {
        private readonly string? _filter;
        private readonly SortOption _orderBy;
        private readonly bool _ascendingSort;
        private readonly int _offset;

        public LocalFilesContext(
            SortOption sortOption,
            bool ascendingSort,
            int offset,
            string? filter)
        {
            _filter = filter;
            _offset = offset;
            _orderBy = sortOption;
            _ascendingSort = ascendingSort;
        }

        public IPlayableItem? GetTrack(int index)
        {
            var filterQuery = BuildFilterQuery();
            const string fromClause = "FROM MediaItems mi LEFT JOIN Playcount pc ON mi.Id = pc.TrackId ";
            var orderQuery = BuildOrderQuery();
            var limitOffsetQuery = $"LIMIT 1 OFFSET {index + _offset}";

            const string selectClause = "SELECT mi.*, COUNT(pc.TrackId) AS Playcount, MAX(pc.DatePlayed) AS LastPlayed ";


            var finalQuery = $"{selectClause}{fromClause}{filterQuery} GROUP BY mi.Id {orderQuery} {limitOffsetQuery}";

            return Ioc.Default.GetRequiredService<ILocalAudioDb>()
                .ReadTrack(finalQuery);
        }

        public int Length
        {
            get
            {
                var filterQuery = BuildFilterQuery();
                const string fromClause = "FROM MediaItems mi LEFT JOIN Playcount pc ON mi.Id = pc.TrackId ";

                var countQuery = $"SELECT COUNT(*) {fromClause}{filterQuery}";
                return Ioc.Default.GetRequiredService<ILocalAudioDb>()
                    .Count(countQuery);
            }
        }

        private string BuildFilterQuery()
        {
            return string.IsNullOrEmpty(_filter) ? string.Empty : $"WHERE {_filter}";
        }

        private string BuildOrderQuery()
        {
            var orderMapping = _orderBy switch
            {
                SortOption.Year => "mi." + nameof(LocalTrack.Year),
                SortOption.Genre => "mi." + nameof(LocalTrack.Genres),
                SortOption.DateAdded => "mi." + nameof(LocalTrack.DateImported),
                SortOption.Playcount => "Playcount",
                SortOption.LastPlayed => "LastPlayed",
                SortOption.BPM => "mi." + nameof(LocalTrack.BeatsPerMinute),
                SortOption.Title => "mi." + nameof(LocalTrack.Title),
                SortOption.Artist => "mi." + nameof(LocalTrack.Performers),
                SortOption.Album => "mi." + nameof(LocalTrack.Album),
                SortOption.Duration => "mi." + nameof(LocalTrack.Duration),
                _ => "mi." + nameof(LocalTrack.DateImported)
            };

            var orderQueryAppend = _ascendingSort ? "ASC" : "DESC";
            return $"ORDER BY {orderMapping} {orderQueryAppend}, mi.Id {orderQueryAppend}";
        }
    }
}