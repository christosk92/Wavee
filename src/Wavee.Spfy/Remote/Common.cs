using Eum.Spotify.context;
using Google.Protobuf;
using Wavee.Spfy.Playback;

namespace Wavee.Spfy.Remote;

internal static class Common
{
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
        var gidEqual = !currentTrackGid.Span.IsEmpty && trackToCheckAgainst.Gid.Span.SequenceEqual(currentTrackGid.Span);
        var uidEqual = trackToCheckAgainst.Uid == currentTrackUid;
        return uidEqual || uriEqual || gidEqual;
    }
}