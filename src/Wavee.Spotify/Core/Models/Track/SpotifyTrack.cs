using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Core.Models.Common;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Track;

public readonly struct SpotifyTrack : ISpotifyPlayableItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public required SpotifyTrackAlbum Album { get; init; }
    public required IReadOnlyCollection<SpotifyTrackArtist> Artists { get; init; }
    public required uint Number { get; init; }
    public required TimeSpan Duration { get; init; }
    public required uint DiscNumber { get; init; }
    public required bool Explicit { get; init; }
    public required SpotifyTrackRestrictions Restrictions { get; init; }
    public required ImmutableArray<SpotifyAudioFile> AudioFiles { get; init; }
    public required SpotifySalePeriods SalePeriod { get; init; }
    public required ImmutableArray<SpotifyAudioFile> PreviewFiles { get; init; }
    public required ImmutableArray<string> Tags { get; init; }
    public required DateTimeOffset? EarliestLiveTime { get; init; }
    public required bool HasLyrics { get; init; }
    public required SpotifyTrackAvailabilities Availability { get; init; }
    public required string? Licensor { get; init; }
    public required ImmutableArray<string> LanguageOfPerformance { get; init; }
    public required SpotifyTrackRatings Rating { get; init; }
    public required string OriginalTitle { get; init; }
    public required string VersionTitle { get; init; }
}

public sealed class SpotifyTrackRatings
{
    public required ImmutableArray<SpotifyTrackRating> SalePeriods { get; init; }
}

public sealed class SpotifyTrackRating
{
    public required string Country { get; init; }
    public required ImmutableArray<string> Tags { get; init; }
}

public sealed class SpotifyTrackAvailabilities
{ 
    public required ImmutableArray<SpotifyTrackAvailability> SalePeriods { get; init; }
}

public sealed class SpotifyTrackAvailability
{
    public required ImmutableArray<Restriction.Types.Catalogue> Catalogues { get; init; }
    public required DateTimeOffset? Start { get; init; }
}

public sealed class SpotifySalePeriods
{
    public required ImmutableArray<SpotifySalePeriod> SalePeriods { get; init; }
}
public sealed class SpotifySalePeriod
{
    public required ImmutableArray<SpotifyTrackRestriction> Restrictions { get; init; }
    public required DateTimeOffset? Start { get; init; }
    public required DateTimeOffset? End { get; init; }
}

public readonly struct SpotifyAudioFile
{
    public required SpotifyAudioFileFormat Format { get; init; }
    public required string FileIdBase16 { get; init; }
}

public readonly struct SpotifyTrackAlbum
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required ImmutableArray<UrlImage> Images { get; init; }
}

public readonly struct SpotifyTrackArtist
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required ArtistWithRole.Types.ArtistRole Role { get; init; }
}

public readonly struct SpotifyTrackRestrictions
{
    public required ImmutableArray<SpotifyTrackRestriction> Restrictions { get; init; }
}
public readonly struct SpotifyTrackRestriction
{
    public required ImmutableArray<Restriction.Types.Catalogue> Catalogues { get; init; }
    public required ImmutableArray<string> AllowedCountries { get; init; }
    public required ImmutableArray<string> DisallowedCountries { get; init; }
}