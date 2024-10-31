using System.Text.Json;
using System.Text.Json.Serialization;
using Wavee.Interfaces;

namespace Wavee.Models.Remote.Commands.Play;

internal sealed partial class PlayItemCommand : ISpotifyRemoteCommand
{
    private readonly string _contextId;
    private readonly Dictionary<string, string> _contextMetadata;
    private readonly string _referrerIdentifier;
    private readonly string _featureIdentifier;
    private readonly string _featureVersion;
    private readonly int? _trackIndex;
    private readonly string? _trackUri;
    private readonly string? _trackUid;

    public PlayItemCommand(string contextId,
        Dictionary<string, string> contextMetadata,
        string referrerIdentifier,
        string featureIdentifier,
        string featureVersion, int? trackIndex, string? trackUri, string? trackUid)
    {
        _contextId = contextId;
        _contextMetadata = contextMetadata;
        _referrerIdentifier = referrerIdentifier;
        _featureIdentifier = featureIdentifier;
        _featureVersion = featureVersion;
        _trackIndex = trackIndex;
        _trackUri = trackUri;
        _trackUid = trackUid;
    }

    public string ToJson()
    {
        var command = new PlayItemCommandBodyRoot
        {
            Command = new PlayItemCommandBody
            {
                Context = new PlayItemContext
                {
                    Uri = _contextId,
                    Url = "context://" + _contextId,
                    Metadata = _contextMetadata
                },
                PlayOrigin = new PlayItemPlayOrigin
                {
                    ReferrerIdentifier = _referrerIdentifier,
                    FeatureIdentifier = _featureIdentifier,
                    FeatureVersion = _featureVersion
                },
                Options = new PlayItemOptions
                {
                    License = "premium",
                    PlayerOptionsOverride = new PlayItemPlayerOptionsOverride(),
                    SkipTo = new PlayItemSkipTo
                    {
                        TrackIndex = _trackIndex ?? default,
                        TrackUri = _trackUri ?? default,
                        TrackUid = _trackUid ?? default
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(command, DefaultJsonOptions);
        return json;
    }

    public string Describe()
    {
        return $"Play item {_contextId}";
    }

    private static JsonSerializerOptions DefaultJsonOptions { get; } = new JsonSerializerOptions
    {
        TypeInfoResolver = PlayItemCommandSerializerContext.Default
    };

    [JsonSerializable(typeof(PlayItemCommandBodyRoot))]
    [JsonSerializable(typeof(PlayItemCommandBody))]
    [JsonSerializable(typeof(PlayItemContext))]
    [JsonSerializable(typeof(PlayItemOptions))]
    [JsonSerializable(typeof(PlayItemSkipTo))]
    [JsonSerializable(typeof(PlayItemPlayerOptionsOverride))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(PlayItemPlayOrigin))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    private partial class PlayItemCommandSerializerContext : JsonSerializerContext
    {
    }

    private sealed class PlayItemCommandBodyRoot
    {
        [JsonPropertyName("command")] public PlayItemCommandBody Command { get; init; }
    }

    private sealed class PlayItemCommandBody
    {
        [JsonPropertyName("context")] public PlayItemContext Context { get; init; }
        [JsonPropertyName("play_origin")] public PlayItemPlayOrigin PlayOrigin { get; init; }
        [JsonPropertyName("options")] public PlayItemOptions Options { get; init; }
        [JsonPropertyName("endpoint")] public string Endpoint { get; init; } = "play";
    }

    private sealed class PlayItemPlayOrigin
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("feature_identifier")]
        public string FeatureIdentifier { get; init; }

        [JsonPropertyName("feature_version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string FeatureVersion { get; init; }

        [JsonPropertyName("referrer_identifier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ReferrerIdentifier { get; init; }
    }

    private sealed class PlayItemContext
    {
        [JsonPropertyName("uri")] public string Uri { get; init; }
        [JsonPropertyName("url")] public string Url { get; init; }
        [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; init; }
    }

    private sealed class PlayItemOptions
    {
        [JsonPropertyName("license")] public string License { get; init; } = "premium";
        [JsonPropertyName("skip_to")] public PlayItemSkipTo SkipTo { get; init; }

        [JsonPropertyName("player_options_override")]
        public PlayItemPlayerOptionsOverride PlayerOptionsOverride { get; init; }
    }

    private sealed class PlayItemSkipTo
    {
        [JsonPropertyName("track_uid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string TrackUid { get; init; }

        [JsonPropertyName("track_index")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int TrackIndex { get; init; }

        [JsonPropertyName("track_uri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string TrackUri { get; init; }
    }

    private sealed class PlayItemPlayerOptionsOverride
    {
    }
}