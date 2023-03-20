using CommunityToolkit.Common.Collections;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Local;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.Models.TrackSources;
public sealed class SqlTracksSource : AbsTrackSource<TrackViewModel>
{
    private readonly ILocalAudioDb _db;
    public SqlTracksSource(ILocalAudioDb db)
    {
        _db = db;
    }
    public async override Task<IEnumerable<TrackViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var skip = pageIndex * pageSize;

        return await HandleLocalServiceTracks(skip, pageSize, cancellationToken);
    }

    private async Task<IEnumerable<LibraryTrackViewModel>>
        HandleLocalServiceTracks(
            int offset,
            int limit,
            CancellationToken ct = default)
    {
        try
        {

            var query = BuildSqlQuery(SortBy, Ascending, HeartedFilter, offset, limit);
            var tracks = (await _db.ReadTracks(query, true, ct)).ToArray();

            //any other sort option will use the default sort option as the group string

            return tracks.Select((t, i) =>
            {
                //get the extra string data depending on the sort option
                var extraStringData = SortBy switch
                {
                    SortOption.Year => t.Year.ToString(),
                    SortOption.Genre => string.Join(",", t.Genres),
                    SortOption.Playcount => t.Playcount.ToString(),
                    SortOption.LastPlayed => t.LastPlayed.ToString("o"),
                    SortOption.BPM => t.BeatsPerMinute.ToString(),
                    _ => t.DateImported.ToString("o")
                };

                return new LibraryTrackViewModel(t, i + offset, extraStringData);
            });
        }
        catch (Exception x)
        {
            return Enumerable.Empty<LibraryTrackViewModel>();
        }
    }

    private static string BuildSqlQuery(SortOption sortBy, bool ascending, bool onlyHearted, int offset, int limit)
    {
        //the db handles the SELECT FROM statement
        //we just need to build the ORDER BY statement
        //if onlyHearted = true, then we need to add a WHERE statement based on the users hearted tracks

        //if we are sorting by playcount or last played, we need to get the playcount and last played data from the playcount service
        // so we need to lookup the playcount table based on the track id
        //we can then use the playcount and last played data in the ORDER BY statement

        var savedTracks = ShellViewModel.Instance.User.ForProfile.SavedTracks;
        const string where = "WHERE ";
        var filterQuery = where +
                          (onlyHearted ? $"{nameof(LocalTrack.Id)} IN ({string.Join(",", savedTracks)})" : "1=1");

        var orderQueryAppend = ascending ? "ASC" : "DESC";


        const string min = "0001-01-01";

        const string baseQuery =
            $@"SELECT mi.*, COALESCE(pc.Playcount, 0) AS Playcount, COALESCE(pc.LastPlayed, '{min}') AS LastPlayed FROM MediaItems mi LEFT JOIN (SELECT TrackId, COUNT(*) AS Playcount, MAX(DatePlayed) AS LastPlayed FROM Playcount GROUP BY TrackId) pc ON mi.Id = pc.TrackId";


        const string sql = "ORDER BY ";
        var orderQuery = sql + sortBy switch
        {
            SortOption.Year => nameof(LocalTrack.Year),
            SortOption.Genre => nameof(LocalTrack.Genres),
            SortOption.DateAdded => nameof(LocalTrack.DateImported),
            SortOption.Playcount => "COALESCE(pc.Playcount, 0)",
            SortOption.LastPlayed => $"COALESCE(pc.LastPlayed, {min})",
            SortOption.BPM => nameof(LocalTrack.BeatsPerMinute),
            SortOption.Title => nameof(LocalTrack.Title),
            SortOption.Artist => nameof(LocalTrack.Performers),
            SortOption.Album => nameof(LocalTrack.Album),
            SortOption.Duration => nameof(LocalTrack.Duration),
            _ => nameof(LocalTrack.DateImported)
        };

        var total = $"{baseQuery} {filterQuery} {orderQuery} {orderQueryAppend}, {nameof(LocalTrack.Id)} {orderQueryAppend} LIMIT {limit} OFFSET {offset}";
        return total;
    }

}
