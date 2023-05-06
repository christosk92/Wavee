namespace Wavee.VorbisDecoder.Contracts.Ogg
{
    interface IPacketReader
    {
        Memory<byte> GetPacketData(int pagePacketIndex);

        void InvalidatePacketCache(IPacket packet);
    }
}
