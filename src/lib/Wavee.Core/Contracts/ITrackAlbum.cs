using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Core.Contracts;

/// <summary>
/// The contract representing an album for a track across all sources.
/// This is different from <see cref="IAlbum"/> in the sense that this contract only represents an album for a track and does not represent the album as a whole.
/// </summary>
public interface ITrackAlbum
{
    /// <summary>
    /// The unique identifier for the album.
    /// </summary>
    AudioId Id { get; }

    /// <summary>
    /// The title of the album.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// An array of artwork for the album.
    /// </summary>
    Seq<Artwork> Artwork { get; }

    /// <summary>
    /// The release date of the album.
    /// For the precision of the release date, see <see cref="ReleaseDatePrecision"/>.
    /// </summary>
    DateOnly ReleaseDate { get; }

    /// <summary>
    /// The precision of the release date of the album.
    /// If the precision is <see cref="ReleaseDatePrecisionType.Year"/>, the release date will only contain the year and the month and day will be set to 1.
    /// </summary>
    ReleaseDatePrecisionType ReleaseDatePrecision { get; }
}

/// <summary>
/// An enum representing the precision of the release date of an album.
/// </summary>
public enum ReleaseDatePrecisionType
{
    /// <summary>
    /// The release date precision is known to only the year.
    /// </summary>
    Year,

    /// <summary>
    /// The release date precision is known to only the year and the month.
    /// </summary>
    Month,

    /// <summary>
    /// The release date precision is known to the year, month and day.
    /// </summary>
    Day,

    /// <summary>
    /// No release date is known.
    /// </summary>
    Unknown
}