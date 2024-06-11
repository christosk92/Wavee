using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;
using Wavee.Core.Decoders.VorbisDecoder.Mapper;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;

internal class VorbisCodebook
{
    private VorbisCodebook(Codebook<Entry32x32, uint> codebook, ushort dimensions, Option<float[]> vqVec)
    {
        Codebook = codebook;
        Dimensions = dimensions;
        VqVec = vqVec;
    }

    public Codebook<Entry32x32, uint> Codebook { get; }
    public ushort Dimensions { get; }
    public Option<float[]> VqVec { get; }

    public static Result<VorbisCodebook> ReadCodebook(ref BitReaderRtlRef bs)
    {
        // Verify codebook synchronization word.
        var syncResult = bs.ReadBitsLeq32(24);
        if (syncResult.IsFaulted)
            return new Result<VorbisCodebook>(syncResult.Error());
        var sync = syncResult.Success();

        if (sync != 0x564342)
            return new Result<VorbisCodebook>(new DecodeError("vorbis: invalid codebook sync word"));

        // Read codebook number of dimensions and entries.
        var codebookDimensionsResult = bs.ReadBitsLeq32(16).Map(x => (ushort)x);
        var codebookEntriesResult = bs.ReadBitsLeq32(24).Map(x => (uint)x);
        if (codebookDimensionsResult.IsFaulted || codebookEntriesResult.IsFaulted)
            return new Result<VorbisCodebook>(codebookDimensionsResult.IsFaulted
                ? codebookDimensionsResult.Error()
                : codebookEntriesResult.Error());

        var codebookDimensions = codebookDimensionsResult.Success();
        var codebookEntries = codebookEntriesResult.Success();

        // Ordered flag.
        var isLengthOrdered = bs.ReadBool();

        var codeLens = new List<byte>((int)codebookEntries);

        if (!isLengthOrdered)
        {
            // Codeword list is not length ordered.
            var isSparse = bs.ReadBool();

            if (isSparse)
            {
                for (var i = 0; i < codebookEntries; i++)
                {
                    var isUsed = bs.ReadBool();
                    byte codeLen = 0;
                    if (isUsed)
                    {
                        var result = bs.ReadBitsLeq32(5).Map(x => (byte)(x + 1));
                        if (result.IsFaulted)
                            return new Result<VorbisCodebook>(result.Error());
                        codeLen = result.Success();
                    }

                    codeLens.Add(codeLen);
                }
            }
            else
            {
                // Densely packed codeword entry list.
                for (var i = 0; i < codebookEntries; i++)
                {
                    var result = bs.ReadBitsLeq32(5).Map(x => (byte)(x + 1));
                    if (result.IsFaulted)
                        return new Result<VorbisCodebook>(result.Error());
                    var codeLen = result.Success();
                    codeLens.Add(codeLen);
                }
            }
        }
        else
        {
            // Codeword list is length ordered.
            uint curEntry = 0;
            var curLenResult = bs.ReadBitsLeq32(5).Map(x => (uint)(x + 1));
            if (curLenResult.IsFaulted)
                return new Result<VorbisCodebook>(curLenResult.Error());
            var curLen = curLenResult.Success();

            while (true)
            {
                uint numBits = 0;
                if (codebookEntries > curEntry)
                {
                    numBits = (codebookEntries - curEntry).ilog();
                }

                var numResult = bs.ReadBitsLeq32(numBits);
                if (numResult.IsFaulted)
                    return new Result<VorbisCodebook>(numResult.Error());
                var num = numResult.Success();
                //                code_lens.extend(std::iter::repeat(cur_len as u8).take(num as usize));
                codeLens.AddRange(Enumerable.Repeat((byte)curLen, num));
                curLen += 1;
                curEntry += (uint)num;

                if (curEntry > codebookEntries)
                {
                    return new Result<VorbisCodebook>(new DecodeError("vorbis: invalid codebook entry count"));
                }

                if (curEntry == codebookEntries)
                    break;
            }
        }

        // Read and unpack vector quantization (VQ) lookup table.
        var lookupTypeResult = bs.ReadBitsLeq32(4);
        if (lookupTypeResult.IsFaulted)
            return new Result<VorbisCodebook>(lookupTypeResult.Error());
        var lookupType = lookupTypeResult.Success();

        Option<float[]> vcVec = Option<float[]>.None;
        switch ((lookupType & 0xf))
        {
            case 0:
                vcVec = Option<float[]>.None;
                break;
            case 1 or 2:
            {
                var minValueArgumentResult = bs.ReadBitsLeq32(32);
                var deltaValueArgumentResult = bs.ReadBitsLeq32(32);
                var valueBitsArgumentResult = bs.ReadBitsLeq32(4).Map(x => (byte)(x + 1));
                if (minValueArgumentResult.IsFaulted || deltaValueArgumentResult.IsFaulted ||
                    valueBitsArgumentResult.IsFaulted)
                    return new Result<VorbisCodebook>(minValueArgumentResult.IsFaulted
                        ? minValueArgumentResult.Error()
                        : deltaValueArgumentResult.IsFaulted
                            ? deltaValueArgumentResult.Error()
                            : valueBitsArgumentResult.Error());

                var minValue = float32_unpack((uint)minValueArgumentResult.Success());
                var deltaValue = float32_unpack((uint)deltaValueArgumentResult.Success());
                var valueBits = valueBitsArgumentResult.Success();
                var sequenceP = bs.ReadBool();

                // Lookup type is either 1 or 2 as per outer match.
                var lookupValues = lookupType switch
                {
                    1 => VorbisMapper.lookup1_values(codebookEntries, codebookDimensions),
                    2 => codebookEntries * (uint)(codebookDimensions),
                    _ => throw new InvalidOperationException("Invalid lookup type")
                };

                Span<ushort> multiplicands = new ushort[lookupValues];
                for (int i = 0; i < lookupValues; i++)
                {
                    var result = bs.ReadBitsLeq32(valueBits).Map(x => (ushort)x);
                    if (result.IsFaulted)
                        return new Result<VorbisCodebook>(result.Error());
                    multiplicands[i] = result.Success();
                }

                vcVec = lookupType switch
                {
                    1 => unpack_vq_lookup_type1(multiplicands, minValue, deltaValue, sequenceP, codebookEntries,
                        codebookDimensions, lookupValues),
                    2 => unpack_vq_lookup_type2(multiplicands, minValue, deltaValue, sequenceP, codebookEntries,
                        codebookDimensions)
                };
                break;
            }
            default:
                return new Result<VorbisCodebook>(new DecodeError("vorbis: invalid lookup type"));
        }

        var codeLensArr = codeLens.ToArray();
        // Generate a canonical list of codewords given the set of codeword lengths.
        var codeWordsResult = synthesize_codewords(codeLensArr);
        if (codeWordsResult.IsFaulted)
            return new Result<VorbisCodebook>(codeWordsResult.Error());
        var codeWords = codeWordsResult.Success();

        // Generate the values associated for each codeword.
        // TODO: Should unused entries be 0 or actually the correct value?
        var values = Enumerable.Range(0, (int)codebookEntries).Select(i => (uint)i).ToArray();

        // Finally, generate the codebook with a reverse (LSb) bit order.
        var builder = CodebookBuilder.NewSparse(BitOrder.Reverse);

        // Read in 8-bit blocks.
        builder.BitsPerRead(8);

        var codebookResult = builder.Make<Entry32x32, uint>(codeWords, codeLensArr, values);
        if (codebookResult.IsFaulted)
            return new Result<VorbisCodebook>(codebookResult.Error());

        var codebook = codebookResult.Success();
        return new VorbisCodebook(
            codebook,
            dimensions: codebookDimensions,
            vqVec: vcVec
        );
    }

    private static Result<uint[]> synthesize_codewords(byte[] codeLens)
    {
        // This codeword generation algorithm works by maintaining a table of the next valid codeword for
        // each codeword length.
        //
        // Consider a huffman tree. Each level of the tree correlates to a specific length of codeword.
        // For example, given a leaf node at level 2 of the huffman tree, that codeword would be 2 bits
        // long. Therefore, the table being maintained contains the codeword that would identify the next
        // available left-most node in the huffman tree at a given level. Therefore, this table can be
        // interrogated to get the next codeword in a simple lookup and the tree will fill-out in the
        // canonical order.
        //
        // Note however that, after selecting a codeword, C, of length N, all codewords of length > N
        // cannot use C as a prefix anymore. Therefore, all table entries for codeword lengths > N must
        // be updated such that these codewords are skipped over. Likewise, the table must be updated for
        // lengths < N to account for jumping between nodes.
        //
        // This algorithm is a modified version of the one found in the Vorbis reference implementation.
        List<uint> codewords = new List<uint>();
        uint[] nextCodeword = new uint[33];
        int numSparse = 0;
        foreach (byte len in codeLens)
        {
            if (len > 32)
                return new Result<uint[]>(new DecodeError("Invalid codeword length."));

            if (len == 0)
            {
                numSparse++;
                codewords.Add(0);
                continue;
            }

            int codewordLen = len;
            uint codeword = nextCodeword[codewordLen];

            if (len < 32 && (codeword >> len) > 0)
                return new Result<uint[]>(new DecodeError("Codebook overspecified"));

            for (int i = codewordLen; i >= 0; i--)
            {
                if ((nextCodeword[i] & 1) == 1)
                {
                    nextCodeword[i] = nextCodeword[i - 1] << 1;
                    break;
                }

                nextCodeword[i]++;
            }

            uint branch = nextCodeword[codewordLen];

            for (int i = 1; i + codewordLen < nextCodeword.Length; i++)
            {
                if (nextCodeword[i + codewordLen] == codeword << i)
                {
                    nextCodeword[i + codewordLen] = branch << i;
                }
                else
                {
                    break;
                }
            }

            codewords.Add(codeword);
        }

        bool isUnderspecified = false;
        for (int i = 1; i < nextCodeword.Length; i++)
        {
            if ((nextCodeword[i] & (UInt32.MaxValue >> (32 - i))) != 0)
            {
                isUnderspecified = true;
                break;
            }
        }

        bool isSingleEntryCodebook = codeLens.Length - numSparse == 1;

        if (isUnderspecified && !isSingleEntryCodebook)
            return new Result<uint[]>(new DecodeError("Codebook underspecified"));

        return codewords.ToArray();
    }

    private static float[] unpack_vq_lookup_type2(Span<ushort> multiplicands, float minValue, float deltaValue,
        bool sequenceP, uint codebookEntries, ushort codebookDimensions)
    {
        float[] vqLookup = new float[codebookEntries * codebookDimensions];
        for (int lookupOffset = 0; lookupOffset < codebookEntries; lookupOffset++)
        {
            float last = 0.0f;
            int multiplicandOffset = lookupOffset * codebookDimensions;

            for (int j = 0; j < codebookDimensions; j++)
            {
                int valueVectorIndex = lookupOffset * codebookDimensions + j;

                vqLookup[valueVectorIndex] = (float)multiplicands[multiplicandOffset] * deltaValue + minValue + last;

                if (sequenceP)
                {
                    last = vqLookup[valueVectorIndex];
                }

                multiplicandOffset++;
            }
        }

        return vqLookup;
    }

    /// <summary>
    /// As defined in section 3.2.1 of the Vorbis I specification.
    /// </summary>
    /// <param name="multiplicands"></param>
    /// <param name="minValue"></param>
    /// <param name="deltaValue"></param>
    /// <param name="sequenceP"></param>
    /// <param name="codebookEntries"></param>
    /// <param name="codebookDimensions"></param>
    /// <param name="lookupValues"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static float[] unpack_vq_lookup_type1(Span<ushort> multiplicands, float minValue, float deltaValue,
        bool sequenceP, uint codebookEntries, ushort codebookDimensions, uint lookupValues)
    {
        float[] vqLookup = new float[codebookEntries * codebookDimensions];
        for (int v = 0; v < codebookEntries; v++)
        {
            int lookupOffset = v;

            float last = 0.0f;
            uint indexDivisor = 1;

            for (int j = 0; j < codebookDimensions; j++)
            {
                int multiplicandOffset = (int)((lookupOffset / indexDivisor) % lookupValues);
                int valueVectorIndex = v * codebookDimensions + j;

                vqLookup[valueVectorIndex] = multiplicands[multiplicandOffset] * deltaValue + minValue + last;

                if (sequenceP)
                {
                    last = vqLookup[valueVectorIndex];
                }

                indexDivisor *= lookupValues;
            }
        }

        return vqLookup;
    }


    private static float float32_unpack(uint x)
    {
        uint mantissa = x & 0x1fffff;
        uint sign = x & 0x80000000;
        int exponent = (int)((x & 0x7fe00000) >> 21);
        float value = mantissa * (float)Math.Pow(2.0, exponent - 788);

        if (sign == 0)
        {
            return value;
        }
        else
        {
            return -value;
        }
    }


    public Result<uint> ReadScalar(ref BitReaderRtlRef bs)
    {
        // An entry in a scalar codebook is just the value.
        var result = bs.ReadCodebook(this.Codebook);
        if (result.IsFaulted)
            return new Result<uint>(result.Error());
        var entry = result.Success();
        return entry.Item1;
    }

    public Result<float[]> ReadVector(ref BitReaderRtlRef bs)
    {
        // An entry in a VQ codebook is the index of the VQ vector.
        var entryRes = bs.ReadCodebook(Codebook);
        if (entryRes.IsFaulted)
            return new Result<float[]>(entryRes.Error());
        var entry = entryRes.Success().Item1;
        if (VqVec.IsSome)
        {
            var dim = this.Dimensions;
            var start = entry * dim;

            var res = VqVec.ValueUnsafe()[(int)start..(int)(start + dim)];
            return res;
        }

        return new Result<float[]>(new DecodeError("vorbis: not a vq codebook"));
    }
}