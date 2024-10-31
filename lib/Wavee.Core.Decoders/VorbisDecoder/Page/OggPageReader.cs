using System.Diagnostics;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Checksum;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Io;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Stream;

namespace Wavee.Core.Decoders.VorbisDecoder.Page;

internal sealed class OggPageReader
{
    private OggPageHeader header; // Assuming PageHeader is a class/struct you've defined elsewhere
    private List<ushort> packetLens = new List<ushort>();
    private byte[] pageBuf = Array.Empty<byte>();
    private int pageBufLen;
    private static readonly byte[] empty4bytearray = new byte[4];

    public static Result<OggPageReader> TryNew<TB>(TB reader) where TB : IReadBytes, ISeekBuffered
    {
        var pageReader = new OggPageReader
        {
            header = new OggPageHeader(), // Assuming you have a default constructor or similar mechanism
            packetLens = new List<ushort>(),
            pageBufLen = 0
        };

        var result = pageReader.TryNextPage(reader);
        if (result.IsFaulted)
        {
            return new Result<OggPageReader>(result.Error());
        }

        return new Result<OggPageReader>(pageReader);
    }

    public OggPageHeader Header => header;

    /// <summary>
    /// Attempts to read the next page. If the page is corrupted or invalid, returns an error.
    /// </summary>
    /// <param name="reader"></param>
    /// <typeparam name="TB"></typeparam>
    /// <returns></returns>
    internal Result<Unit> TryNextPage<TB>(TB reader) where TB : IReadBytes, ISeekBuffered
    {
        var headerBuf = new byte[OggPage.OGG_PAGE_HEADER_SIZE];
        OggPage.OggPageMarker.CopyTo(headerBuf.AsSpan()[..4]);

        // Synchronize to an OGG page capture pattern.
        var syncPageResult = OggPageHeader.SyncPage(reader);
        if (syncPageResult.IsFaulted)
        {
            return new Result<Unit>(syncPageResult.Error());
        }

        // Record the position immediately after synchronization. If the page is found corrupt the
        // reader will need to seek back here to try to regain synchronization.
        var syncPos = reader.Position();

        // Read the part of the page header after the capture pattern into a buffer.
        reader.ReadBufferExactly(headerBuf.AsSpan()[4..]);

        // Parse the page header buffer.
        var headerMaybe = OggPageHeader.ReadPageHeader(new BufReader(headerBuf));
        if (headerMaybe.IsFaulted)
        {
            return new Result<Unit>(headerMaybe.Error());
        }

        var header = headerMaybe.Success();
        if (header.Sequence == 33)
        {
            
        }

        // The CRC of the OGG page requires the page checksum bytes to be zeroed.
        empty4bytearray.CopyTo(headerBuf.AsSpan()[22..26]);

        // Instantiate a Crc32, initialize it with 0, and feed it the page header buffer.
        var crc32 = new OggCrc32(0);

        crc32.ProcessBufferBytes(headerBuf);

        // The remainder of the page will be checksummed as it is read.
        var crc32reader = new MonitorStream<TB, OggCrc32>(inner: reader, monitor: crc32);

        // Read segment table.
        uint pageBodyLen = 0;
        ushort packetLen = 0;

        // TODO: Can this be transactional? A corrupt page causes the PageReader's state not
        // to change.
        this.packetLens.Clear();

        for (var i = 0; i < header.NSegments; i++)
        {
            var segmentLen = crc32reader.ReadByte();
            pageBodyLen += segmentLen;
            packetLen += segmentLen;

            // A segment with a length < 255 indicates that the segment is the end of a packet.
            // Push the packet length into the packet queue for the stream.
            if (segmentLen < 255)
            {
                this.packetLens.Add(packetLen);
                packetLen = 0;
            }
        }

        // Read page body.
        var pageBodyResult = this.ReadPageBody(
            reader: crc32reader,
            length: (int)pageBodyLen
        );
        if (pageBodyResult.IsFaulted)
        {
            return new Result<Unit>(pageBodyResult.Error());
        }

        var calculatedCrc = crc32reader.Monitor().Crc();

        // If the CRC for the page is incorrect, then the page is corrupt.
        if (calculatedCrc != header.Crc)
        {
            static string ToHex(uint value) => $"0x{value:X}";
            // Log.Warning("crc mismatch: expected {ExpectedCrc}, got {CalculatedCrc}", ToHex(header.Crc),
            //     ToHex(calculatedCrc));

            // Clear packet buffer.
            this.packetLens.Clear();
            this.pageBufLen = 0;

            // Seek back to the immediately after the previous sync position.
            crc32reader.IntoInner().SeekBuffered(syncPos);

            return new Result<Unit>(new DecodeError("ogg: crc mismatch"));
        }

        this.header = header;
        return new Result<Unit>(Unit.Default);
    }

    private Result<Unit> ReadPageBody<TB>(TB reader, int length) where TB : IReadBytes
    {
        try
        {
            // This is precondition.
            Debug.Assert(length <= 255 * 255);

            if (length > pageBuf.Length)
            {
                // New page buffer size, rounded up to the nearest 8K block.
                var newBufLen = (length + (8 * 1024 - 1)) & ~(8 * 1024 - 1);
                System.Diagnostics.Debug.WriteLine($"grow page buffer to {newBufLen} bytes");

                // Resize the buffer
                System.Array.Resize(ref pageBuf, newBufLen);
            }

            pageBufLen = length;
            var r = reader.ReadBufferExactly(buffer: pageBuf.AsSpan()[..length]);
            return new Result<Unit>(Unit.Default);
        }
        catch (Exception e)
        {
            return new Result<Unit>(e);
        }
    }

    public Option<byte[]> FirstPacket()
    {
        //        self.packet_lens.first().map(|&len| &self.page_buf[..usize::from(len)])
        if (packetLens.Count == 0)
        {
            return Option<byte[]>.None;
        }

        var len = packetLens[0];
        return pageBuf[..len];
    }

    public OggPage Page()
    {
        //        assert!(self.page_buf_len <= 255 * 255, "ogg pages are <= 65025 bytes");
        Debug.Assert(pageBufLen <= 255 * 255, "ogg pages are <= 65025 bytes");
        return new OggPage(
            header: header,
            packetLens: packetLens,
            pageBuf: pageBuf[..pageBufLen]
        );
    }

    /// <summary>
    /// Reads the next page. If the next page is corrupted or invalid, the page is discarded and
    /// the reader tries again until a valid page is read or end-of-stream.
    /// </summary>
    /// <param name="reader"></param>
    /// <typeparam name="TB"></typeparam>
    /// <returns></returns>
    public Result<Unit> NextPage<TB>(TB reader) where TB : IReadBytes, ISeekBuffered
    {
        while (true)
        {
            var result = TryNextPage(reader);
            if (result.IsSuccess)
            {
                break;
            }

            var error = result.Error();

            if (error is IOException or EndOfStreamException)
                return new Result<Unit>(error);
            else if(error is ObjectDisposedException)
                throw error;
            
            continue;
        }

        return Unit.Default;
    }

    /// <summary>
    /// Reads the next page with a specific serial. If the next page is corrupted or invalid, the
    /// page is discarded and the reader tries again until a valid page is read or end-of-stream.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="serial"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Result<Unit> NextPageForSerial<TB>(TB reader, uint serial) where TB : IReadBytes, ISeekBuffered
    {
        while (true)
        {
            var result = TryNextPage(reader);
            if (result.IsSuccess)
            {
                var header = Header;
                // Exit if a page with the specific serial is found.
                if (header.Serial == serial)
                {
                    break;
                }

                continue;
            }

            var error = result.Error();
            if (error is IOException)
                return new Result<Unit>(error);

            break;
        }
        
        return Unit.Default;
    }
}