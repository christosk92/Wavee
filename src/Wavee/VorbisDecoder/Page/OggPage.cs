using LanguageExt.Common;
using Wavee.Vorbis.Infrastructure;
using Wavee.Vorbis.Infrastructure.Stream;

namespace Wavee.Vorbis.Page;

internal class OggPage
{
    public static readonly byte[] OggPageMarker = "OggS"u8.ToArray();
    public const int OGG_PAGE_HEADER_SIZE = 27;
    public const int OGG_PAGE_MAX_SIZE = OGG_PAGE_HEADER_SIZE + 255 + 255 * 255;
    
    public OggPageHeader Header { get; }
    
    private List<ushort> packetLens; 
    private byte[] pageBuf;     

    public OggPage(OggPageHeader header, List<ushort> packetLens, byte[] pageBuf)
    {
        this.Header = header;
        this.packetLens = packetLens;
        this.pageBuf = pageBuf;
    }
    
    /// <summary>
    /// Returns an iterator over all complete packets within the page.
    /// If this page contains a partial packet, then the partial packet data may be retrieved using
    /// the PartialPacket function of the iterator.
    /// </summary>
    public PagePackets Packets()
    {
        return new PagePackets(packetLens, pageBuf);
    }

    /// <summary>
    /// Gets the number of packets completed on this page.
    /// </summary>
    public int NumPackets()
    {
        return packetLens.Count;
    }
}