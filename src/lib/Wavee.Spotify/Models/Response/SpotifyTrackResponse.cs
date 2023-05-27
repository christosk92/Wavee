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
        return false;
    }
}