using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Decoders.VorbisDecoder.Format;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Stream;
using Wavee.Core.Decoders.VorbisDecoder.Mapper;
using Wavee.Core.Decoders.VorbisDecoder.Packets;
using Wavee.Core.Decoders.VorbisDecoder.Page;
using Wavee.Core.Decoders.VorbisDecoder.Streams;

namespace Wavee.Core.Decoders.VorbisDecoder;

public sealed class OggReader
{
    private Option<OggPacket> _lastKnownPacket = Option<OggPacket>.None;

    internal OggReader(MediaSourceStream reader, List<Track> tracks, List<Cue> cues, MetadataLog metadata,
        FormatOptions options, OggPageReader pages, SortedDictionary<uint, LogicalStream> streams,
        ulong physByteRangeStart, Option<ulong> physByteRangeEnd)
    {
        Reader = reader;
        Tracks = tracks;
        Cues = cues;
        Metadata = metadata;
        Options = options;
        Pages = pages;
        Streams = streams;
        PhysByteRangeStart = physByteRangeStart;
        PhysByteRangeEnd = physByteRangeEnd;
    }

    internal MediaSourceStream Reader { get; set; }
    internal List<Track> Tracks { get; set; }
    internal List<Cue> Cues { get; set; }
    internal MetadataLog Metadata { get; set; }
    internal FormatOptions Options { get; set; }
    internal OggPageReader Pages { get; set; }
    internal SortedDictionary<uint, LogicalStream> Streams { get; set; }
    internal ulong PhysByteRangeStart { get; set; }
    internal Option<ulong> PhysByteRangeEnd { get; set; }

    public Option<Track> DefaultTrack
    {
        get
        {
            if (this.Tracks.Count == 0)
                return Option<Track>.None;

            return this.Tracks[0];
        }
    }

    public Option<TimeSpan> Position => _lastKnownPacket.Bind(PositionOfPacket);

    public Option<TimeSpan> TotalTime
    {
        get
        {
            var time = this.DefaultTrack.Bind(t => t.CodecParams.NFrames)
                .Bind(f => DefaultTrack.Map(t => t.CodecParams.StartTs + f))
                .Bind(ts => this.DefaultTrack.Bind(t =>
                    t.CodecParams.SampleRate.Map(sampleRate => new TimeBase(1, sampleRate).CalcTime(ts))))
                .Bind(f => f.Match(
                    Succ: x => x,
                    Fail: e =>
                    {
                        //Log.Error(e, "Error calculating total time");
                        return Option<TimeSpan>.None;
                    }
                ));
            return time;
        }
    }

    public Option<ulong> TotalBytes => this.Reader.Length;

    public Option<TimeSpan> PositionOfPacket(OggPacket pkt)
    {
        var ts = pkt.Ts;
        var track = this.Tracks.First();
        var res = new TimeBase(1, track.CodecParams.SampleRate.ValueUnsafe()).CalcTime(ts);
        if (res.IsFaulted)
        {
            return Option<TimeSpan>.None;
        }

        return res.Success();
    }

    public Result<OggPacket> NextPacket()
    {
        var pkt = this.NextLogicalPacket();
        if (pkt.IsSuccess)
        {
            _lastKnownPacket = pkt.Success();
        }

        return pkt;
    }

    public Result<SeekedTo> Seek(SeekMode mode, TimeSpan to)
    {
        // Get the timestamp of the desired audio frame.
        var track = this.Tracks.FirstOrDefault();
        if (track == default) return new Result<SeekedTo>(new SeekError(SeekErrorType.Unseekable));

        // Convert the time to a timestamp.
        ulong ts;
        if (this.Streams.TryGetValue(track.Id, out var stream))
        {
            var parameters = stream.CodecParams;
            if (parameters.SampleRate.IsNone)
            {
                // No sample rate. This should never happen.
                return new Result<SeekedTo>(new SeekError(SeekErrorType.Unseekable));
            }

            var tsRes = new TimeBase(1, parameters.SampleRate.ValueUnsafe()).CalcTimestamp(to);
            if (tsRes.IsFaulted)
            {
                return new Result<SeekedTo>(new SeekError(SeekErrorType.Unseekable));
            }

            ts = tsRes.Success();
            // Timestamp lower-bound out-of-range.
            if (ts < parameters.StartTs)
                return new Result<SeekedTo>(new SeekError(SeekErrorType.OutOfRange));

            // Timestamp upper-bound out-of-range.
            if (parameters.NFrames.IsSome)
            {
                var dur = parameters.NFrames.ValueUnsafe();
                if (ts > (dur + parameters.StartTs))
                    return new Result<SeekedTo>(new SeekError(SeekErrorType.OutOfRange));
            }
        }
        else
        {
            // No mapper for track. The user provided a bad track ID.
            return new Result<SeekedTo>(new SeekError(SeekErrorType.InvalidTrack));
        }

        // Log.Information("ogg: seeking track={TrackId} to frame_ts={RequiredTs}", track.Id, ts);

        // Do the actual seek.
        return this.DoSeek(track.Id, ts);
    }


    public static Result<OggReader> TryNew(MediaSourceStream source, FormatOptions options)
    {
        // A seekback buffer equal to the maximum OGG page size is required for this reader.
        source.EnsureSeekbackBuffer(len: OggPage.OGG_PAGE_MAX_SIZE);

        var pagesMaybe = OggPageReader.TryNew(reader: source);
        if (pagesMaybe.IsFaulted)
        {
            return new Result<OggReader>(pagesMaybe.Error());
        }

        var pages = pagesMaybe.Success();
        if (!pages.Header.IsFirstPage)
        {
            return new Result<OggReader>(new NotSupportedException("ogg: page is not marked as first"));
        }

        var ogg = new OggReader(
            reader: source,
            tracks: new List<Track>(),
            cues: new List<Cue>(),
            metadata: new MetadataLog(),
            streams: new SortedDictionary<uint, LogicalStream>(),
            options: options,
            pages: pages,
            physByteRangeStart: 0,
            physByteRangeEnd: Option<ulong>.None);

        var result = ogg.StartNewPhysicalStream();
        if (result.IsFaulted)
        {
            return new Result<OggReader>(result.Error());
        }

        return new Result<OggReader>(ogg);
    }

    private Result<OggPacket> NextLogicalPacket()
    {
        while (true)
        {
            var page = this.Pages.Page();

            // Read the next packet. Packets are only ever buffered in the logical stream of the
            // current page.
            if (this.Streams.TryGetValue(page.Header.Serial, out var stream))
            {
                var packetMaybe = stream.NextPacket();
                if (packetMaybe.IsSome)
                {
                    return new Result<OggPacket>(packetMaybe.ValueUnsafe());
                }
            }

            var readPageResult = this.ReadPage();
            if (readPageResult.IsFaulted)
            {
                return new Result<OggPacket>(readPageResult.Error());
            }
        }
    }

    private Result<Unit> StartNewPhysicalStream()
    {
        // The new mapper set.
        var streams = new SortedDictionary<uint, LogicalStream>();

        // The start of page position.
        var byteRangeStart = this.Reader.Position();

        // Pre-condition: This function is only called when the current page is marked as a
        // first page.
        var firstPage = this.Pages.Header;
        if (!firstPage.IsFirstPage)
        {
            return new Result<Unit>(new DecodeError("ogg: page is not marked as first"));
        }

        //Log.Information("ogg: starting new physical stream");

        // The first page of each logical stream, marked with the first page flag, must contain the
        // identification packet for the encapsulated codec bitstream. The first page for each
        // logical stream from the current logical stream group must appear before any other pages.
        // That is to say, if there are N logical streams, then the first N pages must contain the
        // identification packets for each respective logical stream.
        while (true)
        {
            var header = this.Pages.Header;

            if (!header.IsFirstPage)
                break;

            byteRangeStart = this.Reader.Position();

            // There should only be a single packet, the identification packet, in the first page.
            var packetMaybe = this.Pages.FirstPacket();
            if (packetMaybe.IsSome)
            {
                var packet = packetMaybe.ValueUnsafe();
                var mapperMaybe = Mappings.Detect(packet);
                if (mapperMaybe.IsSome)
                {
                    var mapper = mapperMaybe.ValueUnsafe();
                    // Log.Information("Selected {MapperName} mapper for stream with serial={Serial}",
                    // mapper.Name, header.Serial);

                    var stream = new LogicalStream(
                        mapper: mapper,
                        this.Options.EnableGapless
                    );
                    streams.Add(header.Serial, stream);
                }
            }

            //Read the next page
            var res = this.Pages.TryNextPage(this.Reader);
            if (res.IsFaulted)
            {
                return res;
            }
        }

        // Each logical stream may contain additional header packets after the identification packet
        // that contains format-relevant information such as setup and metadata. These packets,
        // for all logical streams, should be grouped together after the identification packets.
        // Reading pages consumes these headers and returns any relevant data as side data. Read
        // pages until all headers are consumed and the first bitstream packets are buffered.
        while (true)
        {
            var page = this.Pages.Page();

            if (streams.TryGetValue(page.Header.Serial, out var stream))
            {
                var sideDataR = stream.ReadPage(page);
                if (sideDataR.IsFaulted)
                {
                    return new Result<Unit>(sideDataR.Error());
                }

                // Consume each piece of side data.
                foreach (var data in sideDataR.Success())
                {
                    this.Metadata.Push(data.Revision);
                }

                if (stream.HasPackets)
                    break;
            }

            // The current page has been consumed and we're committed to reading a new one. Record
            // the end of the current page.
            byteRangeStart = this.Reader.Position();

            var r = this.Pages.TryNextPage(this.Reader);
            if (r.IsFaulted)
            {
                return new Result<Unit>(r.Error());
            }
        }

        // Probe the logical streams for their start and end pages.
        PhysicalStream.ProbeStreamStart(this.Reader, this.Pages, streams);

        Option<ulong> byteRangeEnd = Option<ulong>.None;
        _lengthWithLength = (long)Reader.Length.IfNone(0);

        // If the media source stream is seekable, then try to determine the duration of each
        // logical stream, and the length in bytes of the physical stream.
        if (this.Reader.CanSeek)
        {
            var totalLen = this.Reader.Length;
            if (totalLen.IsSome)
            {
                var totalLenVal = totalLen.ValueUnsafe();
                var probeResult = PhysicalStream.ProbeStreamEnd(
                    this.Reader,
                    this.Pages,
                    streams,
                    byteRangeStart,
                    totalLenVal
                );
                if (probeResult.IsFaulted)
                {
                    return new Result<Unit>(probeResult.Error());
                }

                byteRangeEnd = probeResult.Success();
            }
        }


        // At this point it can safely be assumed that a new physical stream is starting.

        // First, clear the existing track listing.
        this.Tracks.Clear();

        // Second, add a track for all streams.
        foreach (var (serial, stream) in streams)
        {
            // Warn if the track is not ready. This should not happen if the physical stream was
            // muxed properly.
            if (!stream.IsReady)
            {
                //  Log.Warning("track for serial={Serial} is not ready", serial);
            }

            this.Tracks.Add(new Track(
                Id: serial, CodecParams: (CodecParameters)stream.CodecParams.Clone(), Language: Option<string>.None
            ));
        }

        // Third, replace all logical streams with the new set.
        this.Streams = streams;

        // Last, store the lower and upper byte boundaries of the physical stream for seeking.
        this.PhysByteRangeStart = byteRangeStart;
        this.PhysByteRangeEnd = byteRangeEnd;

        return Unit.Default;
    }

    private long _lengthWithLength = 0;

    private Result<SeekedTo> DoSeek(uint serial, ulong requiredTs)
    {
        // If the reader is seekable, then use the bisection method to coarsely seek to the nearest
        // page that ends before the required timestamp.
        if (this.Reader.CanSeek)
        {
            var stream = this.Streams[serial];

            // The end of the physical stream.
            // check if length changed
            var totalLen = (long)this.Reader.Length.IfNone(0);
            if (totalLen != _lengthWithLength)
            {
                _lengthWithLength = totalLen;
                var start = this.Reader.Position();
                var probeResult = PhysicalStream.ProbeStreamEnd(
                    this.Reader,
                    this.Pages,
                    this.Streams,
                    start,
                    (ulong)totalLen
                );
                if (probeResult.IsFaulted)
                {
                    return new Result<SeekedTo>(probeResult.Error());
                }

                this.PhysByteRangeEnd = probeResult.Success();
            }

            var physicalEnd = this.PhysByteRangeEnd.ValueUnsafe();

            var startBytePos = this.PhysByteRangeStart;
            var endBytePos = physicalEnd;


            // Bisection method.
            while (true)
            {
                // Find the middle of the upper and lower byte search range.
                var midBytePos = (startBytePos + endBytePos) / 2;

                // Seek to the middle of the byte range.
                this.Reader.Seek(SeekOrigin.Begin, midBytePos);

                // Read the next page.
                var result = this.Pages.NextPageForSerial(Reader, serial);
                if (result.IsFaulted)
                {
                    return new Result<SeekedTo>(new SeekError(SeekErrorType.OutOfRange));
                }

                // Probe the page to get the start and end timestamp.
                var (startTs, endTs) = stream.InspectPage(this.Pages.Page());

                //Log.Debug(
                //  "ogg: seek: bisect step: page={{ start={StartTs}, end={EndTs} }} byte_range=[{StartBytePos}..{EndBytePos}], mid={MidBytePos}",
                // startTs, endTs, startBytePos, endBytePos, midBytePos);

                if (requiredTs < startTs)
                {
                    // The required timestamp is less-than the timestamp of the first sample in
                    // page1. Update the upper bound and bisect again.
                    endBytePos = midBytePos;
                }
                else if (requiredTs > endTs)
                {
                    // The required timestamp is greater-than the timestamp of the final sample in
                    // the in page1. Update the lower bound and bisect again.
                    startBytePos = midBytePos;
                }
                else
                {
                    // The sample with the required timestamp is contained in page1. Return the
                    // byte position for page0, and the timestamp of the first sample in page1, so
                    // that when packets from page1 are read, those packets will have a non-zero
                    // base timestamp.
                    break;
                }


                // Prevent infinite iteration and too many seeks when the search range is less
                // than 2x the maximum page size.
                if ((endBytePos - startBytePos) <= (2 * OggPage.OGG_PAGE_MAX_SIZE))
                {
                    this.Reader.Seek(SeekOrigin.Begin, startBytePos);

                    var res = this.Pages.NextPageForSerial(this.Reader, serial);
                    if (res.IsFaulted)
                    {
                        return new Result<SeekedTo>(new SeekError(SeekErrorType.OutOfRange));
                    }

                    break;
                }
            }

            // Reset all logical bitstreams since the physical stream will be reading from a new
            // location now.
            foreach (var (s, str) in this.Streams)
            {
                str.Reset();

                // Read in the current page since it contains our timestamp.
                if (s == serial)
                {
                    var res = str.ReadPage(this.Pages.Page());
                    if (res.IsFaulted)
                    {
                        return new Result<SeekedTo>(res.Error());
                    }
                }
            }
        }

        // Consume packets until reaching the desired timestamp.
        ulong actualTs = 0;
        while (true)
        {
            var packetReadResult = this.PeekLogicalPacket();
            if (packetReadResult.IsSome)
            {
                var packet = packetReadResult.ValueUnsafe();
                if (packet.TrackId == serial && (packet.Ts + packet.Dur) >= requiredTs)
                {
                    actualTs = packet.Ts;
                    break;
                }

                this.DiscardLogicalPacket();
            }
            else
            {
                var readPageResult = this.ReadPage();
                if (readPageResult.IsFaulted)
                {
                    return new Result<SeekedTo>(readPageResult.Error());
                }
            }
        }

        var accuracyFrac = (double)actualTs / requiredTs;

        //Log: "ogg: seeked track={TrackId} to packet_ts={ActualTs} (delta={Delta}). accuracy={Accuracy}%"
        //Log.Information("ogg: seeked track={TrackId} to packet_ts={ActualTs} (delta={Delta}). accuracy={Accuracy}%",
        //  serial, actualTs, actualTs - requiredTs, accuracyFrac * 100);
        return new Result<SeekedTo>(new SeekedTo(serial, requiredTs, actualTs));
    }

    private Result<Unit> ReadPage()
    {
        // Try reading pages until a page is successfully read, or an IO error.
        while (true)
        {
            var pageResult = this.Pages.NextPage(this.Reader);
            if (pageResult.IsFaulted)
            {
                var error = pageResult.Error();
                if (error is IOException)
                {
                    // IO error.
                    if (error is EndOfStreamException) return new Result<Unit>(error);
                    break;
                }

                // Discard the page and try again.
                //Log.Warning("ogg: failed to read page: {Error}", error);
                continue;
            }

            break;
        }

        var page = this.Pages.Page();
        // If the page is marked as a first page, then try to start a new physical stream.
        if (page.Header.IsFirstPage)
        {
            this.StartNewPhysicalStream();
            return new Result<Unit>(new ResetError());
        }

        if (Streams.TryGetValue(page.Header.Serial, out var stream))
        {
            // TODO: Process side data.
            var _side_Data = stream.ReadPage(page);
        }
        else
        {
            // If there is no associated logical stream with this page, then this is a
            // completely random page within the physical stream. Discard it.
        }

        return Unit.Default;
    }

    private void DiscardLogicalPacket()
    {
        var page = this.Pages.Page();

        // Consume a packet from the logical stream belonging to the current page.
        if (this.Streams.TryGetValue(page.Header.Serial, out var stream))
        {
            stream.ConsumePacket();
        }
    }

    private Option<OggPacket> PeekLogicalPacket()
    {
        var page = this.Pages.Page();

        if (this.Streams.TryGetValue(page.Header.Serial, out var stream))
        {
            var pck = stream.PeekPacket();
            return pck;
        }

        return Option<OggPacket>.None;
    }
}

public enum SeekMode
{
    /// <summary>
    /// Coarse seek mode is a best-effort attempt to seek to the requested position. The actual
    /// position seeked to may be before or after the requested position. Coarse seeking is an
    /// optional performance enhancement. If a `FormatReader` does not support this mode an
    /// accurate seek will be performed instead.
    /// </summary>
    Coarse,

    /// <summary>
    /// Accurate (aka sample-accurate) seek mode will be always seek to a position before the
    /// requested position.
    /// </summary>
    Accurate
}

/// <summary>
/// Represents a seek operation.
/// </summary>
/// <param name="TrackId">
/// The track ID that was seeked.
/// </param>
/// <param name="RequiredTs">
/// A TimeStamp represents an instantenous instant in time since the start of a stream. One
/// TimeStamp "tick" is equivalent to the stream's TimeBase in seconds.
/// </param>
/// <param name="ActualTs">
/// A TimeStamp represents an instantenous instant in time since the start of a stream. One
/// TimeStamp "tick" is equivalent to the stream's TimeBase in seconds.
/// </param>
public readonly record struct SeekedTo(uint TrackId, ulong RequiredTs, ulong ActualTs);