using System.Text.Json.Serialization;

namespace Wavee.Spotify.Infrastructure.ApResolver;

internal readonly record struct ApResolveData(
    [property: JsonPropertyName("accesspoint")]
    string[] AccessPoint,
    [property: JsonPropertyName("dealer")] 
    string[] Dealer,
    [property: JsonPropertyName("spclient")]
    string[] SpClient);