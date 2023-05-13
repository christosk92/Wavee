using Google.Protobuf;
using LanguageExt;

namespace Wavee.Spotify.Playback.Playback.Cdn;

internal readonly record struct CdnUrl(ByteString FileId, Seq<MaybeExpiringUrl> Urls);