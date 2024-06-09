using System;
using System.Linq;
using Spotify.Metadata;
using Wavee.UI.Spotify.Common;

namespace Wavee.UI.Spotify.Responses.Parsers;

internal static class TrackParsers
{
    public static SpotifySimpleTrack ToTrack(this Track track)
    {
        var album = track.Album.ToSimpleAlbum();
        var id = RegularSpotifyId.FromRaw(track.Gid.Span, SpotifyIdItemType.Track).AsString;
        var name = track.Name;
        var mainContributor = track.Artist.FirstOrDefault().ToContributor();
        var duration = track.Duration;
        var files = track.File.Select(x => new SpotifyAudioFile(x)).ToArray();
        var item = new SpotifySimpleTrack(id, name, mainContributor, TimeSpan.FromMilliseconds(duration), album, files);

        return item;
    }
}