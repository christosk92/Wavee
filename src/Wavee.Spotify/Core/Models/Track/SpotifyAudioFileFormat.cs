namespace Wavee.Spotify.Core.Models.Track;

public enum SpotifyAudioFileFormat
{
    OGG_VORBIS_96  = 0,
    OGG_VORBIS_160 = 1,
    OGG_VORBIS_320 = 2,
    MP3_256        = 3,
    MP3_320        = 4,
    MP3_160        = 5, // Unencrypted, 1 substream
    MP3_96         = 6, // Unencrypted, 1 substream, for previews
    MP3_160_ENC    = 7, // Encrypted, 1 substream, rc4
    AAC_24         = 8, // Encrypted, 1 substream, aes
    AAC_48         = 9, // Encrypted, 1 substream, aes
    MP4_128        = 10, // AAC + EME, web audio
    MP4_256        = 11, // AAC + EME, web audio
    MP4_128_DUAL   = 12, // dual DRM
    MP4_256_DUAL   = 13, // dual DRM
    FLAC_FLAC      = 16,
}