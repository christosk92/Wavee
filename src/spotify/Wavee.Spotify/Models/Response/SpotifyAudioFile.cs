using System.Numerics;
using Google.Protobuf;
using Wavee.Spotify.Extensions;

namespace Wavee.Spotify.Models.Response;

public readonly record struct SpotifyAudioFile(SpotifyAudioFileType Type, ByteString FileId)
{
    public string FileIdBase16 => FileId.ToStringUtf8();
}

public enum SpotifyAudioFileType
{
    OGG_VORBIS_320,
    OGG_VORBIS_160,
    OGG_VORBIS_96
}