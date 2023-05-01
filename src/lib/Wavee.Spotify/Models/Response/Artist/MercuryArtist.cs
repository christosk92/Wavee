using System.Text.Json.Serialization;

namespace Wavee.Spotify.Models.Response.Artist;

public readonly record struct MercuryArtist(
    [property: JsonPropertyName("header_image")]
    MercuryHeaderImage? HeaderImage,
    MercuryArtistShortInfo Info
);

public readonly record struct MercuryArtistShortInfo(string Name);

public readonly record struct MercuryHeaderImage(string Image);