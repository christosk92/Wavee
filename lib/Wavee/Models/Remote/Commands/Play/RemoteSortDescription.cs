namespace Wavee.Models.Remote.Commands.Play
{
    /// <summary>
    /// Describes a single sorting criterion with associated metadata.
    /// </summary>
    public struct SortDescription
    {
        /// <summary>
        /// The "list_util_sort" value corresponding to the sort.
        /// </summary>
        public string ListUtilSort { get; }

        /// <summary>
        /// The "sorting.criteria" value corresponding to the sort.
        /// </summary>
        public string SortingCriteria { get; }

        // Private constructor to prevent external instantiation
        private SortDescription(string listUtilSort, string sortingCriteria)
        {
            ListUtilSort = listUtilSort;
            SortingCriteria = sortingCriteria;
        }

        // Predefined sorting options for Liked Songs

        /// <summary>
        /// Sort by Title Ascending.
        /// </summary>
        public static readonly SortDescription Title = new SortDescription("name ASC", "title");

        /// <summary>
        /// Sort by Title Descending.
        /// </summary>
        public static readonly SortDescription TitleDescending = new SortDescription("name DESC", "title DESC");

        /// <summary>
        /// Sort by Artist Ascending.
        /// </summary>
        public static readonly SortDescription Artist = new SortDescription("artist.name ASC,album.name,discNumber,trackNumber", "artist_name,album_title,album_disc_number,album_track_number");

        /// <summary>
        /// Sort by Artist Descending.
        /// </summary>
        public static readonly SortDescription ArtistDescending = new SortDescription("artist.name DESC,album.name,discNumber,trackNumber", "artist_name DESC,album_title,album_disc_number,album_track_number");

        /// <summary>
        /// Sort by Album Ascending.
        /// </summary>
        public static readonly SortDescription Album = new SortDescription("album.name ASC,discNumber,trackNumber", "album_title,album_disc_number,album_track_number");

        /// <summary>
        /// Sort by Album Descending.
        /// </summary>
        public static readonly SortDescription AlbumDescending = new SortDescription("album.name DESC,discNumber,trackNumber", "album_title DESC,album_disc_number,album_track_number");

        /// <summary>
        /// Sort by Date Added Descending (default).
        /// </summary>
        public static readonly SortDescription DateAdded = new SortDescription("addTime DESC,album.name,album.artist.name,discNumber,trackNumber", "added_at DESC,album_title,album_artist_name,album_disc_number,album_track_number");

        /// <summary>
        /// Sort by Date Added Ascending.
        /// </summary>
        public static readonly SortDescription DateAddedAscending = new SortDescription("addTime ASC,album.name,album.artist.name,discNumber,trackNumber", "added_at,album_title,album_artist_name,album_disc_number,album_track_number");
    }
}
