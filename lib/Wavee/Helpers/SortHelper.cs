using Wavee.Models.Common;
using Wavee.Playback.Player;

namespace Wavee.Helpers;

internal static class SortHelper
{
    public static Func<WaveePlayerMediaItem, IComparable> GetKeySelector(string fieldName)
    {
        return null;
        // switch (fieldName.ToLowerInvariant())
        // {
        //     case "title":
        //         return item => item.Title ?? "";
        //     case "artist_name":
        //         return item => item.ArtistName ?? "";
        //     case "album_title":
        //         return item => item.AlbumTitle ?? "";
        //     case "album_disc_number":
        //         return item => item.AlbumDiscNumber;
        //     case "album_track_number":
        //         return item => item.AlbumTrackNumber;
        //     case "original_index":
        //         return item => item.OriginalIndex;
        //     case "added_at":
        //         return item => item.AddedAt;
        //     case "duration":
        //         return item => item.Duration ?? TimeSpan.Zero;
        //     default:
        //         return null;
        // }
    }

    public static string RemoveLeadingArticles(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        string[] articles = { "the ", "a ", "an " };
        foreach (var article in articles)
        {
            if (str.StartsWith(article, StringComparison.OrdinalIgnoreCase))
            {
                return str.Substring(article.Length);
            }
        }

        return str;
    }

    public static void SortTracks(List<WaveePlayerMediaItem> tracks, List<SortDescriptor> sortDescriptors)
    {
        IOrderedEnumerable<WaveePlayerMediaItem> orderedEnumerable = null;

        var tracksInstance = tracks.ToList();
        foreach (var sortDescriptor in sortDescriptors)
        {
            var keySelector = sortDescriptor.KeySelector;

            if (orderedEnumerable == null)
            {
                orderedEnumerable = sortDescriptor.Descending
                    ? tracksInstance.OrderByDescending(keySelector)
                    : tracksInstance.OrderBy(keySelector);
            }
            else
            {
                orderedEnumerable = sortDescriptor.Descending
                    ? orderedEnumerable.ThenByDescending(keySelector)
                    : orderedEnumerable.ThenBy(keySelector);
            }
        }

        if (orderedEnumerable != null)
        {
            tracks.Clear();
            var list = orderedEnumerable.ToList();
            tracks.AddRange(list);
        }
    }
}