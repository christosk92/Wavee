using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wavee;

internal static class Polyfills
{
    public static string GetUTF8String(this ReadOnlySpan<byte> bytes)
    { 
        return Encoding.UTF8.GetString(bytes.ToArray());
    }
    public static string GetUTF8String(this Span<byte> bytes)
    {
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    public static void FillRandomBytes(this Span<byte> data)
    {
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            byte[] randomBytes = new byte[data.Length];
            rng.GetBytes(randomBytes);
        
            unsafe
            {
                fixed (byte* pBytes = randomBytes)
                fixed (byte* pData = &MemoryMarshal.GetReference(data))
                {
                    System.Runtime.InteropServices.Marshal.Copy(randomBytes, 0, (IntPtr)pData, data.Length);
                }
            }
        }
    }
    
    public static float ReadSingleLittleEndian(this Span<byte> bytes)
    {
        //read float from bytes
        return !BitConverter.IsLittleEndian ?
            Int32BitsToSingle(ReverseEndianness(MemoryMarshal.Read<int>(bytes))) :
            MemoryMarshal.Read<float>(bytes);
    }
    private static unsafe float Int32BitsToSingle(int value) => *((float*)&value);
    private static int ReverseEndianness(int value) => (int)ReverseEndianness((uint)value);
  
    /// <summary>
    /// Reverses a primitive value - performs an endianness swap
    /// </summary>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseEndianness(uint value)
    {
        // This takes advantage of the fact that the JIT can detect
        // ROL32 / ROR32 patterns and output the correct intrinsic.
        //
        // Input: value = [ ww xx yy zz ]
        //
        // First line generates : [ ww xx yy zz ]
        //                      & [ 00 FF 00 FF ]
        //                      = [ 00 xx 00 zz ]
        //             ROR32(8) = [ zz 00 xx 00 ]
        //
        // Second line generates: [ ww xx yy zz ]
        //                      & [ FF 00 FF 00 ]
        //                      = [ ww 00 yy 00 ]
        //             ROL32(8) = [ 00 yy 00 ww ]
        //
        //                (sum) = [ zz yy xx ww ]
        //
        // Testing shows that throughput increases if the AND
        // is performed before the ROL / ROR.

        return RotateRight(value & 0x00FF00FFu, 8) // xx zz
               +RotateLeft(value & 0xFF00FF00u, 8); // ww yy
    }
    
    /// <summary>
    /// Rotates the specified value right by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROR.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..31] is treated as congruent mod 32.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [CLSCompliant(false)]
    public static uint RotateRight(uint value, int offset)
        => (value >> offset) | (value << (32 - offset));
    
    /// <summary>
    /// Rotates the specified value left by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROL.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..31] is treated as congruent mod 32.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [CLSCompliant(false)]
    public static uint RotateLeft(uint value, int offset)
        => (value << offset) | (value >> (32 - offset));
}