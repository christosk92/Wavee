using LanguageExt.Effects.Database;
using LinqToDB.Mapping;

namespace Wavee.Spotify.Cache.Entities;

[Table("EncryptedFiles")]
public record FileEntity(
    [property: Column(IsPrimaryKey = true)]
    string Id,
    [property: Column] byte[] CompressedData,
    [property: Column] int FormatType,
    [property: Column] DateTimeOffset CreatedAt
) : IEntity<string>;