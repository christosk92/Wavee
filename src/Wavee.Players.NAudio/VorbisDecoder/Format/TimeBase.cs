using LanguageExt.Common;
using Wavee.Vorbis.Infrastructure;

namespace Wavee.Vorbis.Format;

internal readonly record struct TimeBase(uint Numer, uint Denom)
{
    /// <summary>
    /// Accurately calculates a `Time` using the `TimeBase` and the provided `TimeStamp`. On
    /// overflow, the seconds field of `Time` wraps.
    /// </summary>
    /// <param name="ts"></param>
    /// <returns></returns>
    public Result<TimeSpan> CalcTime(ulong ts)
    {
        // The dividend requires up-to 96-bits (32-bit timebase numerator * 64-bit timestamp).
        var dividend = (UInt128)Numer * (UInt128)ts;

        // For an accurate floating point division, both the dividend and divisor must have an
        // accurate floating point representation. A 64-bit floating point value has a mantissa of
        // 52 bits and can therefore accurately represent a 52-bit integer. The divisor (the
        // denominator of the timebase) is limited to 32-bits. Therefore, if the dividend
        // requires less than 52-bits, a straight-forward floating point division can be used to
        // calculate the time.
        if (dividend < ((UInt128)1 << 52))
        {
            double secs = (double)dividend / (double)Denom;
            var fracs = secs - Math.Floor(secs);

            return TimeSpan.FromSeconds(secs + fracs);
        }

        // If the dividend requires more than 52 bits, calculate the integer portion using
        // integer arithmetic, then calculate the fractional part separately.
        var quotient = dividend / (UInt128)Denom;

        // The remainder is the fractional portion before being divided by the divisor (the
        // denominator). The remainder will never equal or exceed the divisor (or else the
        // fractional part would be >= 1.0), so the remainder must fit within a u32.
        //            let rem = (dividend - (quotient * u128::from(self.denom))) as u32;
        var rem = (uint)(dividend - (quotient * (UInt128)Denom));

        // The fractional part is the remainder divided by the divisor.
        var frac = (double)rem / (double)Denom;

        // The integer part is the quotient.
        var sec = (double)quotient;

        return TimeSpan.FromSeconds(sec + frac);
    }

    /// <summary>
    /// Accurately calculates a `TimeStamp` from the given `Time` using the `TimeBase` as the
    /// conversion factor. On overflow, the `TimeStamp` wraps.
    /// </summary>
    /// <param name="to"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Result<ulong> CalcTimestamp(TimeSpan time)
    {
        if (Numer <= 0 || Denom <= 0)
            return new Result<ulong>(new ArgumentException("TimeBase numerator or denominator are 0."));

        double frac = time.Milliseconds / 1000.0;
        if (frac < 0.0 || frac >= 1.0)
            return new Result<ulong>(new ArgumentException("Invalid range for Time fractional part."));
        // The dividing factor.
        double k = 1.0 / Numer;

        // Multiplying seconds by the denominator requires up-to 96-bits (32-bit timebase
        // denominator * 64-bit timestamp).
        ulong seconds = (ulong)time.TotalSeconds;
        UInt128 product = (UInt128)seconds * (UInt128)Denom;

        // Like calc_time, a 64-bit floating-point value only has 52-bits of integer precision.
        // If the product requires more than 52-bits, split the product into upper and lower parts
        // and multiply by k separately, before adding back together.
        ulong a;
        if (product > ((UInt128)1 << 52))
        {
            // Split the 96-bit product into 48-bit halves.
            ulong u = (ulong)((product & ~0xffff_ffff_ffffUL) >> 48);
            ulong l = (ulong)((product & 0xffff_ffff_ffffUL) >> 0);

            double uk = u * k;
            double ul = l * k;

            // Add the upper and lower halves.
            // a = ((ulong)uk << 48) + (ulong)ul;
            //            ((uk as u64) << 48).wrapping_add(ul as u64)
            a = ((ulong)(uk * (1UL << 48))).WrappingAdd((ulong)ul);
        }
        else
        {
            a = (ulong)((double)product * k);
        }

        // The fractional portion can be calculated directly using floating-point arithmetic.
        ulong b = (ulong)(k * (double)Denom * frac);

        return a.WrappingAdd(b);
    }
}