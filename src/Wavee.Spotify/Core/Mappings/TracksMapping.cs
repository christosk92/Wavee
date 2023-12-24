using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Google.Protobuf.Collections;
using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Spotify.Core.Extension;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Core.Mappings;

internal static class TracksMapping
{
    public static SpotifyTrack MapToDto(this Track track)
    {
        return new SpotifyTrack
        {
            Uri = SpotifyId.FromRaw(track.Gid.Span, AudioItemType.Track),
            Name = track.Name,
            Album = new SpotifyTrackAlbum
            {
                Id = SpotifyId.FromRaw(track.Album.Gid.Span, AudioItemType.Album),
                Name = track.Album.Name,
                Images = track.Album.CoverGroup.MapToDto()
            },
            Artists = track.ArtistWithRole.Select(x => new SpotifyTrackArtist
            {
                Id = SpotifyId.FromRaw(x.ArtistGid.Span, AudioItemType.Artist),
                Name = x.ArtistName,
                Role = x.Role
            }).ToList(),
            Number = (uint)track.Number,
            Duration = TimeSpan.FromMilliseconds(track.Duration),
            DiscNumber = (uint)track.DiscNumber,
            Explicit = track.Explicit,
            Restrictions = track.Restriction.MapToDto(),
            AudioFiles = track.File.MapToDto(track.Alternative),
            SalePeriod = track.SalePeriod.MapToDto(),
            PreviewFiles = track.Preview.MapToDto(track.Alternative),
            Tags = track.Tags.ToImmutableArray(),
            EarliestLiveTime = track.HasEarliestLiveTimestamp
                ? DateTimeOffset.FromUnixTimeMilliseconds(track.EarliestLiveTimestamp)
                : null,
            HasLyrics = track.HasHasLyrics && track.HasLyrics,
            Availability = track.Availability.MapToDto(),
            Licensor = track.Licensor.HasUuid ? track.Licensor.Uuid.Span.ToBase16() : null,
            LanguageOfPerformance = track.LanguageOfPerformance.ToImmutableArray(),
            Rating = track.ContentRating.MapToDto(),
            OriginalTitle = track.OriginalTitle,
            VersionTitle = track.VersionTitle
        };
    }

    public static SpotifyTrackRatings MapToDto(this RepeatedField<ContentRating> rating)
    {
        var result = new SpotifyTrackRating[rating.Count];
        for (var i = 0; i < rating.Count; i++)
        {
            var f = rating[i];
            result[i] = new SpotifyTrackRating
            {
                Country = f.Country,
                Tags = f.Tag.ToImmutableArray(),
            };
        }
        
        return new SpotifyTrackRatings
        {
            SalePeriods = ImmutableCollectionsMarshal.AsImmutableArray(result)
        };
    }

    public static SpotifyTrackRestrictions MapToDto(this RepeatedField<Restriction> restriction)
    {
        var result = new SpotifyTrackRestriction[restriction.Count];
        for (var i = 0; i < restriction.Count; i++)
        {
            var f = restriction[i];
            result[i] = new SpotifyTrackRestriction
            {
                AllowedCountries = default,
                DisallowedCountries = default,
                Catalogues = default
            };
        }
        
        return new SpotifyTrackRestrictions
        {
            Restrictions = ImmutableCollectionsMarshal.AsImmutableArray(result)
        };
    }

    public static ImmutableArray<SpotifyAudioFile> MapToDto(this RepeatedField<AudioFile> file,
        RepeatedField<Track> trackAlternative)
    {
        if(file.Count == 0)
        {
            //use alternative
            foreach(var t in trackAlternative)
            {
                var altres = MapToDto(t.File, t.Alternative);
                if(altres.Length > 0)
                {
                    return altres;
                }
            }
        }
        
        
        var result = new SpotifyAudioFile[file.Count];
        for (var i = 0; i < file.Count; i++)
        {
            var f = file[i];
            result[i] = new SpotifyAudioFile
            {
                Format = (SpotifyAudioFileFormat)(int)f.Format,
                FileIdBase16 = f.FileId.Span.ToBase16()
            };
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(result);
    }

    public static SpotifySalePeriods MapToDto(this RepeatedField<SalePeriod> salePeriod)
    {
        var result = new SpotifySalePeriod[salePeriod.Count];
        for (var i = 0; i < salePeriod.Count; i++)
        {
            var f = salePeriod[i];
            result[i] = new SpotifySalePeriod
            {
                Restrictions = default,
                Start = null,
                End = null
            };
        }
        
        return new SpotifySalePeriods
        {
            SalePeriods = ImmutableCollectionsMarshal.AsImmutableArray(result)
        };
    }

    public static SpotifyTrackAvailabilities MapToDto(this RepeatedField<Availability> availability)
    {
        var result = new SpotifyTrackAvailability[availability.Count];
        for (var i = 0; i < availability.Count; i++)
        {
            var f = availability[i];
            result[i] = new SpotifyTrackAvailability
            {
                Catalogues = default,
                Start = null
            };
        }
        
        return new SpotifyTrackAvailabilities
        {
            SalePeriods = ImmutableCollectionsMarshal.AsImmutableArray(result)
        };
    }
}