using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Id;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Infrastructure;

namespace Wavee.Spotify.Models.Responses;

internal readonly record struct SpotifyTrackResponse(AudioId Id, string Title, Seq<ITrackArtist> Artists,
    ITrackAlbum Album, TimeSpan Duration, bool CanPlay) : ITrack
{
    public static SpotifyTrackResponse From(string country, string cdnUrl, Track track)
    {
        return new SpotifyTrackResponse(
            Id: track.ToId(),
            Title: track.Name,
            Artists: track.ArtistWithRole.Select(SpotifyTrackArtist.From).Cast<ITrackArtist>().ToSeq(),
            Album: SpotifyTrackAlbum.From(cdnUrl, track.Album),
            Duration: TimeSpan.FromMilliseconds(track.Duration),
            CanPlay: CheckCanPlay(country, track)
        );
    }

    private static bool CheckCanPlay(string country, Track track)
    {
        return false;
    }
}