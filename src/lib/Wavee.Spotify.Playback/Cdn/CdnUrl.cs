using Google.Protobuf;

namespace Wavee.Spotify.Playback.Cdn;

internal readonly record struct CdnUrl(ByteString FileId, Seq<MaybeExpiringUrl> Urls);