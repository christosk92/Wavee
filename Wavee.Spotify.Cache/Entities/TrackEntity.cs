using LanguageExt.Effects.Database;
using LinqToDB.Mapping;

namespace Wavee.Spotify.Cache.Entities;

[Table("Track")]
public record TrackEntity(
    [property: Column(IsPrimaryKey = true)]
    string Id,
    [property: Column] string GidBase64,
    [property: Column] string Name,
    [property: Column] string FirstArtistName,
    [property: Column] string FirstArtistGid,
    [property: Column] string Artists,
    [property: Column] string AlbumName,
    [property: Column] string AlbumId,
    [property: Column] string Album,
    [property: Column] string Files,
    [property: Column] string AlternativeFiles,
    [property: Column] DateTimeOffset CreatedAt) : IEntity<string>;