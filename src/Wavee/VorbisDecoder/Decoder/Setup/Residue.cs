using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Decoder.Setup.Codebooks;
using Wavee.Vorbis.Infrastructure;
using Wavee.Vorbis.Infrastructure.BitReaders;

namespace Wavee.Vorbis.Decoder.Setup;

internal sealed class Residue
{
    private ResidueSetup _setup;
    private byte[] _partClasses;
    private float[] _type2Buffer;

    private Residue(ResidueSetup setup, byte[] partClasses, float[] type2Buffer)
    {
        _setup = setup;
        _partClasses = partClasses;
        _type2Buffer = type2Buffer;
    }

    public static Result<Residue> Read(ref BitReaderRtlRef bs, ushort residueType, byte maxcodebook)
    {
        var setupRes = ReadSetup(ref bs, residueType, maxcodebook);
        if (setupRes.IsFaulted)
            return new Result<Residue>(setupRes.Error());

        return new Result<Residue>(new Residue(
            setup: setupRes.Success(),
            partClasses: Array.Empty<byte>(),
            type2Buffer: Array.Empty<float>()
        ));
    }

    private static Result<ResidueSetup> ReadSetup(ref BitReaderRtlRef bs, ushort residueType, byte maxcodebook)
    {
        var residueBeginRes = bs.ReadBitsLeq32(24);
        if (residueBeginRes.IsFaulted)
            return new Result<ResidueSetup>(residueBeginRes.Error());
        var residueEndRes = bs.ReadBitsLeq32(24);
        if (residueEndRes.IsFaulted)
            return new Result<ResidueSetup>(residueEndRes.Error());
        var residuePartitionSizeRes = bs.ReadBitsLeq32(24).Map(x => x + 1);
        if (residuePartitionSizeRes.IsFaulted)
            return new Result<ResidueSetup>(residuePartitionSizeRes.Error());
        var residueClassificationsRes = bs.ReadBitsLeq32(6).Map(x => (byte)(x + 1));
        if (residueClassificationsRes.IsFaulted)
            return new Result<ResidueSetup>(residueClassificationsRes.Error());
        var residueClassbookRes = bs.ReadBitsLeq32(8).Map(x => (byte)x);
        if (residueClassbookRes.IsFaulted)
            return new Result<ResidueSetup>(residueClassbookRes.Error());

        var residueBegin = (uint)residueBeginRes.Success();
        var residueEnd = (uint)residueEndRes.Success();
        var residuePartitionSize = (uint)residuePartitionSizeRes.Success();
        var residueClassifications = residueClassificationsRes.Success();
        var residueClassbook = residueClassbookRes.Success();

        if (residueEnd < residueBegin)
            return new Result<ResidueSetup>(new DecodeError("vorbis: invalid residue begin and end"));

        var residueVqBooks = new List<ResidueVqClass>();

        for (int _ = 0; _ < residueClassifications; _++)
        {
            var lowBitsRes = bs.ReadBitsLeq32(3).Map(x => (byte)x);
            if (lowBitsRes.IsFaulted)
                return new Result<ResidueSetup>(lowBitsRes.Error());
            var lowBits = lowBitsRes.Success();

            var hasHighBits = bs.ReadBool();
            byte highBits = 0;
            if (hasHighBits)
            {
                var highBitsRes = bs.ReadBitsLeq32(5).Map(x => (byte)x);
                if (highBitsRes.IsFaulted)
                    return new Result<ResidueSetup>(highBitsRes.Error());
                highBits = highBitsRes.Success();
            }

            var isUsed = (byte)((highBits << 3) | lowBits);
            var emptybooks = new byte[8];
            Array.Fill(emptybooks, (byte)0);
            residueVqBooks.Add(new ResidueVqClass(
                Books: emptybooks,
                IsUsed: isUsed
            ));
        }

        var residueMaxPass = 0;
        foreach (var vqBooks in residueVqBooks)
        {
            // For each set of residue codebooks, if the codebook is used, read the codebook
            // number.
            for (int j = 0; j < vqBooks.Books.Length(); j++)
            {
                var book = vqBooks.Books[j];
                // Is a codebook used?
                var isCodebookUsed = (vqBooks.IsUsed & (1 << j)) != 0;

                if (isCodebookUsed)
                {
                    // Read the codebook number.
                    var bookRes = bs.ReadBitsLeq32(8).Map(x => (byte)x);
                    if (bookRes.IsFaulted)
                        return new Result<ResidueSetup>(bookRes.Error());
                    book = bookRes.Success();
                    vqBooks.Books[j] = book;

                    // The codebook number cannot be 0 or exceed the number of codebooks in this
                    // stream.
                    if (book == 0 || book >= maxcodebook)
                        return new Result<ResidueSetup>(new DecodeError("vorbis: invalid codebook for residue"));

                    residueMaxPass = Math.Max(residueMaxPass, j);
                }
            }
        }

        var residue = new ResidueSetup(
            ResidueType: residueType,
            ResidueBegin: residueBegin,
            ResidueEnd: residueEnd,
            ResiduePartitionSize: residuePartitionSize,
            ResidueClassifications: residueClassifications,
            ResidueClassbook: residueClassbook,
            ResidueVqClass: residueVqBooks.ToArray(),
            ResidueMaxPass: residueMaxPass
        );

        return new Result<ResidueSetup>(residue);
    }

    private record ResidueSetup(ushort ResidueType, uint ResidueBegin, uint ResidueEnd, uint ResiduePartitionSize,
        byte ResidueClassifications, byte ResidueClassbook, ResidueVqClass[] ResidueVqClass, int ResidueMaxPass);

    private record ResidueVqClass(byte[] Books, byte IsUsed)
    {
        public bool IsUsedForPass(int pass)
        {
            return (IsUsed & (1 << pass)) != 0;
        }
    }

    public Result<Unit> ReadResidue(ref BitReaderRtlRef bs, byte bsExp, VorbisCodebook[] codebooks,
        BitSet256 residueChannels, DspChannel[] dspChannels)
    {
        Result<Unit> result = new Result<Unit>();
        if (_setup.ResidueType == 2)
        {
            result = ReadResidueInnerType2(ref bs, bsExp, codebooks, residueChannels, dspChannels);
        }
        else
        {
            result = ReadResidueInnerType01(ref bs, bsExp, codebooks, residueChannels, dspChannels);
        }

        // Read the residue, and ignore end-of-bitstream errors which are legal
        var isOk = result.IsSuccess || result.Error() is EndOfStreamException;
        if (!isOk)
            return new Result<Unit>(result.Error());

        // For format 2, the residue vectors for all channels are interleaved together into one
        // large vector. This vector is in the scratch-pad buffer and can now be de-interleaved
        // into the channel buffers.
        // For format 2, the residue vectors for all channels are interleaved together into one
        // large vector. This vector is in the scratch-pad buffer and can now be de-interleaved
        // into the channel buffers.
        if (_setup.ResidueType == 2)
            Deinterleave2(residueChannels, dspChannels);

        return new Result<Unit>(Unit.Default);
    }

    private void Deinterleave2(BitSet256 residueChannels, DspChannel[] channels)
    {
        var count = residueChannels.Count();
        switch (count)
        {
            case 2:
            {
                // Two channel deinterleave.
                // Two channel deinterleave.
                int ch0, ch1;
                (ch0, ch1) = GetFirstTwoChannelIndices(residueChannels);

                DspChannel channel0 = channels[ch0];
                DspChannel channel1 = channels[ch1];

                // Deinterleave.
                for (int i = 0; i < _type2Buffer.Length; i += 2)
                {
                    channel0.Residue[i / 2] = _type2Buffer[i];
                    channel1.Residue[i / 2] = _type2Buffer[i + 1];
                }

                break;
            }
        }
    }

    // Helper function to get the indices of the first two channels in the residue.
    private (int, int) GetFirstTwoChannelIndices(BitSet256 residueChannels)
    {
        int? ch0 = null, ch1 = null;

        foreach (int ch in residueChannels)
        {
            if (ch0 == null)
            {
                ch0 = ch;
            }
            else if (ch1 == null)
            {
                ch1 = ch;
                break;
            }
        }

        if (ch0.HasValue && ch1.HasValue)
        {
            return (ch0.Value, ch1.Value);
        }

        throw new InvalidOperationException("Not enough channel indices in the residue.");
    }

    private Result<Unit> ReadResidueInnerType01(ref BitReaderRtlRef bs,
        byte bsExp,
        VorbisCodebook[] codebooks,
        BitSet256 residueChannels,
        DspChannel[] channels)
    {
        var classbook = codebooks[_setup.ResidueClassbook];

        // The actual length of the entire residue vector for a channel (formats 0 and 1), or all
        // interleaved channels (format 2).
        var fullResidueLength = ((1 << bsExp) >> 1);

        // The range of the residue vector being decoded.
        var limitResidueBegin = Math.Min((int)_setup.ResidueBegin, fullResidueLength);
        var limitResidueEnd = Math.Min((int)_setup.ResidueEnd, fullResidueLength);

        // Length of the decoded part of the residue vector.
        var residueLen = limitResidueEnd - limitResidueBegin;

        // The number of partitions in the residue vector.
        var partsPerClassword = classbook.Dimensions;

        //Number of partitions to read
        var partsToRead = residueLen / (int)_setup.ResiduePartitionSize;

        // Reserve partition classification space.
        this.PreparePartitionClassifications((int)partsToRead * residueChannels.Count());

        var hasChannelToDecode = false;
        foreach (var ch in residueChannels)
        {
            // Record if the channel needs to be decoded.
            hasChannelToDecode |= !channels[ch].DoNotDecode;

            // Zero the channel residue buffer.
            var span = channels[ch].Residue.AsSpan()[..fullResidueLength];
            span.Clear();
        }

        // If all channels are marked do-not-decode then exit immediately.
        if (!hasChannelToDecode)
            return new Result<Unit>(Unit.Default);

        var partSize = _setup.ResiduePartitionSize;

        // Residues may be encoded in up-to 8 passes. Fewer passes may be encoded by prematurely
        // "ending" the packet. This means that an end-of-bitstream error is actually NOT an error
        for (int pass = 0; pass < (_setup.ResidueMaxPass + 1); pass++)
        {
            // Iterate over the partitions in batches grouped by classword.
            //            for part_first in (0..parts_to_read).step_by(parts_per_classword as usize) {
            for (int partFirst = 0; partFirst < partsToRead; partFirst += partsPerClassword)
            {
                // The class assignments for each partition in the classword group are only
                // encoded in the first pass.
                if (pass == 0)
                {
                    for (int i = 0; i < residueChannels.Count(); i++)
                    {
                        var ch = residueChannels.ElementAt(i);
                        var channel = channels[ch];

                        // If the channel is marked do-not-decode then advance to the next
                        // channel.
                        if (channel.DoNotDecode)
                            continue;

                        var codeRes = classbook.ReadScalar(ref bs);
                        if (codeRes.IsFaulted)
                            return new Result<Unit>(codeRes.Error());
                        var code = codeRes.Success();

                        DecodeClasses(
                            val: code,
                            partsPerClassword,
                            this._setup.ResidueClassifications,
                            this._partClasses.AsSpan(partFirst + i * partsToRead)
                        );
                    }
                }

                // The last partition in this batch of partitions, being careful not to exceed the
                // total number of partitions.
                var partLast = Math.Min(partsToRead, partFirst + partsPerClassword);

                // Iterate over all partitions belonging to the current classword group.
                for (int part = partFirst; part < partLast; part++)
                {
                    // Iterate over each channel vector in the partition.
                    for (int i = 0; i < residueChannels.Count(); i++)
                    {
                        var ch = residueChannels.ElementAt(i);
                        var channel = channels[ch];

                        // If the channel is marked do-not-decode, then advance to the next channel.
                        if (channel.DoNotDecode)
                            continue;

                        var classIdx = (int)_partClasses[part + i * partsToRead];
                        var vqClass = _setup.ResidueVqClass[classIdx];

                        if (vqClass.IsUsedForPass(pass))
                        {
                            var vqbook = codebooks[vqClass.Books[pass]];

                            var partStart = limitResidueBegin + part * partSize;

                            switch (_setup.ResidueType)
                            {
                                case 0:
                                    ReadResiduePartitionFormat0(
                                        ref bs,
                                        vqbook,
                                        channel.Residue.AsSpan((int)partStart..(int)(partSize + partStart))
                                    );
                                    break;
                                case 1:
                                    ReadResiduePartitionFormat1(
                                        ref bs,
                                        vqbook,
                                        channel.Residue.AsSpan((int)partStart..(int)(partSize + partStart))
                                    );
                                    break;
                            }
                        }
                    }
                }
                // End of partition batch iteration.
            }
            // End of pass iteration.
        }

        return new Result<Unit>(Unit.Default);
    }


    private Result<Unit> ReadResidueInnerType2(ref BitReaderRtlRef bs,
        byte bsExp,
        VorbisCodebook[] codebooks,
        BitSet256 residueChannels, DspChannel[] channels)
    {
        var classbook = codebooks[_setup.ResidueClassbook];

        // The actual length of the entire residue vector for a channel (formats 0 and 1), or all
        // interleaved channels (format 2).
        var fullResidueLength = ((1 << bsExp) >> 1) * residueChannels.Count();

        // The range of the residue vector being decoded.
        var limitResidueBegin = Math.Min((int)_setup.ResidueBegin, fullResidueLength);
        var limitResidueEnd = Math.Min((int)_setup.ResidueEnd, fullResidueLength);

        // Length of the decoded part of the residue vector.
        var residueLen = limitResidueEnd - limitResidueBegin;

        // The number of partitions in the residue vector.
        var partsPerClassword = classbook.Dimensions;

        //Number of partitions to read
        var partsToRead = residueLen / _setup.ResiduePartitionSize;

        // Reserve partition classification space
        PreparePartitionClassifications((int)partsToRead);

        // reserve type 2 interleave buffer storage and zero all samples
        PrepareType2FormatBuffer(fullResidueLength);

        // If all channels are marked do-not-decode then exit immediately.
        var hasChannelToDecode
            = residueChannels.Select((_, i) => i).Any(c => !channels[c].DoNotDecode);
        if (!hasChannelToDecode)
            return new Result<Unit>(Unit.Default);

        var partSize = _setup.ResiduePartitionSize;

        // Residues may be encoded in up-to 8 passes. Fewer passes may be encoded by prematurely
        // "ending" the packet. This means that an end-of-bitstream error is actually NOT an error.
        for (int pass = 0; pass < (_setup.ResidueMaxPass + 1); pass++)
        {
            // Iterate over the partitions in batches grouped by classword.
            for (int partFirst = 0; partFirst < partsToRead; partFirst += (int)partsPerClassword)
            {
                // The class assignments for each partition in the classword group are only
                // encoded in the first pass.
                if (pass == 0)
                {
                    var codeResult = classbook.ReadScalar(ref bs);
                    if (codeResult.IsFaulted)
                        return new Result<Unit>(codeResult.Error());
                    var code = codeResult.Success();

                    DecodeClasses(
                        code,
                        partsPerClassword,
                        _setup.ResidueClassifications,
                        _partClasses.AsSpan(partFirst)
                    );
                }

                // The class assignments for each partition in the classword group are only
                // encoded in the first pass.
                var partLast = Math.Min(partsToRead, partFirst + partsPerClassword);

                // Iterate over all partitions belonging to the current classword group.
                for (int part = partFirst; part < partLast; part++)
                {
                    var vqClass = _setup.ResidueVqClass[_partClasses[part]];

                    if (vqClass.IsUsedForPass(pass))
                    {
                        var vqbook = codebooks[vqClass.Books[pass]];
                        var partStart = limitResidueBegin + part * partSize;

                        // Residue type 2 is implemented in term of type 1.
                        ReadResiduePartitionFormat1(
                            ref bs,
                            vqbook,
                            _type2Buffer.AsSpan((int)partStart..(int)(partSize + partStart))
                        );
                    }
                }
            }
            // End of partition batch iteration.
        }
        // End of pass iteration.

        return new Result<Unit>(Unit.Default);
    }

    private void PreparePartitionClassifications(int len)
    {
        if (_partClasses.Length < len)
        {
            var oldLen = _partClasses.Length;
            Array.Resize(ref _partClasses, len);
            for (int i = oldLen; i < len; i++)
            {
                _partClasses[i] = 0;
            }
        }
    }

    private Result<Unit> ReadResiduePartitionFormat0(ref BitReaderRtlRef bs, VorbisCodebook vqbook, Span<float> output)
    {
        var step = output.Length / vqbook.Dimensions;

        for (int i = 0; i < step; i++)
        {
            var vqRes = vqbook.ReadVector(ref bs);
            if (vqRes.IsFaulted)
                return new Result<Unit>(vqRes.Error());
            var vq = vqRes.Success();
            for (int j = i; j < output.Length; j += step)
            {
                output[j] += vq[j / step];
            }
        }
        
        return new Result<Unit>(Unit.Default);
    }

    private static Result<Unit> ReadResiduePartitionFormat1(ref BitReaderRtlRef bs, VorbisCodebook vqbook,
        Span<float> output)
    {
        var dim = vqbook.Dimensions;

        // For small dimensions it is too expensive to use iterator loops. Special case small sizes
        // to improve performance.
        switch (dim)
        {
            case 2:
            {
                for (int i = 0; i < output.Length; i += 2)
                {
                    var vqRes = vqbook.ReadVector(ref bs);
                    if (vqRes.IsFaulted)
                        return new Result<Unit>(vqRes.Error());
                    var vq = vqRes.Success();
                    // Amortize the cost of the bounds check.
                    output[0 + i] += vq[0];
                    output[1 + i] += vq[1];
                }

                break;
            }
            case 4:
            {
                for (int i = 0; i < output.Length; i += 4)
                {
                    var vqRes = vqbook.ReadVector(ref bs);
                    if (vqRes.IsFaulted)
                        return new Result<Unit>(vqRes.Error());
                    var vq = vqRes.Success();

                    // Amortize the cost of the bounds check.
                    output[0 + i] += vq[0];
                    output[1 + i] += vq[1];
                    output[2 + i] += vq[2];
                    output[3 + i] += vq[3];
                }

                break;
            }
            default:
            {
                for (int i = 0; i < output.Length; i += dim)
                {
                    var vqRes = vqbook.ReadVector(ref bs);
                    if (vqRes.IsFaulted)
                        return new Result<Unit>(vqRes.Error());
                    var vq = vqRes.Success();

                    // Ensure that the chunk size is correct
                    if (vq.Length != dim)
                    {
                        throw new InvalidOperationException("Invalid VQ length.");
                    }

                    for (int j = 0; j < dim && i + j < output.Length; j++)
                    {
                        output[i + j] += vq[j];
                    }
                }

                break;
            }
        }

        return new Result<Unit>(Unit.Default);
    }

    private void DecodeClasses(uint val, uint partsPerClassword, ushort classifications, Span<byte> output)
    {
        //The number of partitions that need a class assignment
        var numParts = output.Length;

        // If the number of partitions per classword is greater than the number of partitions that need
        // a class assignment, then the excess classes must be dropped because class assignments are
        // assigned in reverse order.
        int skip = 0;
        if (partsPerClassword > numParts)
        {
            skip = (int)(partsPerClassword - numParts);

            for (int i = 0; i < skip; i++)
            {
                val /= classifications;
            }
        }

        for (int i = (int)(partsPerClassword - skip - 1); i >= 0; i--)
        {
            output[i] = (byte)(val % classifications);
            val /= classifications;
        }
    }

    private void PrepareType2FormatBuffer(int len)
    {
        if (_type2Buffer.Length < len)
        {
            // for (int i = _type2Buf.Length; i < len; i++)
            // {
            //     _type2Buf.Add(0);
            // }
            var oldLen = _type2Buffer.Length;
            Array.Resize(ref _type2Buffer, len);
            for (int i = oldLen; i < len; i++)
            {
                _type2Buffer[i] = 0;
            }
        }

        for (int i = 0; i < len; i++)
        {
            _type2Buffer[i] = 0;
        }
    }
}