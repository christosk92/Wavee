using System.Collections;
using LanguageExt.Common;
using Wavee.Vorbis.Infrastructure;

namespace Wavee.Vorbis.Decoder.Setup.Codebooks;

internal sealed class CodebookBuilder
{
    private byte _maxBitsPerBlock;
    private readonly BitOrder _bitOrder;
    private readonly bool _isSparse;

    public CodebookBuilder(byte maxBitsPerBlock, BitOrder bitOrder, bool isSparse)
    {
        _maxBitsPerBlock = maxBitsPerBlock;
        _bitOrder = bitOrder;
        _isSparse = isSparse;
    }

    /// <summary>
    /// Instantiates a new `CodebookBuilder` for sparse codebooks.
    ///
    /// A sparse codebook is one in which not all codewords are valid. These invalid codewords
    /// are effectively "unused" and have no value. Therefore, it is illegal for a bitstream to
    /// contain the codeword bit pattern.
    ///
    /// Unused codewords are marked by having a length of 0.
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    public static CodebookBuilder NewSparse(BitOrder order)
    {
        return new CodebookBuilder(
            maxBitsPerBlock: 4,
            bitOrder: order,
            isSparse: true
        );
    }

    /// <summary>
    /// Specify the maximum number of bits that should be consumed from the source at a time.
    /// This value must be within the range 1 <= `max_bits_per_read` <= 16. Values outside of
    /// this range will cause this function to panic. If not provided, a value will be
    /// automatically chosen.
    /// </summary>
    /// <param name="i"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void BitsPerRead(byte maxBitsPerRead)
    {
        if (maxBitsPerRead > 16)
        {
            throw new NotImplementedException();
        }

        if (maxBitsPerRead <= 0)
        {
            throw new NotImplementedException();
        }

        _maxBitsPerBlock = maxBitsPerRead;
    }

    /// <summary>
    /// Construct a `Codebook` using the given codewords, their respective lengths, and values.
    ///
    /// This function may fail if the provided codewords do not form a complete VLC tree, or if
    /// the `CodebookEntry` is undersized.
    ///
    /// This function will panic if the number of code words, code lengths, and values differ.
    /// </summary>
    /// <param name="codeWords"></param>
    /// <param name="codeLens"></param>
    /// <param name="values"></param>
    /// <typeparam name="TE"></typeparam>
    /// <typeparam name="TEValueType"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Result<Codebook<TE, TEValueType>> Make<TE, TEValueType>(uint[] codeWords, byte[] codeLens,
        TEValueType[] values) where TE : struct, ICodebookEntry<TEValueType> where TEValueType : unmanaged
    {
        if (codeWords.Length != codeLens.Length)
        {
            return new Result<Codebook<TE, TEValueType>>(new DecodeError("codeWords.Length != codeLens.Length"));
        }

        if (codeWords.Length != values.Length)
        {
            return new Result<Codebook<TE, TEValueType>>(new DecodeError("codeWords.Length != values.Length"));
        }

        var blocks = new List<CodebookBlock<TEValueType>>();
        byte maxCodeLen = 0;

        // Only attempt to generate something if there are code words.
        if (codeWords.Length > 0)
        {
            var prefixMask = (uint)~(~0 << _maxBitsPerBlock);

            // Push a root block.
            blocks.Add(new CodebookBlock<TEValueType>(
                0,
                new SortedDictionary<ushort, int>(),
                new List<CodebookValue<TEValueType>>()
            ));

            // Populate the tree
            //                for ((&code, &code_len), &value) in code_words.iter().zip(code_lens).zip(values) {
            foreach (var ((code, codeLen), value) in codeWords.Zip(codeLens).Zip(values))
            {
                var parentBlockId = 0;
                var len = codeLen;

                // A zero length codeword in a spare codebook is allowed, but not in a regular
                // codebook.
                if (codeLen == 0)
                {
                    if (this._isSparse)
                    {
                        continue;
                    }
                    else
                    {
                        return new Result<Codebook<TE, TEValueType>>(
                            new CodebookError("core (io): zero length codeword"));
                    }
                }

                while (len > this._maxBitsPerBlock)
                {
                    len -= _maxBitsPerBlock;

                    var prefix = (ushort)((code >> len) & prefixMask);

                    // Recurse down the tree.
                    if (blocks[parentBlockId].Nodes.TryGetValue(prefix, out var pBlockId))
                    {
                        parentBlockId = pBlockId;
                    }
                    else
                    {
                        // Add a child block to the parent block.
                        var blockId = blocks.Count;
                        var block = blocks[parentBlockId];
                        block.Nodes.Add(prefix, blockId);

                        // The parent's block width must accomodate the prefix of the child.
                        // This is always max_bits_per_block bits.
                        block.Width = _maxBitsPerBlock;

                        // Append the new block.
                        blocks.Add(new CodebookBlock<TEValueType>(
                            0,
                            new SortedDictionary<ushort, int>(),
                            new List<CodebookValue<TEValueType>>()
                        ));

                        parentBlockId = blockId;
                    }
                }

                // The final chunk of code bits always has <= max_bits_per_block bits. Obtain
                // the final prefix.
                var finalPrefix = (ushort)(code & (prefixMask >> (_maxBitsPerBlock - len)));

                var finalBlock = blocks[parentBlockId];

                // Push the value.

                finalBlock.Values.Add(new CodebookValue<TEValueType>(
                    Prefix: finalPrefix,
                    Width: len,
                    Value: value
                ));

                // Update the block's width.

                finalBlock.Width = Math.Max(finalBlock.Width, len);

                // Update maximum observed codeword.
                maxCodeLen = Math.Max(maxCodeLen, codeLen);
            }
        }

        // Generate the codebook lookup table.
        var tableResult = GenerateLut<TE, TEValueType>(this._bitOrder, this._isSparse, blocks);
        if (tableResult.IsFaulted)
        {
            return new Result<Codebook<TE, TEValueType>>(tableResult.Error());
        }
        var table = tableResult.Success();
        // Determine the first block length if skipping the initial jump entry.
        var firstBlockLen = table.Length > 0 ? table[0].JumpLength : 0;

        return new Codebook<TE, TEValueType>(
            Table: table,
            MaxCodeLen: (uint)maxCodeLen,
            InitBlockLen: firstBlockLen
        );
    }

    private static Result<TE[]> GenerateLut<TE, TEValueType>(BitOrder bitOrder, bool isSparse,
        List<CodebookBlock<TEValueType>> blocks) where TE : ICodebookEntry<TEValueType> where TEValueType : unmanaged
    {
        // The codebook table.
        var table = new List<TE>();

        var queue = new LinkedList<int>();

        // The computed end of the table given the blocks in the queue.
        uint tableEnd = 0;

        if (blocks.Count > 0)
        {
            // Start traversal at the first block.
            queue.AddFirst(0);

            // The first entry in the table is always a jump to the first block.
            var block = blocks[0];
            var empty = default(TE);
            var newJump = (TE)empty.new_jump(1, block.Width);
            table.Add(newJump);
            tableEnd += 1 + (uint)(1 << block.Width);
        }

        // Traverse the tree in breadth-first order.
        while (queue.Count > 0)
        {
            // Count of the total number of entries added to the table by this block.
            var entryCount = 0;

            // Get the block id at the front of the queue.
            var blockId = queue.First.Value;
            queue.RemoveFirst();

            // Get the block at the front of the queue.
            var block = blocks[blockId];
            var blockLen = 1 << block.Width;

            // The starting index of the current block.
            var tableBase = table.Count;

            // Resize the table to accomodate all entries within the block.
            table.AddRange(Enumerable.Repeat(default(TE), (int)tableBase + blockLen - table.Count));
            // Push child blocks onto the queue and record the jump entries in the table. Jumps
            // will be in order of increasing prefix because of the implicit sorting provided
            // by BTreeMap, thus traversing a level of the tree left-to-right.
            foreach (var (prefix, childBlockId) in block.Nodes)
            {
                queue.AddLast(childBlockId);

                // The width of the child block in bits.
                var childBlockWidth = blocks[childBlockId].Width;

                var jumpOffsetMax = default(TE).JumpOffsetMax;
                // Verify the jump offset does not exceed the entry's jump maximum.
                if (tableEnd > jumpOffsetMax)
                {
                    return new Result<TE[]>(new DecodeError("core (io): codebook overflow"));
                }

                // Determine the offset into the table depending on the bit-order.
                var offset = (int)(bitOrder switch
                {
                    BitOrder.Verbatim => prefix,
                    BitOrder.Reverse => prefix.ReverseBits().RotateLeft((int)block.Width)
                });

                // Add a jump entry to table.
                var jumpEntry = (TE)default(TE).new_jump(offset: tableEnd, length: childBlockWidth);

                table[tableBase + offset] = jumpEntry;

                // Add the length of the child block to the end of the table.
                tableEnd += (uint)(1 << childBlockWidth);

                // Update the entry count.
                entryCount += 1;
            }

            // Add value entries into the table. If a value has a prefix width less than the
            // block width, then do-not-care bits must added to the end of the prefix to pad it
            // to the block width
            foreach (var value in block.Values)
            {
                // The number of do-not-care bits to add to the value's prefix.
                var numDncBits = block.Width - value.Width;

                // Extend the value's prefix to the block's width.
                var basePrefix = value.Prefix << (int)numDncBits;

                // Using the base prefix, synthesize all prefixes for this value.
                var count = 1 << (int)numDncBits;

                // The value entry that will be duplicated.
                var valueEntry = (TE)default(TE).new_value(value.Value, value.Width);

                switch (bitOrder)
                {
                    case BitOrder.Verbatim:
                        throw new NotImplementedException();
                        break;
                    case BitOrder.Reverse:
                    {
                        // For reverse bit order, the do-not-care bits are in the MSb position.
                        var start = (uint)basePrefix;
                        var end = start + count;

                        for (uint prefix = start; prefix < end; prefix++)
                        {
                            var offset = prefix.ReverseBits().RotateLeft((int)block.Width);
                            table[(int)offset + tableBase] = valueEntry;
                        }
                        break;
                    }
                }
                
                // Update the entry count.
                entryCount += count;
            }
            
            // If the decoding tree is not sparse, the number of entries added to the table
            // should equal the block length if the. It is a fatal error if this is not true
            if (!isSparse && entryCount != blockLen)
            {
                return new Result<TE[]>(new DecodeError("core (io): codebook is incomplete"));
            }
        }
        
        return new Result<TE[]>(table.ToArray());
    }
}

internal enum BitOrder
{
    /// <summary>
    /// The provided codewords have bits in the same order as the order in which they're being
    /// read.
    /// </summary>
    Verbatim,

    /// <summary>
    /// The provided codeword have bits in the reverse order as the order in which they're
    /// being read.
    /// </summary>
    Reverse,
}