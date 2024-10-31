namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure;

internal static class IntExtensions
{
    public static ulong WrappingAdd(this ulong x, ulong y)
    {
        return x + y;
    }
    
    public static ulong SaturatingSub(this ulong x, ulong y)
    {
        if (x < y) return 0;
        return x - y;
    }
    public static uint RotateLeft(this uint value, int offset)
    {
        offset %= 32; // Ensure the offset is within the valid range for uint (32 bits)
        return (value << offset) | (value >> (32 - offset));
    }
    public static uint ReverseBits(this uint value)
    {
        value = (value & 0x55555555) << 1 | (value & 0xAAAAAAAA) >> 1;
        value = (value & 0x33333333) << 2 | (value & 0xCCCCCCCC) >> 2;
        value = (value & 0x0F0F0F0F) << 4 | (value & 0xF0F0F0F0) >> 4;
        value = (value & 0x00FF00FF) << 8 | (value & 0xFF00FF00) >> 8;
        return (value << 16) | (value >> 16);
    }
    
    public static ushort ReverseBits(this ushort value)
    {
        value = (ushort)((value & 0x5555) << 1 | (value & 0xAAAA) >> 1);
        value = (ushort)((value & 0x3333) << 2 | (value & 0xCCCC) >> 2);
        value = (ushort)((value & 0x0F0F) << 4 | (value & 0xF0F0) >> 4);
        value = (ushort)((value & 0x00FF) << 8 | (value & 0xFF00) >> 8);
        return value;
    }

    public static ushort RotateLeft(this ushort value, int offset)
    {
        offset &= 15;
        return (ushort)((value << offset) | ((value >> (16 - offset)) & ((1 << offset) - 1)));
    }
    public static int CountOnes(this int value)
    {
        var count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }

        return count;
    }

    public static uint ilog(this uint x)
    {
        uint cnt = 0;
        while (x > 0)
        {
            ++cnt;
            x >>= 1; // this is safe because we'll never get here if the sign bit is set
        }

        return cnt;
    }

    internal static int ilog(this int x)
    {
        int cnt = 0;
        while (x > 0)
        {
            ++cnt;
            x >>= 1; // this is safe because we'll never get here if the sign bit is set
        }

        return cnt;
    }

    public static ulong SaturatingAdd(this ulong x, ulong y)
    {
        var result = x + y;
        if (result < x) return ulong.MaxValue;
        return result;
    }

    public static int NextPowerOfTwo(this int x)
    {
        if (x < 2) return 2;

        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x++;

        return x;
    }
}