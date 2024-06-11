using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Io;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Stream;
using Wavee.Core.Decoders.VorbisDecoder.Mapper;
using Wavee.Core.Decoders.VorbisDecoder.Page;

namespace Wavee.Core.Decoders.VorbisDecoder.Streams;

internal static class PhysicalStream
{
    /*
    pub fn probe_stream_start(
    reader: &mut MediaSourceStream,
    pages: &mut PageReader,
    streams: &mut BTreeMap<u32, LogicalStream>,
)*/
    public static Result<Unit> ProbeStreamStart(MediaSourceStream reader, OggPageReader pages,
        SortedDictionary<uint, LogicalStream> streams)
    {
        // Save the original position to jump back to.
        var originalPos = reader.Position();

        // Scope the reader the prevent overruning the seekback region.
        var scopedReader = new ScopedStream<MediaSourceStream>(reader, OggPage.OGG_PAGE_MAX_SIZE);

        var probed = new SortedSet<uint>();

        // Examine the first bitstream page of each logical stream within the physical stream to
        // determine the number of leading samples, and start time. This function is called assuming
        // the page reader is on the first bitstream page within the physical stream.
        while (true)
        {
            var page = pages.Page();
            // If the page does not belong to the current physical stream, break out.
            if (!streams.TryGetValue(page.Header.Serial, out var stream))
                break;

            // If the stream hasn't been marked as probed.
            if (!probed.Contains(page.Header.Serial))
            {
                // Probe the first page of the logical stream.
                stream.InspectStartPage(page);
                // Mark the logical stream as probed.
                probed.Add(page.Header.Serial);
            }

            // If all logical streams were probed, break out immediately
            if (probed.Count >= streams.Count)
                break;

            // Read the next page.
            var result = pages.TryNextPage(scopedReader);
            if (result.IsFaulted)
            {
                break;
            }
        }

        scopedReader.Inner.SeekBuffered(originalPos);

        return Unit.Default;
    }

    public static Result<Option<ulong>> ProbeStreamEnd(MediaSourceStream reader, OggPageReader pages,
        SortedDictionary<uint, LogicalStream> streams, ulong byteRangeStart, ulong byteRangeEnd)
    {
        // Save the original position.
        var originalPos = reader.Position();

        // Number of bytes to linearly scan. We assume the OGG maximum page size for each logical
        // stream.
        var linearScanLen = (ulong)(streams.Count * OggPage.OGG_PAGE_MAX_SIZE);

        // Optimization: Try a linear scan of the last few pages first. This will cover all
        // non-chained physical streams, which is the majority of cases.
        if (byteRangeEnd >= linearScanLen && byteRangeStart <= byteRangeEnd - linearScanLen)
        {
            reader.Seek(SeekOrigin.Begin, byteRangeEnd - linearScanLen);
        }
        else
        {
            reader.Seek(SeekOrigin.Begin, byteRangeStart);
        }

        var nextPageResult = pages.NextPage(reader);
        if (nextPageResult.IsFaulted)
        {
            return new Result<Option<ulong>>(nextPageResult.Error());
        }

        //    let result = scan_stream_end(reader, pages, streams, byte_range_end);
        var result = ScanStreamEnd(reader, pages, streams, byteRangeEnd);

        // If there are no pages belonging to the current physical stream at the end of the media
        // source stream, then one or more physical streams are chained. Use a bisection method to find
        // the end of the current physical stream.
        Option<ulong> finalResult = Option<ulong>.None;
        if (result.IsNone)
        {
          //  Log.Information("media source stream is chained, bisecting end of physical stream");
        }
        else
        {
            finalResult = result;
        }

        reader.Seek(SeekOrigin.Begin, originalPos);
        
        return new Result<Option<ulong>>(finalResult);
    }


    private static Option<ulong> ScanStreamEnd(MediaSourceStream reader, OggPageReader pages,
        SortedDictionary<uint, LogicalStream> streams, ulong byteRangeEnd)
    {
        var scopedLen = byteRangeEnd - reader.Position();

        var scopedReader = new ScopedStream<MediaSourceStream>(reader, scopedLen);

        Option<ulong> upperPos = Option<ulong>.None;

        var state = new InspectState(Option<Bound>.None, Option<IPacketParser>.None);

        // Read pages until the provided end position or a new physical stream starts.
        while (true)
        {
            var page = pages.Page();

            // If the page does not belong to the current physical stream, then break out, the
            // extent of the physical stream has been found.
            if (!streams.TryGetValue(page.Header.Serial, out var stream))
            {
                break;
            }

            state = stream.InspectEndPage(state, page);

            // The new end of the physical stream is the position after this page.
            upperPos = Option<ulong>.Some(reader.Position());

            // Read to the next page.
            var result = pages.TryNextPage(scopedReader);
            if (result.IsFaulted)
            {
                break;
            }
        }

        return upperPos;
    }
}

internal record InspectState(Option<Bound> Bound, Option<IPacketParser> Parser);