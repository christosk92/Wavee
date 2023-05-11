using System.Linq.Expressions;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Effects.Traits;
using Spotify.Metadata;
using Wavee.Spotify.Cache.Domain.Tracks;
using Wavee.Spotify.Cache.Entities;

namespace Wavee.Spotify.Cache.Repositories;

public interface HasTrackRepo<R> : HasDatabase<R>
    where R : struct,
    HasDatabase<R>,
    HasTrackRepo<R>
{
}

internal static class TrackRepo<R>
    where R : struct,
    HasTrackRepo<R>
{
    public static Aff<R, string> Create(NewTrack item)
        =>
            from id in Database<R>.Insert<TrackEntity, string>(From(item.Track, item.Id, item.DateAdded))
            select id;

    public static Aff<R, Option<Track>> FindOne(Expression<Func<TrackEntity, bool>> filter)
        => from track in Database<R>.FindOne<TrackEntity>(filter)
            select track.Map(To);

    private static Track To(TrackEntity track)
    {
        var result = new Track
        {
            Gid = ByteString.FromBase64(track.GidBase64),
            Name = track.Name,
            Artist =
            {
                track.Artists.Split(",").Select(x => Artist.Parser.ParseFrom(ByteString.FromBase64(x)))
            },
            Album = Album.Parser.ParseFrom(ByteString.FromBase64(track.Album)),
            File = {track.Files.Split(",").Select(x => AudioFile.Parser.ParseFrom(ByteString.FromBase64(x)))},
            Alternative = { track.AlternativeFiles.Split(",").Select(x => Track.Parser.ParseFrom(ByteString.FromBase64(x))) },
            
        };
        return result;
    }

    private static TrackEntity From(Track itemTrack, string id, DateTimeOffset createdAt)
    {
        return new TrackEntity(
            Id: id,
            GidBase64: itemTrack.Gid.ToBase64(),
            Name: itemTrack.Name,
            FirstArtistName: itemTrack.Artist.FirstOrDefault()?.Name,
            FirstArtistGid: itemTrack.Artist.FirstOrDefault()?.Gid.ToBase64(),
            Artists: string.Join(",", itemTrack.Artist.Select(x => x.ToByteString().ToBase64())),
            AlbumName: itemTrack.Album.Name,
            AlbumId: itemTrack.Album.Gid.ToBase64(),
            Album: itemTrack.Album.ToByteString().ToBase64(),
            Files: string.Join(",", itemTrack.File.Select(x => x.ToByteString().ToBase64())),
            AlternativeFiles: string.Join(",", itemTrack.Alternative.Select(x => x.ToByteString().ToBase64())),
            CreatedAt: createdAt
        );
    }
}