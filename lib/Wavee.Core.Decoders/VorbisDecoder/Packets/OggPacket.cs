namespace Wavee.Core.Decoders.VorbisDecoder.Packets;

public class OggPacket
{
    public OggPacket(uint TrackId, ulong Ts, ulong Dur, uint TrimStart, uint TrimEnd, byte[] Data)
    {
        this.TrackId = TrackId;
        this.Ts = Ts;
        this.Dur = Dur;
        this.TrimStart = TrimStart;
        this.TrimEnd = TrimEnd;
        this.Data = Data;
    }

    public uint TrackId { get; init; }
    public ulong Ts { get; set; }
    public ulong Dur { get; set; }
    public uint TrimStart { get; set; }
    public uint TrimEnd { get; set; }
    public byte[] Data { get; init; }

    public void Deconstruct(out uint TrackId, out ulong Ts, out ulong Dur, out uint TrimStart, out uint TrimEnd, out byte[] Data)
    {
        TrackId = this.TrackId;
        Ts = this.Ts;
        Dur = this.Dur;
        TrimStart = this.TrimStart;
        TrimEnd = this.TrimEnd;
        Data = this.Data;
    }
}
