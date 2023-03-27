using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.Interfaces.Models;
using Wavee.Playback.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.ViewModels.Libray;

namespace Wavee.UI.Playback.Contexts
{
    public class LocalFilesContext : IPlayContext
    {
        public string? Filter
        {
            get;
            init;
        }
        public SortOption OrderBy
        {
            get;
            init;
        }
        public bool AscendingSort
        {
            get;
            init;
        }

        public LocalFilesContext(
            SortOption sortOption,
            bool ascendingSort,
            string? filter)
        {
            Filter = filter;
            OrderBy = sortOption;
            AscendingSort = ascendingSort;
        }

        public IPlayableItem? GetTrack(int index)
        {
            var filterQuery = BuildFilterQuery();
            const string fromClause = "FROM MediaItems mi LEFT JOIN Playcount pc ON mi.Id = pc.TrackId ";
            var orderQuery = BuildOrderQuery();
            var limitOffsetQuery = $"LIMIT 1 OFFSET {index}";

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

                var countQuery = $"SELECT COUNT(*) OVER () {fromClause}{filterQuery} GROUP BY mi.Id LIMIT 1";
                return Ioc.Default.GetRequiredService<ILocalAudioDb>()
                    .Count(countQuery);
            }
        }

        private string BuildFilterQuery()
        {
            return string.IsNullOrEmpty(Filter) ? string.Empty : $"WHERE {Filter}";
        }

        private string BuildOrderQuery()
        {
            var orderMapping = OrderBy switch
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

            var orderQueryAppend = AscendingSort ? "ASC" : "DESC";
            return $"ORDER BY {orderMapping} {orderQueryAppend}, mi.Id {orderQueryAppend}";
        }

        protected bool Equals(LocalFilesContext other) => Filter == other.Filter && OrderBy == other.OrderBy && AscendingSort == other.AscendingSort;

        public bool Equals(IPlayContext? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((LocalFilesContext)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Filter, (int)OrderBy, AscendingSort);

        public static bool operator ==(LocalFilesContext? left, LocalFilesContext? right) => Equals(left, right);

        public static bool operator !=(LocalFilesContext? left, LocalFilesContext? right) => !Equals(left, right);
    }
}