using Google.Protobuf;

namespace Wavee.Spotify.Clients.Playback.Cdn;

internal readonly record struct CdnUrl(ByteString FileId, Seq<MaybeExpiringUrl> Urls);