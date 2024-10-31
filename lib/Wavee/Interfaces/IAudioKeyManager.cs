using Wavee.Enums;
using Wavee.Models.Common;
using Wavee.Services;

namespace Wavee.Interfaces;

internal interface IAudioKeyManager
{
    void Dispatch(PacketType cmd, byte[] data);
    Task<AudioKey> RequestAsync(SpotifyId track, FileId file);
}