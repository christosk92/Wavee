using Wavee.Enums;

namespace Wavee.Interfaces;

internal interface IPacketDispatcher
{
    Task SendPacketAsync(PacketType type, byte[] payload);
    void SetAudioKeyDispatcher(IAudioKeyManager audioKeyManager);
}