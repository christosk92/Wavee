using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder;

internal sealed class Imdct
{
    private int _n2;

    private Complex32[] _twiddle;

    /// <summary>
    /// Instantiate a N-point IMDCT with a given scale factor.
    /// </summary>
    /// <param name="n">The value of `n` is the number of spectral samples and must be a power-of-2.</param>
    /// <param name="scale"></param>
    public Imdct(int n, double scale = 1.0)
    {
        if (n < 4 || (n & (n - 1)) != 0)
            throw new ArgumentException("n must be a power of 2 >= 4");

        _n2 = n / 2;

        // Pre-compute the twiddle factors.
        var twiddle = new Complex32[_n2];

        //        let alpha = 1.0 / 8.0 + if scale.is_sign_positive() { 0.0 } else { n2 as f64 };
        var alpha = 1.0 / 8.0 + (scale > 0 ? 0.0 : _n2);
        var pi_n = Math.PI / n;
        var sqrtScale = Math.Sqrt(Math.Abs(scale));

        for (var k = 0; k < _n2; k++)
        {
            var theta = pi_n * (alpha + (double)k);
            var re = Math.Cos(theta) * sqrtScale;
            var im = Math.Sin(theta) * sqrtScale;
            twiddle[k] = new Complex32((float)re, (float)im);
        }

        _twiddle = twiddle;
    }


    /// <summary>
    /// Performs the the N-point Inverse Modified Discrete Cosine Transform.
    ///
    /// The number of input spectral samples provided by the slice `spec` must equal the value of N
    /// that the IMDCT was instantiated with. The length of the output slice, `out`, must be of
    /// length 2N. Failing to meet these requirements will throw an assertion. 
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="output"></param>
    internal void CalcReverse(Span<float> spec, Span<float> output)
    {
        //spectral length: 2x FFT length, 0.5x output length
        var n = _n2 << 1;
        //1x fft size, 0.25x output length
        var n2 = n >> 1;
        //0.5x fft size
        var n4 = n >> 2;

        // The spectrum length must be the same as N.
        if (spec.Length != n)
            throw new ArgumentException("The spectrum length must be the same as N.");
        // The output length must be 2x the spectrum length.
        if (output.Length != n * 2)
            throw new ArgumentException("The output length must be 2x the spectrum length.");
        // Pre-FFT twiddling and packing of the real input signal values into complex signal values.

        var scratch = new Complex32[_n2];
        for (int i = 0; i < _n2; i++)
        {
            var even = spec[i * 2];
            var odd = -spec[n - 1 - i * 2];
            var w = _twiddle[i];

            var re = odd * w.Imaginary - even * w.Real;
            var im = odd * w.Real + even * w.Imaginary;
            scratch[i] = new Complex32(re, im);
        }

        Fourier.Forward(scratch, FourierOptions.NoScaling);
        // FftSharp.Transform.FFT(scratch);
        // Split the output vector (2N samples) into 4 vectors (N/2 samples each).
        var vec0 = output.Slice(0, n2);
        var vec1 = output.Slice(n2, n2);
        var vec2 = output.Slice(2 * n2, n2);
        var vec3 = output.Slice(3 * n2, n2);

        // Post-FFT twiddling and processing to expand the N/2 complex output values into 2N real
        // output samples.
        for (int i = 0; i < n4; i++)
        {
            var x = scratch[i].Conjugate();
            var w = new Complex32((float)_twiddle[i].Real, (float)_twiddle[i].Imaginary);
            var val = w * x;

            int fi = 2 * i;
            int ri = n2 - 1 - 2 * i;

            vec0[ri] = (float)-val.Imaginary;
            vec1[fi] = (float)val.Imaginary;
            vec2[ri] = (float)val.Real;
            vec3[fi] = (float)val.Real;
        }

        for (int i = 0; i < n4; i++)
        {
            var x = scratch[n4 + i].Conjugate();
            var w = new Complex32(_twiddle[n4 + i].Real, _twiddle[n4 + i].Imaginary);
            var val = w * x;

            int fi = 2 * i;
            int ri = n2 - 1 - 2 * i;

            vec0[fi] = (float)-val.Real;
            vec1[ri] = (float)val.Real;
            vec2[fi] = (float)val.Imaginary;
            vec3[ri] = (float)val.Imaginary;
        }
    }


    private static Complex ComplexMultiply(Complex a, Complex b)
    {
        return new Complex(a.Real * b.Real - a.Imaginary * b.Imaginary,
            a.Real * b.Imaginary + a.Imaginary * b.Real);
    }
}

public static class ComplexExtensions
{
    public static Complex Conjugate(this Complex c)
    {
        return new Complex(c.Real, -c.Imaginary);
    }
}