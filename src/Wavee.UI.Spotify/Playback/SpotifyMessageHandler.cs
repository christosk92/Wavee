using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify.connectstate;
using Wavee.UI.Spotify.Common;

namespace Wavee.UI.Spotify.Playback;

internal sealed partial class SpotifyMessageHandler : ISpotifyMessageHandler
{
    private readonly BehaviorSubject<Cluster?> _cluster = new(null);
    private readonly Dictionary<Regex, Action<JsonElement, Dictionary<string, string>>> _uriActions;

    public const string InternalClusterUpdate = "wv://internal-cluster-update/v1/cluster";

    public SpotifyMessageHandler()
    {
        _uriActions = new Dictionary<Regex, Action<JsonElement, Dictionary<string, string>>>
        {
            { InternalClusterRegex(), HandleClusterInternal },
            { MyRegex(), HandleCluster },
            { MyRegex1(), HandleCollection },
            { MyRegex2(), HandleArtist },
            { MyRegex3(), HandleArtistBan },
            { MyRegex4(), HandleListenLater },
            { MyRegex5(), HandleShow },
            { MyRegex6(), HandleUserPlaylist },
            { MyRegex7(), HandlePlaylist },
            { MyRegex8(), HandleUserProfileChanged },
            { MyRegex9(), HandleUserAttributes },
            { MyRegex10(), HandleProductStateUpdate },
            { MyRegex11(), HandleBroadcastStatusUpdate }
        };
        ;
    }

    private void HandleBroadcastStatusUpdate(JsonElement arg1, Dictionary<string, string> arg2)
    {
        //TODO: What is this?
        /*{
    "headers": {
        "Content-Type": "application/json"
    },
    "payloads": [
        {
            "deviceBroadcastStatus": {
                "timestamp": "1717892141030",
                "broadcast_status": "BROADCAST_UNAVAILABLE",
                "device_id": "896e7c2a4d96260dcaac932ed895fc963b74e0a9",
                "output_device_info": {
                    "output_device_type": "UNKNOWN_OUTPUT_DEVICE_TYPE",
                    "device_name": "Headphones (Galaxy Buds2 Pro)"
                }
            }
        }
    ],
    "type": "message",
    "uri": "social-connect/v2/broadcast_status_update"
}*/
    }

    private void HandleClusterInternal(JsonElement arg1, Dictionary<string, string> arg2)
    {
        ReadOnlySpan<byte> bytes = arg1.GetProperty("data").GetBytesFromBase64();
        var cluster = Eum.Spotify.connectstate.Cluster.Parser.ParseFrom(bytes);
        _cluster.OnNext(cluster);
    }

    private void HandleCluster(JsonElement root, Dictionary<string, string> messageHeaders)
    {
        var payload = SpotifyWsUtils.ReadPayload(root, messageHeaders);
        var clusterUpdate = ClusterUpdate.Parser.ParseFrom(payload.Span);
        _cluster.OnNext(clusterUpdate.Cluster);
    }

    private void HandleCollection(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleArtist(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleArtistBan(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleListenLater(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleShow(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleUserPlaylist(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandlePlaylist(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleUserProfileChanged(JsonElement arg1, Dictionary<string, string> arg2)
    {
        /*
         * ValueKind = Object : "{"headers":{"Content-Type":"application/octet-stream"},"payloads":["ChsKGTd1Y2doZGdxdWY2YnlxdXNxa2xpbHR3YzISBwoFQ2hyaXMaRghAEEAaQGh0dHBzOi8vaS5zY2RuLmNvL2ltYWdlL2FiNjc3NTcwMDAwMDNiODJkM2IyMzdlN2NiNGI4MTcwY2IwMmE2NzEaSAisAhCsAhpAaHR0cHM6Ly9pLnNjZG4uY28vaW1hZ2UvYWI2Nzc1NzAwMDAwZWU4NWQzYjIzN2U3Y2I0YjgxNzBjYjAyYTY3MSIAKgAyADoAQgBKAggBUgIIAVoECIzNZ2IAigEAkgEAmgEAogEA"],"type":"message","uri":"hm://identity/user-profile-changed"}"
         */
        var payload = SpotifyWsUtils.ReadPayload(arg1, arg2);
        
    }

    private void HandleUserAttributes(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleProductStateUpdate(JsonElement arg1, Dictionary<string, string> arg2)
    {
        throw new NotImplementedException();
    }

    public void HandleUri(string uri, JsonElement root, Dictionary<string, string> messageHeaders)
    {
        foreach (var kvp in _uriActions)
        {
            if (kvp.Key.IsMatch(uri))
            {
                kvp.Value(root, messageHeaders);
                return;
            }
        }

        // Unhandled message
        Console.WriteLine($"Unhandled message: {uri}");
        Debug.WriteLine($"Unhandled message: {uri}");
        Debugger.Break();
    }


    public IObservable<Cluster> Cluster => _cluster.Where(x => x is not null).Select(x => x!);


    //InternalClusterUpdate
    [GeneratedRegex(InternalClusterUpdate)]
    private static partial Regex InternalClusterRegex();

    [GeneratedRegex(@"^hm://connect-state/v1/cluster$")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"^hm://collection/collection/")]
    private static partial Regex MyRegex1();

    [GeneratedRegex(@"^hm://collection/artist/")]
    private static partial Regex MyRegex2();

    [GeneratedRegex(@"^hm://collection/artistban/")]
    private static partial Regex MyRegex3();

    [GeneratedRegex(@"^hm://collection/listenlater")]
    private static partial Regex MyRegex4();

    [GeneratedRegex(@"^hm://collection/show")]
    private static partial Regex MyRegex5();

    [GeneratedRegex(@"^hm://playlist/v2/user/.+/rootlist$")]
    private static partial Regex MyRegex6();

    [GeneratedRegex(@"^hm://playlist/v2/playlist/")]
    private static partial Regex MyRegex7();

    [GeneratedRegex(@"^hm://identity/user-profile-changed$")]
    private static partial Regex MyRegex8();

    [GeneratedRegex(@"^spotify:user:attributes:mutated$")]
    private static partial Regex MyRegex9();

    [GeneratedRegex(@"^ap://product-state-update$")]
    private static partial Regex MyRegex10();


    [GeneratedRegex(@"^social-connect/v2/broadcast_status_update$")]
    private static partial Regex MyRegex11();

    public void Dispose()
    {
        _cluster?.Dispose();
    }
}