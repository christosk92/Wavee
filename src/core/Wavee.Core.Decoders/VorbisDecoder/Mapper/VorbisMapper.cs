using System.Text;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Decoders.VorbisDecoder.Format;
using Wavee.Core.Decoders.VorbisDecoder.Format.Tags;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Io;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Stream;

namespace Wavee.Core.Decoders.VorbisDecoder.Mapper;

internal sealed class VorbisMapper : IMapper
{
    public static byte[] VORBIS_HEADER_PACKET_SIGNATURE = "vorbis"u8.ToArray();

    private const byte VORBIS_PACKET_TYPE_IDENTIFICATION = 1;
    private const byte VORBIS_PACKET_TYPE_COMMENT = 3;
    public const byte VORBIS_PACKET_TYPE_SETUP = 5;
    private const int VORBIS_IDENTIFICATION_HEADER_SIZE = 30;
    internal const uint CODEC_TYPE_VORBIS = 0x1000;
    private const byte VORBIS_BLOCKSIZE_MIN = 6;

    /// The maximum block size (8192) expressed as a power-of-2 exponent.
    private const byte VORBIS_BLOCKSIZE_MAX = 13;

    private Option<VorbisPacketParser> _parser;
    private readonly CodecParameters _codecParams;
    private readonly IdentHeader _ident;
    private bool _hasSetupHeader;

    public VorbisMapper(CodecParameters codecParams, IdentHeader ident, bool hasSetupHeader)
    {
        _codecParams = codecParams;
        _ident = ident;
        _hasSetupHeader = hasSetupHeader;
        _parser = Option<VorbisPacketParser>.None;
    }

    public static Option<IMapper> Detect(byte[] buf)
    {
        // The identification header packet must be the correct size.
        if (buf.Length != VORBIS_IDENTIFICATION_HEADER_SIZE)
            return Option<IMapper>.None;

        // Read the identification header. Any errors cause detection to fail.
        var identMaybe = ReadIdentHeader(new BufReader(buf));
        if (identMaybe.IsFaulted)
            return Option<IMapper>.None;
        var ident = identMaybe.Success();

        var codec = new CodecParameters();
        codec.ForCodec(CODEC_TYPE_VORBIS)
            .WithSampleRate(ident.SampleRate)
            .WithTimeBase(new TimeBase(1, ident.SampleRate))
            .WithExtraData(buf);

        var channelsMaybe = VorbisChannelsToChannels(ident.NChannels);
        if (channelsMaybe.IsSome)
        {
            codec.WithChannels(channelsMaybe.ValueUnsafe());
        }

        var mapper = new VorbisMapper(codec, ident, hasSetupHeader: false);
        return Option<IMapper>.Some(mapper);
    }

    public string Name => "vorbis";
    public CodecParameters CodecParams => _codecParams;
    public bool IsReady => true;

    public Result<IMapResult> MapPacket(byte[] packet)
    {
        var reader = new BufReader(packet);

        // All Vorbis packets indicate the packet type in the first byte.
        var packetType = reader.ReadByte();

        // An even numbered packet type is an audio packet.
        if ((packetType & 1) == 0)
        {
            var dur = this._parser.Match(
                Some: p => p.ParseNextPacketDuration(packet),
                None: () => (ulong)0
            );

            return new StreamDataMapResult(
                Dur: dur
            );
        }
        else
        {
            // Odd numbered packet types are header packets.
            Span<byte> sig = stackalloc byte[6];
            reader.ReadBufferExactly(sig);

            var a = "";
            // Check if the presumed header packet has the common header packet signature.
            if (!sig.SequenceEqual(VORBIS_HEADER_PACKET_SIGNATURE.AsSpan()))
                return new Result<IMapResult>(new DecodeError("ogg (vorbis): header packet signature invalid"));

            // Handle each header packet type specifically.
            switch (packetType)
            {
                case VORBIS_PACKET_TYPE_COMMENT:
                {
                    var builder = new VorbisMetadataBuilder();
                    var readCommentsREsult = ReadCommentNoFraming(reader, builder);
                    if (readCommentsREsult.IsFaulted)
                        return new Result<IMapResult>(readCommentsREsult.Error());

                    return new SideData(
                        Revision: builder.Metadata
                    );
                    break;
                }
                case VORBIS_PACKET_TYPE_SETUP:
                {
                    // Append the setup headers to the extra data.
                    var extraData = this._codecParams.ExtraData.ValueUnsafe();
                    this._codecParams.ExtraData = Option<byte[]>.None;
                    var together = new byte[extraData.Length + packet.Length];
                    extraData.CopyTo(together, 0);
                    packet.CopyTo(together, extraData.Length);

                    // Try to read the setup header.
                    var readSetupResult = ReadSetup(new BufReader(packet), this._ident);
                    if (readSetupResult.IsSuccess)
                    {
                        var modes = readSetupResult.Success();
                        var numModes = modes.Length;
                        ulong modesBlockFlags = 0;
                        if (numModes > 64)
                            return new Result<IMapResult>(new DecodeError("ogg (vorbis): too many modes"));
                        for (var index = 0; index < modes.Length; index++)
                        {
                            var mode = modes[index];
                            if (mode.BlockFlag)
                            {
                                modesBlockFlags |= (ulong)1 << index;
                            }
                        }

                        var parser = new VorbisPacketParser(
                            numModes: (byte)numModes,
                            modesBlockFlags: modesBlockFlags,
                            bs0Exp: this._ident.Bs0Exp,
                            bs1Exp: this._ident.Bs1Exp
                        );
                        _parser = Option<VorbisPacketParser>.Some(parser);
                    }

                    _codecParams.WithExtraData(together);
                    _hasSetupHeader = true;

                    return new Result<IMapResult>(new SetupMapResult());
                }
                default:
                {
                    //Log.Warning("ogg (vorbis): packet type {PacketType} unexpected", packetType);

                    return new Result<IMapResult>(new UnknownMapResult());
                }
            }
        }

        throw new NotImplementedException();
    }

    private Result<Mode[]> ReadSetup(BufReader reader, IdentHeader ident)
    {
        // The packet type must be an setup header.
        var packetType = reader.ReadByte();

        if (packetType != VORBIS_PACKET_TYPE_SETUP)
            return new Result<Mode[]>(new DecodeError("ogg (vorbis): invalid packet type for setup header"));

        // Next, the setup packet signature must be correct.
        Span<byte> packetSigBuf = stackalloc byte[6];
        var r = reader.ReadBufferExactly(packetSigBuf);
        if (r.IsFaulted)
            return new Result<Mode[]>(r.Error());

        if (!packetSigBuf.SequenceEqual(VORBIS_HEADER_PACKET_SIGNATURE.AsSpan()))
            return new Result<Mode[]>(new DecodeError("ogg (vorbis): invalid packet signature for setup header"));

        // The remaining portion of the setup header packet is read bitwise.
        var bs = new BitReaderRtlRef(reader.ReadBufBytesAvailableRef());

        // Skip the codebooks.
        var skcr = SkipCodebooks(ref bs);
        if (skcr.IsFaulted)
            return new Result<Mode[]>(skcr.Error());

        // Skip the time-domain transforms (placeholders in Vorbis 1).
        var skptdtr = SkipTimeDomainTransforms(ref bs);
        if (skptdtr.IsFaulted)
            return new Result<Mode[]>(skptdtr.Error());

        // Skip the floors.
        var skpflr = SkipFloors(ref bs);
        if (skpflr.IsFaulted)
            return new Result<Mode[]>(skpflr.Error());

        // Skip the residues.
        var skpresdr = SkipResidues(ref bs);
        if (skpresdr.IsFaulted)
            return new Result<Mode[]>(skpresdr.Error());

        // Skip the channel mappings.
        var skipchannelsres = SkipChannelMappings(ref bs, ident.NChannels);
        if (skipchannelsres.IsFaulted)
            return new Result<Mode[]>(skipchannelsres.Error());

        // Read modes.
        var modesR = ReadModes(ref bs);
        if (modesR.IsFaulted)
            return new Result<Mode[]>(modesR.Error());

        // Framing flag must be set.
        if (!bs.ReadBool())
            return new Result<Mode[]>(new DecodeError("ogg (vorbis): framing flag not set for setup header"));

        return new Result<Mode[]>(modesR.Success());
    }

    private Result<Unit> SkipChannelMappings(ref BitReaderRtlRef bs, byte audioChannels)
    {
        static Result<Unit> SkipMapping0Setup(ref BitReaderRtlRef bs, byte audioChannels)
        {
            uint numSubmaps = 1;
            if (bs.ReadBool())
            {
                var numSubmapsR = bs.ReadBitsLeq32(4).Map(x => x + 1);
                if (numSubmapsR.IsFaulted)
                    return new Result<Unit>(numSubmapsR.Error());
                numSubmaps = (uint)numSubmapsR.Success();
            }

            if (bs.ReadBool())
            {
                // Number of channel couplings (up-to 256).
                var couplingStepsR = bs.ReadBitsLeq32(8).Map(x => (ushort)(x + 1));
                if (couplingStepsR.IsFaulted)
                    return new Result<Unit>(couplingStepsR.Error());
                var couplingSteps = couplingStepsR.Success();
                // The maximum channel number.
                var maxCh = audioChannels - 1;

                // The number of bits to read for the magnitude and angle channel numbers. Never exceeds 8.
                var couplingBits = ((uint)maxCh).ilog();
                if (couplingBits > 8)
                    return new Result<Unit>(new DecodeError("ogg (vorbis): invalid coupling bits"));

                // Read each channel coupling.
                for (int i = 0; i < couplingSteps; i++)
                {
                    var magR = bs.ReadBitsLeq32(couplingBits).Map(x => (byte)x);
                    if (magR.IsFaulted)
                        return new Result<Unit>(magR.Error());
                    var angR = bs.ReadBitsLeq32(couplingBits).Map(x => (byte)x);
                    if (angR.IsFaulted)
                        return new Result<Unit>(angR.Error());
                }
            }

            var mappingReservedR = bs.ReadBitsLeq32(2);
            if (mappingReservedR.IsFaulted)
                return new Result<Unit>(mappingReservedR.Error());
            var mappingReserved = mappingReservedR.Success();
            if (mappingReserved != 0)
                return new Result<Unit>(new DecodeError("ogg (vorbis): reserved mapping bits non-zero"));

            // If the number of submaps is > 1 read the multiplex numbers from the bitstream, otherwise
            // they're all 0.
            if (numSubmaps > 1)
            {
                // Mux to use per channel.
                bs.IgnoreBits((uint)(audioChannels * 4));
            }

            // Reserved, floor, and residue to use per submap.
            bs.IgnoreBits(numSubmaps * (8 + 8 + 8));
            return new Result<Unit>(Unit.Default);
        }

        static Result<Unit> SkipMappingSetup(ref BitReaderRtlRef bs, byte audioChannels)
        {
            var mappingTypeR = bs.ReadBitsLeq32(16);
            if (mappingTypeR.IsFaulted)
                return new Result<Unit>(mappingTypeR.Error());
            return mappingTypeR.Success() switch
            {
                0 => SkipMapping0Setup(ref bs, audioChannels),
                _ => new Result<Unit>(new DecodeError("ogg (vorbis): invalid channel mapping type"))
            };
        }

        var countR = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countR.IsFaulted)
            return new Result<Unit>(countR.Error());
        for (int i = 0; i < countR.Success(); i++)
        {
            var r = SkipMappingSetup(ref bs, audioChannels);
            if (r.IsFaulted)
                return new Result<Unit>(r.Error());
        }

        return new Result<Unit>(Unit.Default);
    }

    private Result<Unit> SkipResidues(ref BitReaderRtlRef bs)
    {
        static Result<Unit> SkipResidueSetup(ref BitReaderRtlRef bs)
        {
            // residue_begin
            // residue_end
            // residue_partition_size
            bs.IgnoreBits(24 + 24 + 24);

            var residueClassificationsR = bs.ReadBitsLeq32(6).Map(x => (byte)(x + 1));
            if (residueClassificationsR.IsFaulted)
                return new Result<Unit>(residueClassificationsR.Error());

            // residue_classbook
            bs.IgnoreBits(8);

            uint numCodebooks = 0;

            for (int i = 0; i < residueClassificationsR.Success(); i++)
            {
                var lowBitsR = bs.ReadBitsLeq32(3).Map(x => (byte)x);
                if (lowBitsR.IsFaulted)
                    return new Result<Unit>(lowBitsR.Error());
                byte highBits = 0;
                if (bs.ReadBool())
                {
                    var readResult = bs.ReadBitsLeq32(5).Map(x => (byte)x);
                    if (readResult.IsFaulted)
                        return new Result<Unit>(readResult.Error());

                    highBits = readResult.Success();
                }

                var isUsed = (highBits << 3) | lowBitsR.Success();
                numCodebooks += (uint)isUsed.CountOnes();
            }

            bs.IgnoreBits((uint)(numCodebooks * 8));

            return new Result<Unit>(Unit.Default);
        }

        var countR = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countR.IsFaulted)
            return new Result<Unit>(countR.Error());

        for (int i = 0; i < countR.Success(); i++)
        {
            var residueTypeR = bs.ReadBitsLeq32(16);
            if (residueTypeR.IsFaulted)
                return new Result<Unit>(residueTypeR.Error());

            var r = SkipResidueSetup(ref bs);
            if (r.IsFaulted)
                return new Result<Unit>(r.Error());
        }

        return new Result<Unit>(Unit.Default);
    }

    private Result<Unit> SkipFloors(ref BitReaderRtlRef bs)
    {
        static Result<Unit> SkipFloor0Setup(ref BitReaderRtlRef bs)
        {
            // floor0_order
            // floor0_rate
            // floor0_bark_map_size
            // floor0_amplitude_bits
            // floor0_amplitude_offset
            bs.IgnoreBits(8 + 16 + 16 + 6 + 8);
            var floor0NumberOfBooksR = bs.ReadBitsLeq32(4).Map(x => x + 1);
            if (floor0NumberOfBooksR.IsFaulted)
                return new Result<Unit>(floor0NumberOfBooksR.Error());
            bs.IgnoreBits((uint)(8 * floor0NumberOfBooksR.Success()));
            return new Result<Unit>(Unit.Default);
        }

        static Result<Unit> SkipFloor1Setup(ref BitReaderRtlRef bs)
        {
            // The number of partitions. 5-bit value, 0..31 range.
            var partitionsR = bs.ReadBitsLeq32(5);
            if (partitionsR.IsFaulted)
                return new Result<Unit>(partitionsR.Error());
            var partitions = partitionsR.Success();
            // Parition list of up-to 32 partitions (floor1_partitions), with each partition indicating
            // a 4-bit class (0..16) identifier.
            Span<uint> partitionClasses = stackalloc uint[32];
            Span<uint> partitionClassDimensions = stackalloc uint[16];

            if (partitions > 0)
            {
                byte maxClass = 0;
                //        for class_idx in &mut floor1_partition_class_list[..floor1_partitions] {
                for (int i = 0; i < partitions; i++)
                {
                    var classR = bs.ReadBitsLeq32(4).Map(x => (byte)x);
                    if (classR.IsFaulted)
                        return new Result<Unit>(classR.Error());
                    var classIdx = classR.Success();
                    partitionClasses[i] = classIdx;
                    maxClass = Math.Max(maxClass, classIdx);
                }

                var numClasses = maxClass + 1;
                // for dimensions in floor1_classes_dimensions[..num_classes].iter_mut() {
                for (int i = 0; i < numClasses; i++)
                {
                    var dimensionsR = bs.ReadBitsLeq32(3).Map(x => (byte)(x + 1));
                    if (dimensionsR.IsFaulted)
                        return new Result<Unit>(dimensionsR.Error());
                    var dimensions = dimensionsR.Success();
                    partitionClassDimensions[i] = dimensions;

                    var subclassBitsR = bs.ReadBitsLeq32(2);
                    if (subclassBitsR.IsFaulted)
                        return new Result<Unit>(subclassBitsR.Error());
                    var subclassBits = subclassBitsR.Success();

                    if (subclassBits != 0)
                    {
                        var _mainBookR = bs.ReadBitsLeq32(8);
                        if (_mainBookR.IsFaulted)
                            return new Result<Unit>(_mainBookR.Error());
                    }

                    var numSubclasses = 1 << (int)subclassBits;

                    // Sub-class books
                    bs.IgnoreBits((uint)(8 * numSubclasses));
                }
            }

            var _floor1_multiplierR = bs.ReadBitsLeq32(2);
            if (_floor1_multiplierR.IsFaulted)
                return new Result<Unit>(_floor1_multiplierR.Error());

            var rangebitsR = bs.ReadBitsLeq32(4);
            if (rangebitsR.IsFaulted)
                return new Result<Unit>(rangebitsR.Error());
            //    for &class_idx in &floor1_partition_class_list[..floor1_partitions] {
            for (int i = 0; i < partitions; i++)
            {
                var classIdx = partitionClasses[i];
                var dimensions = partitionClassDimensions[(int)classIdx];
                // TODO? No more than 65 elements are allowed.
                bs.IgnoreBits((uint)(dimensions * rangebitsR.Success()));
            }

            return new Result<Unit>(Unit.Default);
        }

        static Result<Unit> SkipFloor(ref BitReaderRtlRef bs)
        {
            var floorTypeR = bs.ReadBitsLeq32(16);
            if (floorTypeR.IsFaulted)
                return new Result<Unit>(floorTypeR.Error());

            var floorType = floorTypeR.Success();
            switch (floorType)
            {
                case 0:
                    return SkipFloor0Setup(ref bs);
                case 1:
                    return SkipFloor1Setup(ref bs);
                default:
                    return new Result<Unit>(new DecodeError("ogg (vorbis): invalid floor type"));
            }
        }

        var count = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (count.IsFaulted)
            return new Result<Unit>(count.Error());

        for (int i = 0; i < count.Success(); i++)
        {
            var r = SkipFloor(ref bs);
            if (r.IsFaulted)
                return new Result<Unit>(r.Error());
        }

        return new Result<Unit>(Unit.Default);
    }

    private Result<Unit> SkipTimeDomainTransforms(ref BitReaderRtlRef bs)
    {
        var countResult = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countResult.IsFaulted)
            return new Result<Unit>(countResult.Error());

        for (int i = 0; i < countResult.Success(); i++)
        {
            // All these values are placeholders and must be 0.
            var zero = bs.ReadBitsLeq32(16);
            if (zero.IsFaulted)
                return new Result<Unit>(zero.Error());

            if (zero.Success() != 0)
            {
                return new Result<Unit>(new DecodeError("ogg (vorbis): invalid time-domain transform"));
            }
        }

        return new Result<Unit>(Unit.Default);
    }

    public static Result<Mode[]> ReadModes(ref BitReaderRtlRef bs)
    {
        var countr = bs.ReadBitsLeq32(6).Map(x => x + 1);
        if (countr.IsFaulted)
            return new Result<Mode[]>(countr.Error());

        var output = new Mode[countr.Success()];
        for (int i = 0; i < output.Length; i++)
        {
            var r = ReadMode(ref bs);
            if (r.IsFaulted)
                return new Result<Mode[]>(r.Error());
            output[i] = r.Success();
        }

        return output;

        static Result<Mode> ReadMode(ref BitReaderRtlRef bs)
        {
            var blockFlag = bs.ReadBool();
            var windowTypeR = bs.ReadBitsLeq32(16).Map(x => (ushort)x);
            if (windowTypeR.IsFaulted)
                return new Result<Mode>(windowTypeR.Error());
            var windowType = windowTypeR.Success();

            var transformTypeR = bs.ReadBitsLeq32(16).Map(x => (ushort)x);
            if (transformTypeR.IsFaulted)
                return new Result<Mode>(transformTypeR.Error());
            var transformType = transformTypeR.Success();

            var mappingR = bs.ReadBitsLeq32(8).Map(x => (byte)x);
            if (mappingR.IsFaulted)
                return new Result<Mode>(mappingR.Error());

            // Only window type 0 is allowed in Vorbis 1 (section 4.2.4).
            if (windowType != 0)
                return new Result<Mode>(new DecodeError("ogg (vorbis): invalid window type"));

            // Only transform type 0 is allowed in Vorbis 1 (section 4.2.4).
            if (transformType != 0)
                return new Result<Mode>(new DecodeError("ogg (vorbis): invalid transform type"));

            var mode = new Mode(
                BlockFlag: blockFlag,
                Mapping: mappingR.Success()
            );

            return new Result<Mode>(mode);
        }
    }

    private Result<Unit> SkipCodebooks(ref BitReaderRtlRef bs)
    {
        static Result<Unit> SkipCodebook(ref BitReaderRtlRef bs)
        {
            // Verify codebook synchronization word.
            var syncR = bs.ReadBitsLeq32(24);
            if (syncR.IsFaulted)
                return new Result<Unit>(syncR.Error());

            if (syncR.Success() != 0x564342)
                return new Result<Unit>(new DecodeError("ogg (vorbis): invalid codebook sync word"));

            // Read codebook number of dimensions and entries.
            var codebookDimensionsResult = bs.ReadBitsLeq32(16);
            if (codebookDimensionsResult.IsFaulted)
                return new Result<Unit>(codebookDimensionsResult.Error());
            var codebookEntriesResult = bs.ReadBitsLeq32(24);
            if (codebookEntriesResult.IsFaulted)
                return new Result<Unit>(codebookEntriesResult.Error());

            var isLengthOrdered = bs.ReadBool();

            var codebookDimensions = (ushort)codebookDimensionsResult.Success();
            var codebookEntries = (uint)codebookEntriesResult.Success();

            if (!isLengthOrdered)
            {
                // Codeword list is not length ordered.
                var isSparse = bs.ReadBool();
                if (isSparse)
                {
                    // Sparsely packed codeword entry list.
                    for (int i = 0; i < codebookEntries; i++)
                    {
                        if (bs.ReadBool())
                        {
                            var a = bs.ReadBitsLeq32(5);
                            if (a.IsFaulted)
                                return new Result<Unit>(a.Error());
                        }
                    }
                }
                else
                {
                    bs.IgnoreBits(codebookEntries * 5);
                }
            }
            else
            {
                // Codeword list is length ordered.
                uint curEntry = 0;
                var curLenResult = bs.ReadBitsLeq32(5);
                if (curLenResult.IsFaulted)
                    return new Result<Unit>(curLenResult.Error());
                var curLen = curLenResult.Success() + 1;

                while (true)
                {
                    uint numBits = 0;
                    if (codebookEntries > curEntry)
                    {
                        numBits = (codebookEntries - curEntry).ilog();
                    }

                    var numR = bs.ReadBitsLeq32(numBits);
                    if (numR.IsFaulted)
                        return new Result<Unit>(numR.Error());

                    curEntry += (uint)numR.Success();

                    if (curEntry > codebookEntries)
                        return new Result<Unit>(new DecodeError("ogg (vorbis): invalid codebook"));

                    if (curEntry == codebookEntries)
                        break;
                }
            }

            // Read and unpack vector quantization (VQ) lookup table.
            var lookupTypeR = bs.ReadBitsLeq32(4);
            if (lookupTypeR.IsFaulted)
                return new Result<Unit>(lookupTypeR.Error());

            var lookupType = lookupTypeR.Success();
            switch ((lookupType & 0xf))
            {
                case 0:
                    break;
                case 1 or 2:
                {
                    var minValueR = bs.ReadBitsLeq32(32);
                    if (minValueR.IsFaulted)
                        return new Result<Unit>(minValueR.Error());
                    var deltaValueR = bs.ReadBitsLeq32(32);
                    if (deltaValueR.IsFaulted)
                        return new Result<Unit>(deltaValueR.Error());
                    var valueBitsR = bs.ReadBitsLeq32(4);
                    if (valueBitsR.IsFaulted)
                        return new Result<Unit>(valueBitsR.Error());
                    var valueBits = valueBitsR.Success() + 1;
                    var sequenceP = bs.ReadBool();

                    // Lookup type is either 1 or 2 as per outer match.
                    var lookupValues = lookupType switch
                    {
                        1 => lookup1_values(codebookEntries, codebookDimensions),
                        2 => codebookEntries * (ushort)codebookDimensions
                    };
                    // Multiplicands
                    bs.IgnoreBits(lookupValues * (uint)valueBits);
                    break;
                }
                default:
                    return new Result<Unit>(new DecodeError("ogg (vorbis): invalid codeword lookup type"));
            }

            return new Result<Unit>(Unit.Default);
        }

        var countResult = bs.ReadBitsLeq32(8);
        if (countResult.IsFaulted)
            return new Result<Unit>(countResult.Error());
        var count = countResult.Success() + 1;

        for (int _ = 0; _ < count; _++)
        {
            var r = SkipCodebook(ref bs);
            if (r.IsFaulted)
                return new Result<Unit>(r.Error());
        }

        return new Result<Unit>(Unit.Default);
    }


    public static uint lookup1_values(uint entries, ushort dimensions)
    {
        var r = (uint)Math.Floor(Math.Exp(Math.Log(entries) / dimensions));

        if (Math.Floor(Math.Pow(r + 1, dimensions)) <= entries) ++r;

        return r;
    }

    public static Result<IdentHeader> ReadIdentHeader<TB>(TB reader) where TB : IReadBytes
    {
        // The packet type must be an identification header.
        try
        {
            var packetType = reader.ReadByte();
            if (packetType != VORBIS_PACKET_TYPE_IDENTIFICATION)
                return new Result<IdentHeader>(
                    new DecodeError("ogg (vorbis): invalid packet type for identification header"));

            // Next, the header packet signature must be correct.
            Span<byte> packetSigBuf = stackalloc byte[6];
            var readResult = reader.ReadBufferExactly(packetSigBuf);
            if (readResult.IsFaulted)
                return new Result<IdentHeader>(readResult.Error());

            // Next, the Vorbis version must be 0.
            var versionMaybe = reader.ReadUInt();
            if (versionMaybe.IsFaulted)
                return new Result<IdentHeader>(versionMaybe.Error());
            var version = versionMaybe.Success();
            if (version != 0)
                return new Result<IdentHeader>(
                    new DecodeError("ogg (vorbis): invalid version for identification header"));

            // Next, the number of channels and sample rate must be non-zero.
            var n_channels = reader.ReadByte();

            if (n_channels == 0)
                return new Result<IdentHeader>(
                    new DecodeError("ogg (vorbis): invalid number of channels for identification header"));

            var sampleRateMaybe = reader.ReadUInt();
            if (sampleRateMaybe.IsFaulted)
                return new Result<IdentHeader>(sampleRateMaybe.Error());

            // Read the bitrate range.
            var _bitrate_max = reader.ReadUInt();
            var _bitrate_nominal = reader.ReadUInt();
            var _bitrate_min = reader.ReadUInt();
            if (_bitrate_max.IsFaulted || _bitrate_nominal.IsFaulted || _bitrate_min.IsFaulted)
                return new Result<IdentHeader>(
                    new DecodeError("ogg (vorbis): invalid bitrate range for identification header"));

            // Next, blocksize_0 and blocksize_1 are packed into a single byte.
            var blockSizes = reader.ReadByte();


            byte bs0Exp = (byte)((blockSizes & 0x0F) >> 0);
            byte bs1Exp = (byte)((blockSizes & 0xF0) >> 4);

            if (bs0Exp < VORBIS_BLOCKSIZE_MIN || bs0Exp > VORBIS_BLOCKSIZE_MAX)

                return new Result<IdentHeader>(new DecodeError("ogg (vorbis): blocksize_0 out-of-bounds"));
            if (bs1Exp < VORBIS_BLOCKSIZE_MIN || bs1Exp > VORBIS_BLOCKSIZE_MAX)
                return new Result<IdentHeader>(new DecodeError("ogg (vorbis): blocksize_1 out-of-bounds"));

            //0 must be >= 1
            if (bs0Exp > bs1Exp)
                return new Result<IdentHeader>(new DecodeError("ogg (vorbis): blocksize_0 exceeds blocksize_1"));

            // Framing flag must be set.
            var framingFlag = reader.ReadByte();
            if (framingFlag != 1)
                return new Result<IdentHeader>(
                    new DecodeError("ogg (vorbis): framing flag not set for identification header"));

            return new IdentHeader(
                NChannels: n_channels,
                SampleRate: sampleRateMaybe.Success(),
                Bs0Exp: bs0Exp,
                Bs1Exp: bs1Exp
            );
        }
        catch (Exception e)
        {
            return new Result<IdentHeader>(e);
        }
    }

    private Result<Unit> ReadCommentNoFraming(BufReader reader, VorbisMetadataBuilder metadata)
    {
        // Read the vendor string length in bytes.
        var vendorLengthResult = reader.ReadUInt();
        if (vendorLengthResult.IsFaulted)
            return new Result<Unit>(vendorLengthResult.Error());
        var vendorLength = vendorLengthResult.Success();

        //Ignore the vendor string
        var ignoreBytesResult = reader.IgnoreBytes((ulong)vendorLength);
        if (ignoreBytesResult.IsFaulted)
            return new Result<Unit>(ignoreBytesResult.Error());

        // Read the number of comments.
        var nCommentsResult = reader.ReadUInt();
        if (nCommentsResult.IsFaulted)
            return new Result<Unit>(nCommentsResult.Error());

        // Read each comment.
        for (int i = 0; i < nCommentsResult.Success(); i++)
        {
            // Read the comment string length in bytes.
            var commentLengthResult = reader.ReadUInt();
            if (commentLengthResult.IsFaulted)
                return new Result<Unit>(commentLengthResult.Error());

            // Read the comment string.
            Span<byte> commentByte = new byte[commentLengthResult.Success()];
            var readBytesResult = reader.ReadBufferExactly(commentByte);
            if (readBytesResult.IsFaulted)
                return new Result<Unit>(readBytesResult.Error());

            metadata.Metadata.Tags.Add(parse(Encoding.UTF8.GetString(commentByte)));
        }

        return new Result<Unit>(Unit.Default);
    }

    private static Tag parse(string getString)
    {
        // Vorbis Comments (aka tags) are stored as <key>=<value> where <key> is
        // a reduced ASCII-only identifier and <value> is a UTF8 value.
        //
        // <Key> must only contain ASCII 0x20 through 0x7D, with 0x3D ('=') excluded.
        // ASCII 0x41 through 0x5A inclusive (A-Z) is to be considered equivalent to
        // ASCII 0x61 through 0x7A inclusive (a-z) for tag matching.

        var field = getString.Split('=');

        // Attempt to assign a standardized tag key.
        //TODO:
        var stdTag = StandardTagKey.Album;

        // The value field was empty so only the key field exists. Create an empty tag for the given
        // key field.
        if (field.Length == 1)
            return new Tag(stdTag, field[0], new StringTagValue(string.Empty));

        return new Tag(stdTag, field[0], new StringTagValue(field[1]));
    }

    public ulong AbsGpToTs(ulong ts)
    {
        return ts;
    }

    public Option<IPacketParser> MakeParser()
    {
        return new Option<IPacketParser>(_parser.Map(x =>
        {
            return new VorbisPacketParser(
                numModes: x.NumModes,
                modesBlockFlags: x.ModesBlockFlags,
                bs0Exp: x.Bs0exp,
                bs1Exp: x.Bs1exp
            );
        }));
    }

    public void Reset()
    {
        _parser.Map(x =>
        {
            x.Reset();
            return Unit.Default;
        });
    }

    public static Option<Channels> VorbisChannelsToChannels(byte numChannels)
    {
        switch (numChannels)
        {
            case 1:
                return Channels.FRONT_LEFT;
                break;
            case 2:
                return Channels.FRONT_LEFT | Channels.FRONT_RIGHT;
                break;
            case 3:
                return Channels.FRONT_LEFT | Channels.FRONT_CENTER | Channels.FRONT_RIGHT;
                break;
            case 4:
                return Channels.FRONT_LEFT | Channels.FRONT_RIGHT | Channels.REAR_LEFT | Channels.REAR_RIGHT;
                break;
            case 5:
                return Channels.FRONT_LEFT | Channels.FRONT_CENTER | Channels.FRONT_RIGHT | Channels.REAR_LEFT |
                       Channels.REAR_RIGHT;
                break;
            case 6:
                return Channels.FRONT_LEFT | Channels.FRONT_CENTER | Channels.FRONT_RIGHT | Channels.REAR_LEFT |
                       Channels.REAR_RIGHT | Channels.LFE1;
                break;
            case 7:
                return Channels.FRONT_LEFT | Channels.FRONT_CENTER | Channels.FRONT_RIGHT | Channels.SIDE_LEFT |
                       Channels.SIDE_RIGHT | Channels.REAR_CENTER | Channels.LFE1;
                break;
            case 8:
                return Channels.FRONT_LEFT | Channels.FRONT_CENTER | Channels.FRONT_RIGHT | Channels.SIDE_LEFT |
                       Channels.SIDE_RIGHT | Channels.REAR_LEFT | Channels.REAR_RIGHT | Channels.LFE1;
                break;
            default:
                return Option<Channels>.None;
        }
    }
}

internal readonly record struct Mode(bool BlockFlag, byte Mapping);

[Flags]
public enum Channels : uint
{
    /// <summary>
    /// Front-left (left) or the Mono channel.
    /// </summary>
    FRONT_LEFT = 0x0000_0001,

    /// <summary>
    /// Front-right (right) channel.
    /// </summary>
    FRONT_RIGHT = 0x0000_0002,

    /// <summary>
    /// Front-centre (centre) channel.
    /// </summary>
    FRONT_CENTER = 0x0000_0004,

    /// <summary>
    /// Low frequency channel 1.
    /// </summary>
    LFE1 = 0x0000_0008,

    /// <summary>
    /// Rear-left (surround rear left) channel.
    /// </summary>
    REAR_LEFT = 0x0000_0010,

    /// <summary>
    /// /Rear-right (surround rear right) channel.
    /// </summary>
    REAR_RIGHT = 0x0000_0020,

    /// <summary>
    /// Front left-of-centre (left center) channel.
    /// </summary>
    FRONT_LEFT_CENTER = 0x0000_0040,

    /// <summary>
    /// Front right-of-centre (right center) channel.
    /// </summary>
    FRONT_RIGHT_CENTER = 0x0000_0080,

    /// <summary>
    /// Rear-centre (surround rear centre) channel.
    /// </summary>
    REAR_CENTER = 0x0000_0100,

    /// <summary>
    /// Side left (surround left) channel.
    /// </summary>
    SIDE_LEFT = 0x0000_0200,

    /// <summary>
    /// Side right (surround right) channel.
    /// </summary>
    SIDE_RIGHT = 0x0000_0400,

    /// <summary>
    /// Top centre channel.
    /// </summary>
    TOP_CENTER = 0x0000_0800,

    /// <summary>
    /// Top front-left channel.
    /// </summary>
    TOP_FRONT_LEFT = 0x0000_1000,

    /// <summary>
    /// Top centre channel.
    /// </summary>
    TOP_FRONT_CENTER = 0x0000_2000,

    /// <summary>
    /// Top front-right channel.
    /// </summary>
    TOP_FRONT_RIGHT = 0x0000_4000,

    /// <summary>
    /// Top rear-left channel.
    /// </summary>
    TOP_REAR_LEFT = 0x0000_8000,

    /// <summary>
    /// Top rear-centre channel.
    /// </summary>
    TOP_REAR_CENTER = 0x0001_0000,

    /// <summary>
    /// Top rear-right channel.
    /// </summary>
    TOP_REAR_RIGHT = 0x0002_0000,

    /// <summary>
    /// Rear left-of-centre channel.
    /// </summary>
    REAR_LEFT_CENTER = 0x0004_0000,

    /// <summary>
    /// Rear right-of-centre channel.
    /// </summary>
    REAR_RIGHT_CENTER = 0x0008_0000,

    /// <summary>
    /// Front left-wide channel.
    /// </summary>
    FONRT_LEFT_WIDE = 0x0010_0000,

    /// <summary>
    /// Front right-wide channel.
    /// </summary>
    FRONT_RIGHT_WIDE = 0x0020_0000,

    /// <summary>
    /// Front left-high channel.
    /// </summary>
    FRONT_LEFT_HIGH = 0x0040_0000,

    /// <summary>
    /// Front centre-high channel.
    /// </summary>
    FRONT_CENTER_HIGH = 0x0080_0000,

    /// <summary>
    /// Front right-high channel.
    /// </summary>
    FRONT_RIGHT_HIGH = 0x0100_0000,

    /// <summary>
    /// Low frequency channel 2.
    /// </summary>
    LFE2 = 0x0200_0000,
}