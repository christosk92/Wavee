using Eum.Spotify.context;
using Google.Protobuf;
using Wavee.Spfy.Playback;
using Wavee.Spfy.Playback.Contexts;

namespace Wavee.Spfy.Remote;

internal static class Common
{
    public static ComposedKey ConstructComposedKeyForCurrentTrack(SpotifyContextTrack tr,
        SpotifyId trackValueGid)
    {
        var keys = new List<object>(5);
        tr.Uid.IfSome(uid => keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Uid, uid)));
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Provider, "context"));
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Id, trackValueGid.ToString()));

        foreach (var (key, value) in tr.Metadata)
        {
            keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Metadata, $"{key};{value}"));
        }

        return ComposedKey.FromKeys(keys);
    }

    public static ComposedKey ConstructComposedKeyForCurrentTrack(Eum.Spotify.transfer.Playback transferStatePlayback,
        SpotifyId itemID)
    {
        var keys = new List<object>(5);
        var currentTrack = transferStatePlayback.CurrentTrack;
        if (!string.IsNullOrEmpty(currentTrack.Uid))
        {
            keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Uid,
                currentTrack.Uid));
        }

        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Provider, "context"));
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Id, itemID.ToString()));

        foreach (var (key, value) in currentTrack.Metadata)
        {
            keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Metadata, $"{key};{value}"));
        }

        // if(transferStatePlayback)
        // keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Index, _currentTrack.ValueUnsafe().Value.Index.ToString()));
        // keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.PageIndex, _currentPage.ValueUnsafe().Value.Index.ToString()));

        return ComposedKey.FromKeys(keys);
    }

    public static bool Predicate(ContextTrack trackToCheckAgainst,
        string? currentTrackUri,
        ByteString currentTrackGid,
        string? currentTrackUid)
    {
        if (string.IsNullOrEmpty(trackToCheckAgainst.Uri) && currentTrackGid.IsEmpty &&
            !string.IsNullOrEmpty(currentTrackUri))
        {
            currentTrackGid = ByteString.CopyFrom(SpotifyId.FromUri(currentTrackUri).ToRaw());
        }

        var uriEqual = trackToCheckAgainst.Uri == currentTrackUri;
        var gidEqual = !currentTrackGid.Span.IsEmpty &&
                       trackToCheckAgainst.Gid.Span.SequenceEqual(currentTrackGid.Span);
        var uidEqual = trackToCheckAgainst.Uid == currentTrackUid;
        return uidEqual || uriEqual || gidEqual;
    }

    public static ISpotifyContext CreateContext(Guid instanceId, Context context)
    {
        if (!EntityManager.TryGetClient(instanceId, out var spotifyClient))
            throw new NotSupportedException();

        ISpotifyContext playContext = default;
        if (context.Uri.StartsWith("spotify:station:"))
        {
            playContext = new SpotifyStationContext(instanceId, context, spotifyClient.Playback.CreateSpotifyStream);
        }
        else if (context.Uri.StartsWith("spotify:artist:"))
        {
            playContext = new SpotifyArtistContext(instanceId, context, spotifyClient.Playback.CreateSpotifyStream);
        }
        else
        {
            playContext =
                new SpotifyNormalFiniteContext(instanceId, context, spotifyClient.Playback.CreateSpotifyStream);
        }

        return playContext;
    }
}