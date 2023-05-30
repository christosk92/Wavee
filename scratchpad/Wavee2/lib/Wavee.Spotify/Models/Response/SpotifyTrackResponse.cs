using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Models.Response;

public readonly record struct SpotifyTrackResponse(AudioId Id, string Title, Seq<ITrackArtist> Artists,
    ITrackAlbum Album, TimeSpan Duration, bool CanPlay, int TrackNumber) : ITrack
{
    public static SpotifyTrackResponse From(string country, string cdnUrl, Track track)
    {
        cdnUrl ??= "https://i.scdn.co/image/{file_id}";

        return new SpotifyTrackResponse(
            Id: AudioId.FromRaw(track.Gid.Span, AudioItemType.Track, ServiceType.Spotify),
            Title: track.Name,
            Artists: track.ArtistWithRole.Select(SpotifyTrackArtist.From).Cast<ITrackArtist>().ToSeq(),
            Album: SpotifyTrackAlbum.From(cdnUrl, track.Album, track.DiscNumber),
            Duration: TimeSpan.FromMilliseconds(track.Duration),
            CanPlay: CheckCanPlay(country, track),
            TrackNumber: track.Number
        );
    }

    private static bool CheckCanPlay(string country, Track track)
    {
        if (!track.File.Any())
        {
            var alternative = track.Alternative.FirstOrDefault();
            if (alternative is null) 
                return false;
            track = alternative;
        }
        foreach (var restriction in track.Restriction)
        {
            if (restriction.HasCountriesAllowed)
            {
                // A restriction will specify either a whitelast *or* a blacklist,
                // but not both. So restrict availability if there is a whitelist
                // and the country isn't on it.
                ReadOnlySpan<string> splitted = restriction.CountriesAllowed.Split(",");
                if (!splitted.Contains(country))
                {
                    return false;
                }
            }

            if (restriction.HasCountriesForbidden)
            {
                // A restriction will specify either a whitelast *or* a blacklist,
                // but not both. So restrict availability if there is a blacklist
                // and the country is on it.
                ReadOnlySpan<string> splitted = restriction.CountriesForbidden.Split(",");
                if (splitted.Contains(country))
                {
                    return false;
                }
            }
        }

        if (track.Availability.Count == 0) return true;
        foreach (var availability in track.Availability)
        {
            if (DateTimeOffset.UtcNow >= new DateTimeOffset(new DateTime(
                year: availability.Start.Year,
                month: availability.Start.Month,
                day: availability.Start.Day,
                hour: availability.Start.Hour,
                minute: availability.Start.Minute,
                second: 59)))
            {
                return false;
            }
        }

        return true;
    }
}