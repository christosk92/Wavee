using Wavee.VorbisDecoder.Decoder;

namespace Wavee.Vorbis.Decoder;

internal sealed class DspChannel
{
    private Memory<float> _imdct;
    private Memory<float> _overlap;
    private int _bs0;
    private int _bs1;

    public DspChannel(byte bs0Exp, byte bs1Exp)
    {
        var bs0 = 1 << bs0Exp;
        var bs1 = 1 << bs1Exp;

        var l = bs1 >> 1;
        var floor = new float[l];
        var residue = new float[l];
        var imdct = new float[bs1];
        var overlap = new float[l];

        for (var i = 0; i < l; i++)
        {
            floor[i] = residue[i] = overlap[i] = 0;
        }

        for (var i = 0; i < bs1; i++)
        {
            imdct[i] = 0;
        }

        _bs0 = bs0;
        _bs1 = bs1;
        _imdct = imdct;
        _overlap = overlap;
        Floor = floor;
        Residue = residue;
        DoNotDecode = false;
    }

    public float[] Floor { get; }
    public float[] Residue { get; }
    public bool DoNotDecode { get; set; }

    internal void Synth(bool blockFlag,
        LappingState? lapState,
        Windows windows,
        Imdct imdct,
        Span<float> buf)
    {
        // Block size of the current block.
        var bs = blockFlag ? _bs1 : _bs0;

        // Perform the inverse MDCT on the audio spectrum.
        //        imdct.imdct(&self.floor[..bs / 2], &mut self.imdct[..bs]);
        // var span = Floor.AsSpan(0, bs);
        //we need to fill an array of bs length
        // var span = new float[bs];
        // Floor
        //     .AsSpan(0, Math.Min(bs, Floor.Length))
        //     .CopyTo(span);
        //
        // imdct.CalcReverse(span);
        // span.CopyTo(_imdct.Span);

        //        imdct.imdct(&self.floor[..bs / 2], &mut self.imdct[..bs]);
        var span = Floor[..(bs / 2)].ToArray();
        imdct.CalcReverse(span, _imdct[..bs].Span);

        // Overlap-add and windowing with the previous buffer.
        if (lapState is not null)
        {
            // Window for this block.

            ReadOnlySpan<float> win;
            if (blockFlag && lapState.PrevBlockFlag)
            {
                win = windows.Long;
            }
            else
            {
                win = windows.Short;
            }

            if (lapState.PrevBlockFlag == blockFlag)
            {
                // Both the previous and current blocks are either short or long. In this case,
                // there is a complete overlap between.
                OverlapAdd(buf,
                    _overlap[..(bs / 2)].Span,
                    _imdct[..(bs / 2)].Span,
                    win);
            }
            else if (lapState.PrevBlockFlag && !blockFlag)
            {
                // The previous block is long and the current block is short.
                int start = (_bs1 - _bs0) / 4;
                int end = start + _bs0 / 2;

                // Unity samples (no overlap).
                //buf[..start].CopyTo(_overlap.Slice(0, start).Span);
                _overlap[..start].Span.CopyTo(buf[..start]);

                // Overlapping samples.
                OverlapAdd(buf[start..],
                    _overlap[start..end].Span,
                    _imdct[..(_bs0 / 2)].Span,
                    win);
            }
            else
            {
                // The previous block is short and the current block is long.
                int start = (_bs1 - _bs0) / 4;
                int end = start + _bs0 / 2;

                // Overlapping samples.
                OverlapAdd(buf[..(_bs0 / 2)],
                    _overlap[..(_bs0 / 2)].Span,
                    _imdct[start..end].Span,
                    win);

                // Unity samples (no overlap).
                // _imdct.Slice(end, _bs1 / 2 - end).Span.CopyTo(buf.Slice(_bs0 / 2));
                //                buf[self.bs0 / 2..].copy_from_slice(&self.imdct[end..self.bs1 / 2]);
                _imdct[end..(_bs1 / 2)].Span.CopyTo(buf[(_bs0 / 2)..]);
            }


            // Clamp the output samples.
            for (var i = 0; i < buf.Length; i++)
            {
                buf[i] = Math.Clamp(buf[i], -1, 1);
            }
        }


        // Save right-half of IMDCT buffer for later.
        _imdct.Span[(bs / 2)..bs].CopyTo(_overlap.Span[..(bs / 2)]);
    }

    private static void OverlapAdd(Span<float> output, Span<float> left, Span<float> right, ReadOnlySpan<float> win)
    {
        if (left.Length != right.Length || left.Length != win.Length || left.Length != output.Length)
            throw new ArgumentException("All input spans must have the same length.");

        for (int i = 0; i < left.Length; i++)
        {
            double s0 = left[i];
            double s1 = right[i];
            double w0 = win[win.Length - i - 1];
            double w1 = win[i];
            output[i] = (float)(s0 * w0 + s1 * w1);
        }
    }
}