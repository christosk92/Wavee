using System.Diagnostics;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Floor;
using Wavee.Core.Decoders.VorbisDecoder.Format;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.Io;
using Wavee.Core.Decoders.VorbisDecoder.Mapper;
using Wavee.Core.Decoders.VorbisDecoder.Packets;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder;

public sealed class VorbisDecoder
{
    private readonly CodecParameters _parameters;
    private readonly IdentHeader _ident;
    private readonly VorbisCodebook[] _codebooks;
    private readonly IFloor[] _floors;
    private readonly Residue[] _residues;
    private readonly Mode[] _modes;
    private readonly Mapping[] _mappings;
    private readonly Dsp _dsp;
    private readonly AudioBuffer<float> _buffer;

    private VorbisDecoder(CodecParameters parameters, IdentHeader ident, VorbisCodebook[] codebooks, IFloor[] floors,
        Residue[] residues, Mode[] modes, Mapping[] mappings, Dsp dsp, AudioBuffer<float> buffer)
    {
        _parameters = parameters;
        _ident = ident;
        _codebooks = codebooks;
        _floors = floors;
        _residues = residues;
        _modes = modes;
        _mappings = mappings;
        _dsp = dsp;
        _buffer = buffer;
    }

    public static Result<VorbisDecoder> TryNew(CodecParameters trackCodecParams, DecoderOptions decoderOptions)
    {
        // This decoder only supports Vorbis.
        if (trackCodecParams.CodecType != VorbisMapper.CODEC_TYPE_VORBIS)
        {
            return new Result<VorbisDecoder>(new NotSupportedException("vorbis: unsupported codec type"));
        }

        // Get the extra data (mandatory).
        var extraDataMaybe = trackCodecParams.ExtraData;
        if (extraDataMaybe.IsNone)
            return new Result<VorbisDecoder>(new NotSupportedException("vorbis: missing extradata"));
        var extraData = extraDataMaybe.ValueUnsafe();

        // The extra data contains the identification and setup headers.
        var reader = new BufReader(extraData);

        // Read ident header.
        var identHeaderResult = VorbisMapper.ReadIdentHeader(reader);
        if (identHeaderResult.IsFaulted)
        {
            return new Result<VorbisDecoder>(identHeaderResult.Error());
        }

        // Read ident header.
        var ident = identHeaderResult.Success();

        // Read setup header.
        var setupResult = VorbisSetup.ReadSetupHeader(reader, ident);
        if (setupResult.IsFaulted)
        {
            return new Result<VorbisDecoder>(setupResult.Error());
        }

        var setup = setupResult.Success();

        // Initialize static DSP data.
        var windows = new Windows(1 << ident.Bs0Exp, 1 << ident.Bs1Exp);

        // Initialize dynamic DSP for each channel.
        var dspChannels = Enumerable.Range(0, ident.NChannels)
            .Select(_ => new DspChannel(ident.Bs0Exp, ident.Bs1Exp))
            .ToArray();

        // Map the channels
        var channelsResult = VorbisMapper.VorbisChannelsToChannels(ident.NChannels);
        if (channelsResult.IsNone)
        {
            return new Result<VorbisDecoder>(new NotSupportedException("vorbis: unsupported channel layout"));
        }

        var channels = channelsResult.ValueUnsafe();

        // Initialize the output buffer.
        var spec = new SignalSpec(ident.SampleRate, channels);

        var imdctShort = new Imdct((1 << ident.Bs0Exp) >> 1);
        var imdctLong = new Imdct((1 << ident.Bs1Exp) >> 1);

        // TODO: Should this be half the block size?
        var duration = 1UL << ident.Bs1Exp;

        var dsp = new Dsp(windows, dspChannels, imdctShort, imdctLong);

        var buffer = new AudioBuffer<float>(duration, spec);

        return new VorbisDecoder(
            parameters: (CodecParameters)trackCodecParams.Clone(),
            ident: ident,
            codebooks: setup.Codebooks,
            floors: setup.Floors,
            residues: setup.Residues,
            modes: setup.Modes,
            mappings: setup.Mappings,
            dsp: dsp,
            buffer: buffer
        );
    }

    /// <summary>
    /// Decodes a `Packet` of audio data and returns a copy-on-write generic (untyped) audio buffer
    /// of the decoded audio.
    ///
    /// If a `DecodeError` or `IoError` is returned, the packet is undecodeable and should be
    /// discarded. Decoding may be continued with the next packet. If `ResetRequired` is returned,
    /// consumers of the decoded audio data should expect the duration and `SignalSpec` of the
    /// decoded audio buffer to change. All other errors are unrecoverable.
    ///
    /// Implementors of decoders *must* `clear` the internal buffer if an error occurs.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Result<float[]> Decode(OggPacket packet)
    {
        var decodeResult = this.DecodeInner(packet);
        if (decodeResult.IsFaulted)
        {
            this._buffer.Clear();
            return new Result<float[]>(decodeResult.Error());
        }

        return _buffer.AsAudioBufferRef();
    }

    private Result<Unit> DecodeInner(OggPacket packet)
    {
        var bs = new BitReaderRtlRef(packet.Data);
        // Section 4.3.1 - Packet Type, Mode, and Window Decode

        // First bit must be 0 to indicate audio packet.
        if(bs.ReadBool())
            return new Result<Unit>(new DecodeError("vorbis: not an audio packet"));
        
        var numModes = _modes.Length - 1;

        var modenumberResArg = ((uint)numModes).ilog();
        var modeNumberRes = bs.ReadBitsLeq32(modenumberResArg);
        if (modeNumberRes.IsFaulted)
            return new Result<Unit>(modeNumberRes.Error());
        var modeNumber = modeNumberRes.Success();
        
        if (modeNumber >= _modes.Length())
            return new Result<Unit>(new DecodeError("vorbis: invalid mode number"));
        
        var mode = _modes[modeNumber];
        var mapping = _mappings[mode.Mapping];

        byte bsExp = _ident.Bs0Exp;
        Imdct imdct = _dsp.ImdctShort;
        if (mode.BlockFlag)
        {
            // This packet (block) uses a long window. Do not use the window flags since they may
            // be wrong.
            var _prevWindowFlag = bs.ReadBool();
            var _nextWindowFlag = bs.ReadBool();
            
            bsExp = _ident.Bs1Exp;
            imdct = _dsp.ImdctLong;
        }
        
        // Block, and half-block size
        var n = 1 << bsExp;
        var n2 = n >> 1;
        
        // Section 4.3.2 - Floor Curve Decode

        // Read the floors from the packet. There is one floor per audio channel. Each mapping will
        // have one multiplex (submap number) per audio channel. Therefore, iterate over all
        // muxes in the mapping, and read the floor.
        foreach (var (submapNum, ch) in mapping.Multiplex.Zip(this._dsp.Channels))
        {
            var submap = _mappings[mode.Mapping].Submaps[submapNum];
            var floor = _floors[submap.Floor];
            
            // Read the floor from the bitstream.
            var res = floor.ReadChannel(ref bs, _codebooks);
            if (res.IsFaulted)
                return new Result<Unit>(res.Error());

            ch.DoNotDecode = floor.IsUnused();

            if (!ch.DoNotDecode)
            {
                // Since the same floor can be used by multiple channels and thus overwrite the
                // data just read from the bitstream, synthesize the floor curve for this channel
                // now and save it for audio synthesis later.
                var synResult = floor.Synthesis(bsExp, ch.Floor.AsSpan());
                if (synResult.IsFaulted)
                    return new Result<Unit>(synResult.Error());
            }
            else
            {
                // If the channel is unused, zero the floor vector.
                var span = ch.Floor.AsSpan()[..n2];
                span.Clear();
            }
        }
        
        // Section 4.3.3 - Non-zero Vector Propagate

        // If within a pair of coupled channels, one channel has an unused floor (do_not_decode
        // is true for that channel), but the other channel is used, then both channels must have
        // do_not_decode unset.
        foreach (var couple in mapping.Couplings)
        {
            var magnitudeChIdx = (int)couple.MagnituteCh;
            var angleChIdx = (int)couple.AngleCh;

            if (this._dsp.Channels[magnitudeChIdx].DoNotDecode != this._dsp.Channels[angleChIdx].DoNotDecode)
            {
                this._dsp.Channels[magnitudeChIdx].DoNotDecode = false;
                this._dsp.Channels[angleChIdx].DoNotDecode = false;
            }
        }
        
        // Section 4.3.4 - Residue Decode
        for (int submapIdx = 0; submapIdx < mapping.Submaps.Length; submapIdx++)
        {
            var submap = mapping.Submaps[submapIdx];
            var residueChannels = new BitSet256();
            
            // Find the channels using this submap.
            for (int c = 0; c < mapping.Multiplex.Length; c++)
            {
                var chSubmapIdx = mapping.Multiplex[c];
                if (submapIdx == (int)chSubmapIdx)
                {
                    residueChannels.Set(c);
                }
            }

            var residue = this._residues[submap.Residue];

            var readRes = residue.ReadResidue(
                ref bs,
                bsExp,
                _codebooks,
                residueChannels,
                this._dsp.Channels
            );
            if (readRes.IsFaulted)
                return new Result<Unit>(readRes.Error());
        }
        
        // Section 4.3.5 - Inverse Coupling
        foreach (var coupling in mapping.Couplings)
        {
            if (coupling.MagnituteCh == coupling.AngleCh)
                return new Result<Unit>(new DecodeError("vorbis: invalid coupling"));
            
        
            // Get mutable reference to each channel in the pair.
            DspChannel magnitudeCh, angleCh;
            if (coupling.MagnituteCh < coupling.AngleCh)
            {
                // Magnitude channel index < angle channel index.
                magnitudeCh = _dsp.Channels[coupling.MagnituteCh];
                angleCh = _dsp.Channels[coupling.AngleCh];
            }
            else
            {
                // Angle channel index < magnitude channel index.
                magnitudeCh = _dsp.Channels[coupling.AngleCh];
                angleCh = _dsp.Channels[coupling.MagnituteCh];
            }
            
            for (int i = 0; i < n2; i++)
            {
                float m = magnitudeCh.Residue[i];
                float a = angleCh.Residue[i];
                float newM, newA;

                if (m > 0.0f)
                {
                    if (a > 0.0f)
                    {
                        newM = m;
                        newA = m - a;
                    }
                    else
                    {
                        newM = m + a;
                        newA = m;
                    }
                }
                else
                {
                    if (a > 0.0f)
                    {
                        newM = m;
                        newA = m + a;
                    }
                    else
                    {
                        newM = m - a;
                        newA = m;
                    }
                }

                magnitudeCh.Residue[i] = newM;
                angleCh.Residue[i] = newA;
            }
        }
        
        // Section 4.3.6 - Dot Product
        foreach (var channel in _dsp.Channels)
        {
            // If the channel is marked as do not decode, the floor vector is all 0. Therefore the
            // dot product will be 0.
            if (channel.DoNotDecode)
                continue;
            
            for (int i = 0; i < n2; i++)
            {
                channel.Floor[i] *= channel.Residue[i];
            }
        }
        
        // Combined Section 4.3.7 and 4.3.8 - Inverse MDCT and Overlap-add (Synthesis)
        _buffer.Clear();
        
        // Calculate the output length and reserve space in the output buffer. If there was no
        // previous packet, then return an empty audio buffer since the decoder will need another
        // packet before being able to produce audio.
        if (_dsp.LappingState is not null)
        {
            // The previous block size.
            var prevBlockSize =
                _dsp.LappingState.PrevBlockFlag ? (1 << _ident.Bs1Exp) : (1 << _ident.Bs0Exp);

            var renderLen = (prevBlockSize + n) / 4;
            var r =_buffer.RenderReserve(renderLen);
            if (r.IsFaulted)
                return new Result<Unit>(r.Error());
        }
        
        // Render all the audio channels.
        for (int i = 0; i < _dsp.Channels.Length; i++)
        {
            var mapped = MapVorbisChannel(_ident.NChannels, i);
            var channel = _dsp.Channels[i];
            channel.Synth(
                mode.BlockFlag,
                _dsp.LappingState,
                _dsp.Windows,
                imdct,
                _buffer.ChanMut(mapped)
            );
        }
        
        // Trim
        _buffer.Trim(packet.TrimStart, packet.TrimEnd);

        // Save the new lapping state.
        _dsp.LappingState = new LappingState
        {
            PrevBlockFlag = mode.BlockFlag
        };
        return Unit.Default;
    }
    
    private static int MapVorbisChannel(int numChannels, int ch)
    {
        // This pre-condition should always be true.
        Debug.Assert(ch < numChannels);

        int mappedCh;
        switch (numChannels)
        {
            case 1:
                mappedCh = new int[] { 0 }[ch]; // FL
                break;
            case 2:
                mappedCh = new int[] { 0, 1 }[ch]; // FL, FR
                break;
            case 3:
                mappedCh = new int[] { 0, 2, 1 }[ch]; // FL, FC, FR
                break;
            case 4:
                mappedCh = new int[] { 0, 1, 2, 3 }[ch]; // FL, FR, RL, RR
                break;
            case 5:
                mappedCh = new int[] { 0, 2, 1, 3, 4 }[ch]; // FL, FC, FR, RL, RR
                break;
            case 6:
                mappedCh = new int[] { 0, 2, 1, 4, 5, 3 }[ch]; // FL, FC, FR, RL, RR, LFE
                break;
            case 7:
                mappedCh = new int[] { 0, 2, 1, 5, 6, 4, 3 }[ch]; // FL, FC, FR, SL, SR, RC, LFE
                break;
            case 8:
                mappedCh = new int[] { 0, 2, 1, 6, 7, 4, 5, 3 }[ch]; // FL, FC, FR, SL, SR, RL, RR, LFE
                break;
            default:
                return ch;
        }

        return mappedCh;
    }

}

public class AudioBufferRef
{
}