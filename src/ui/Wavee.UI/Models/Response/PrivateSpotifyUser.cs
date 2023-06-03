using System.Text.Json.Serialization;

namespace Wavee.UI.Models.Response;

internal readonly struct PrivateSpotifyUser
{
    [JsonPropertyName("display_name")]
    public required string DisplayName { get; init; }
    [JsonPropertyName("images")]
    public Artwork[] Images { get; init; }
    [JsonPropertyName("followers")]
    public FollowersObject Followers { get; init; }
}

internal readonly struct FollowersObject
{
    [JsonPropertyName("total")]
    public required int Total { get; init; }
}