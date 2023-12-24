using System.Runtime.InteropServices.JavaScript;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using Wavee.Vorbis.Format;
using Wavee.Vorbis.Infrastructure;
using Wavee.Vorbis.Mapper;
using Wavee.Vorbis.Packets;
using Wavee.Vorbis.Page;

namespace Wavee.Vorbis.Streams;

internal sealed class LogicalStream
{
    private const int MAX_PACKET_LEN = 8 * 1024 * 1024;

    private readonly LinkedList<OggPacket> _packets = new();
    private Option<OggPageInfo> _prevPageInfo;
    private IMapper _mapper;
    private bool _gapless;
    private int _partLen;
    private Option<Bound> _startBound;
    private Option<Bound> _endBound;
    private byte[] _partBuf;

    public LogicalStream(IMapper mapper, bool gapless)
    {
        _mapper = mapper;
        _gapless = gapless;

        _startBound = Option<Bound>.None;
        _endBound = Option<Bound>.None;
        _packets = new LinkedList<OggPacket>();
        _prevPageInfo = Option<OggPageInfo>.None;
        _partBuf = System.Array.Empty<byte>();
    }

    /// <summary>
    ///  Returns true if the logical stream has packets buffered.
    /// </summary>
    public bool HasPackets => _packets.Count > 0;

    public CodecParameters CodecParams => _mapper.CodecParams;
    public bool IsReady => _mapper.IsReady;


    /// <summary>
    /// Reads a page.
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    public Result<List<SideData>> ReadPage(OggPage page)
    {
        // Side data vector. This will not allocate unless data is pushed to it (normal case).
        var sideData = new List<SideData>();

        // If the last sequence number is available, detect non-monotonicity and discontinuities
        // in the stream. In these cases, clear any partial packet data.
        if (this._prevPageInfo.IsSome)
        {
            var lastTs = this._prevPageInfo.ValueUnsafe();
            if (page.Header.Sequence < lastTs.Seq)
            {
                Log.Warning("detected stream page non-monotonicity");
                this._partLen = 0;
            }
            else if ((page.Header.Sequence - lastTs.Seq) > 1)
            {
                Log.Warning("detected stream page discontinuity of {0} pages", page.Header.Sequence - lastTs.Seq);
                _partLen = 0;
            }
        }

        _prevPageInfo = new OggPageInfo(page.Header.Sequence, page.Header.AbsGp);

        using var iter = page.Packets();

        // If there is partial packet data buffered, a continuation page is expected.
        if (!page.Header.IsContinuation && this._partLen > 0)
        {
            Log.Warning("expected a continuation page");

            _partLen = 0;
        }

        // If there is no partial packet data buffered, a continuation page is not expected.
        if (page.Header.IsContinuation && _partLen == 0)
        {
            // If the continuation page contains packets, drop the first packet since it would
            // require partial packet data to be complete. Otherwise, ignore this page entirely
            if (page.NumPackets() > 0)
            {
                Log.Warning("unexpected continuation page, ignoring incomplete first packet");
                iter.Next();
            }
            else
            {
                Log.Warning("unexpected continuation page, ignoring");
                return sideData;
            }
        }

        var numPrevPackets = this._packets.Count;

        //Consume rest of iter
        var current = iter.Current;
        while (current.IsSome)
        {
            var buf = current.ValueUnsafe()!;
            var data = this.GetPacket(buf);

            // Perform packet mapping. If the packet contains stream data, queue it onto the packet
            // queue. If it contains side data, then add it to the side data list. Ignore other
            // types of packet data.
            var mappingResult = this._mapper.MapPacket(data);
            mappingResult.Match(
                Succ: mapped =>
                {
                    switch (mapped)
                    {
                        case StreamDataMapResult streamData:
                        {
                            _packets.AddLast(
                                new OggPacket(
                                    TrackId: page.Header.Serial,
                                    Ts: 0,
                                    Dur: streamData.Dur,
                                    Data: data,
                                    TrimStart: 0,
                                    TrimEnd: 0
                                )
                            );
                            break;
                        }
                        case SideDataMapResult sideDataMapResult:
                        {
                            sideData.Add(sideDataMapResult.SideData);
                            break;
                        }
                    }

                    return Unit.Default;
                },
                Fail: err =>
                {
                    Log.Warning("failed to map packet: {0}. Skipping", err.Message);
                    return Unit.Default;
                }
            );

            current = iter.Next();
        }

        // If the page contains partial packet data, then save the partial packet data for later
        // as the packet will be completed on a later page.
        var partialPacketMaybe = iter.PartialPacket();
        if (partialPacketMaybe.IsSome)
        {
            var partialPacket = partialPacketMaybe.ValueUnsafe()!;
            var saveResult = this.SavePartialPacket(partialPacket);

            if (saveResult.IsFaulted)
            {
                var error = saveResult.Error();
                return new Result<List<SideData>>(error);
            }
        }


        // The number of packets from this page that were queued.
        var numNewPackets = this._packets.Count - numPrevPackets;

        if (numNewPackets > 0)
        {
            // Get the start delay.
            //            let start_delay = self.start_bound.as_ref().map_or(0, |b| b.delay);
            var startDelay = this._startBound.IsSome ? this._startBound.ValueUnsafe().Delay : 0;


            // Assign timestamps by first calculating the timestamp of one past the last sample in
            // in the last packet of this page, add the start delay.
            var pageEndTs = this._mapper.AbsGpToTs(page.Header.AbsGp).SaturatingAdd(startDelay);

            // If this is the last page, then add the end delay to the timestamp.
            if (page.Header.IsLastPage)
            {
                var endDelay = this._endBound.IsSome ? this._endBound.ValueUnsafe().Delay : 0;
                pageEndTs = pageEndTs.SaturatingAdd(endDelay);
            }

            // Then, iterate over the newly added packets in reverse order and subtract their
            // cumulative duration at each iteration to get the timestamp of the first sample
            // in each packet.
            ulong pageDur = 0;
            for (var i = 0; i < numNewPackets; i++)
            {
                var packet = _packets.ElementAt(_packets.Count - i - 1);
                pageDur = pageDur.SaturatingAdd(packet.Dur);
                packet.Ts = pageEndTs.SaturatingSub(pageDur);
            }

            if (this._gapless)
            {
                for (var i = 0; i < numNewPackets; i++)
                {
                    var packet = _packets.ElementAt(_packets.Count - i - 1);

                    TrimPacket(packet, startDelay, _endBound.Map(x => x.Ts));
                }
            }
        }

        return sideData;
    }

    private void TrimPacket(OggPacket packet, ulong delay, Option<ulong> numFrames)
    {
        packet.TrimStart = (packet.Ts < delay) ? (uint)Math.Min(delay - packet.Ts, packet.Dur) : 0;

        if (packet.TrimStart > 0)
        {
            packet.Ts = 0;
            packet.Dur -= packet.TrimStart;
        }
        else
        {
            packet.Ts -= delay;
        }

        if (numFrames.IsSome)
        {
            var val = numFrames.ValueUnsafe()!;
            packet.TrimEnd = (packet.Ts + packet.Dur > val)
                ? (uint)Math.Min(packet.Ts + packet.Dur - val, packet.Dur)
                : 0;

            if (packet.TrimEnd > 0)
            {
                packet.Dur -= packet.TrimEnd;
            }
        }
    }


    private Result<Unit> SavePartialPacket(byte[] buf)
    {
        var newPartLen = this._partLen + buf.Length;

        if (newPartLen > this._partBuf.Length)
        {
            // Do not exceed an a certain limit to prevent unbounded memory growth.
            if (newPartLen > MAX_PACKET_LEN)
            {
                return new Result<Unit>(new DecodeError("ogg: packet buffer would exceed maximum size"));
            }

            // New partial packet buffer size, rounded up to the nearest 8K block.
            //            let new_buf_len = (new_part_len + (8 * 1024 - 1)) & !(8 * 1024 - 1);
            var newBufLen = (newPartLen + (8 * 1024 - 1)) & ~(8 * 1024 - 1);
            Log.Debug("increasing partial packet buffer size to {0}", newBufLen);

            var prevBuf = this._partBuf;
            Array.Resize(ref this._partBuf, newBufLen);
            //Fill the rest of the buffer with 0s
            Array.Fill(this._partBuf, (byte)0, prevBuf.Length, newBufLen - prevBuf.Length);
        }

        Array.Copy(buf, 0, this._partBuf, this._partLen, buf.Length);
        this._partLen = newPartLen;

        return new Result<Unit>(Unit.Default);
    }

    private byte[] GetPacket(byte[] packetBuf)
    {
        if (_partLen == 0)
        {
            return packetBuf;
        }

        ArraySegment<byte> buf = new byte[_partLen + packetBuf.Length];

        // Split packet buffer into two portions: saved and new.
        var (vec0, vec1) = buf.SplitAt(_partLen);

        // Copy and consume the saved partial packet.
        Array.Copy(this._partBuf, 0, vec0.Array!, vec0.Offset, _partLen);
        _partLen = 0;

        // Read the remainder of the partial packet from the page. (packetBuf to vec1)
        Array.Copy(packetBuf, 0, vec1.Array!, vec1.Offset, packetBuf.Length);

        return buf.Array!;
    }

    public void InspectStartPage(OggPage page)
    {
        if (this._startBound.IsSome)
        {
            Log.Debug("start page already found");
            return;
        }

        var parserResult = _mapper.MakeParser();
        if (parserResult.IsNone)
        {
            Log.Debug("failed to make start bound packet parser");
            return;
        }

        var parser = parserResult.ValueUnsafe()!;

        // Calculate the page duration.
        ulong pageDur = 0;
        using var packets = page.Packets();
        var current = packets.Current;
        while (current.IsSome)
        {
            var buf = current.ValueUnsafe()!;
            pageDur = pageDur.SaturatingAdd(parser.ParseNextPacketDur(buf.AsSpan()));
            current = packets.Next();
        }

        var pageEndTs = _mapper.AbsGpToTs(page.Header.AbsGp);

        // If the page timestamp is >= the page duration, then the stream starts at timestamp 0 or
        // a positive start time.

        Bound bound;
        if (pageEndTs >= pageDur)
        {
            bound = new Bound(
                Seq: page.Header.Sequence,
                Ts: pageEndTs - pageDur,
                Delay: 0
            );
        }
        else
        {
            // If the page timestamp < the page duration, then the difference is the start delay.
            bound = new Bound(
                Seq: page.Header.Sequence,
                Ts: 0,
                Delay: pageDur - pageEndTs
            );
        }

        // Update codec parameters.
        var codecParams = _mapper.CodecParams;

        codecParams.WithStartTs(bound.Ts);

        if (bound.Delay > 0)
        {
            codecParams.WithDelay((uint)bound.Delay);
        }

        //Update the start bound.
        _startBound = Option<Bound>.Some(bound);
    }

    /// <summary>
    /// Examines one or more of the last pages of the codec bitstream to obtain the end time and
    /// end delay parameters. To obtain the end delay, at a minimum, the last two pages are
    /// required. The state returned by each iteration of this function should be passed into the
    /// subsequent iteration.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public InspectState InspectEndPage(InspectState state, OggPage page)
    {
        if (this._endBound.IsSome)
        {
            Log.Debug("end page already found");
            return state;
        }

        // Get and/or create the sniffer state.
        if (state.Parser.IsNone)
        {
            state = state with { Parser = _mapper.MakeParser() };

            if (state.Parser.IsNone)
            {
                Log.Debug("failed to make end bound packet parser");
                return state;
            }
        }

        var startDelay = _startBound.Map(x => x.Delay).IfNone(0);

        // The actual page end timestamp is the absolute granule position + the start delay.
        var pageEndTs = _mapper.AbsGpToTs(page.Header.AbsGp).SaturatingAdd(this._gapless ? 0 : startDelay);

        // Calculate the page duration. Note that even though only the last page uses this duration,
        // it is important to feed the packet parser so that the first packet of the final page
        // doesn't have a duration of 0 due to lapping on some codecs.
        ulong pageDur = 0;

        using var packets = page.Packets();
        var current = packets.Current;
        while (current.IsSome)
        {
            var buf = current.ValueUnsafe()!;
            pageDur = pageDur.SaturatingAdd(state.Parser.ValueUnsafe()!.ParseNextPacketDur(buf.AsSpan()));
            current = packets.Next();
        }

        // The end delay can only be determined if this is the last page, and the timstamp of the
        // second last page is known.
        ulong endDelay = 0;
        if (page.Header.IsLastPage)
        {
            if (state.Bound.IsSome)
            {
                var lastBound = state.Bound.ValueUnsafe();
                // The real ending timestamp of the decoded data is the timestamp of the previous
                // page plus the decoded duration of this page.
                var actualPageEndTs = lastBound.Ts.SaturatingAdd(pageDur);

                // Any samples after the stated timestamp of this page are considered delay samples.

                if (actualPageEndTs > pageEndTs)
                {
                    endDelay = actualPageEndTs - pageEndTs;
                }
            }
        }

        var bound = new Bound(
            Seq: page.Header.Sequence,
            Ts: pageEndTs,
            Delay: endDelay
        );

        // If this is the last page, update the codec parameters.
        if (page.Header.IsLastPage)
        {
            var codecParams = _mapper.CodecParams;

            // Do not report the end delay if gapless is enabled.
            var blockEndTs = bound.Ts + (_gapless ? 0 : bound.Delay);

            if (blockEndTs > codecParams.StartTs)
            {
                codecParams.WithNFrames(blockEndTs - codecParams.StartTs);
            }

            if (bound.Delay > 0)
            {
                codecParams.WithPadding((uint)bound.Delay);
            }

            // Update the end bound.
            this._endBound = Option<Bound>.Some(bound);
        }

        return state with { Bound = Option<Bound>.Some(bound) };
    }

    public (ulong startTs, ulong endTs) InspectPage(OggPage page)
    {
        // Get the start delay.
        var startDelay = this._startBound.Map(x => x.Delay).IfNone(0);

        // Get the cumulative duration of all packets within this page.
        ulong pageDur = 0;

        var parserMaybe = _mapper.MakeParser();
        if (parserMaybe.IsSome)
        {
            var parser = parserMaybe.ValueUnsafe()!;
            using var packets = page.Packets();
            var current = packets.Current;
            while (current.IsSome)
            {
                var buf = current.ValueUnsafe()!;
                pageDur = pageDur.SaturatingAdd(parser.ParseNextPacketDur(buf.AsSpan()));
                current = packets.Next();
            }
        }

        // If this is the final page, get the end delay.
        ulong endDelay = 0;
        if (page.Header.IsLastPage)
        {
            endDelay = this._endBound.Map(x => x.Delay).IfNone(0);
        }

        // The total delay.
        var delay = startDelay + endDelay;

        // Add the total delay to the page end timestamp.
        var pageEndTs = _mapper.AbsGpToTs(page.Header.AbsGp).SaturatingAdd(delay);

        // Get the page start timestamp of the page by subtracting the cumulative packet duration.
        var pageStartTs = pageEndTs.SaturatingSub(pageDur);

        if (!this._gapless)
        {
            // If gapless playback is disabled, then report the start and end timestamps with the
            // delays incorporated.
            return (pageStartTs, pageEndTs);
        }

        // If gapless playback is enabled, report the start and end timestamps without the
        // delays.
        return (pageStartTs.SaturatingSub(startDelay), pageEndTs.SaturatingSub(startDelay));
    }

    public void Reset()
    {
        _partLen = 0;
        _prevPageInfo = Option<OggPageInfo>.None;
        _packets.Clear();
        _mapper.Reset();
    }

    public Option<OggPacket> PeekPacket() => _packets.HeadOrNone();

    public void ConsumePacket()
    {
        if (_packets.Count > 0)
            _packets.RemoveFirst();
    }

    public Option<OggPacket> NextPacket()
    {
        if (_packets.Count > 0)
        {
            var packet = _packets.First.Value;
            _packets.RemoveFirst();
            return Option<OggPacket>.Some(packet);
        }
        
        return Option<OggPacket>.None;
    }
}