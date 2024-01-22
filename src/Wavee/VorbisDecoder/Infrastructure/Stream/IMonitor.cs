namespace Wavee.Vorbis.Infrastructure.Stream;

internal interface IMonitor
{
    void ProcessByte(byte b);
    void ProcessBufferBytes(Span<byte> buffer);

    void ProcessQuadBytes(Span<byte> buffer)
    {
        this.ProcessByte(buffer[0]);
        this.ProcessByte(buffer[1]);
        this.ProcessByte(buffer[2]);
        this.ProcessByte(buffer[3]);
    }
}