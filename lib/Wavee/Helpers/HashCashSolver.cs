using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace Wavee.Helpers;

public static class HashCashSolver
{
    public static bool SolveHashCash(byte[] ctx, byte[] prefix, int length, out byte[] dst, out TimeSpan elapsedTime)
    {
        const int TIMEOUT = 5; // seconds
        var startTime = DateTime.UtcNow;

        // Compute SHA1 digest of ctx
        using var sha1 = SHA1.Create();
        ReadOnlySpan<byte> md = sha1.ComputeHash(ctx);

        // Read md[12..20], interpret as big-endian long
        //long target = ReadBigEndianLong(md, 12);
        long target = BinaryPrimitives.ReadInt64BigEndian(md[12..20]);


        long counter = 0L;
        dst = null;

        while (true)
        {
            // Check timeout
            elapsedTime = DateTime.UtcNow - startTime;
            if (elapsedTime.TotalSeconds >= TIMEOUT)
            {
                return false;
            }

            // Compute suffix
            long sum = target + counter;
            //byte[] sumBytes = GetBigEndianBytes(sum);
            byte[] sumBytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(sumBytes, sum);

            //byte[] counterBytes = GetBigEndianBytes(counter);
            byte[] counterBytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

            byte[] suffix = new byte[16];
            Array.Copy(sumBytes, 0, suffix, 0, 8);
            Array.Copy(counterBytes, 0, suffix, 8, 8);

            // Compute SHA1(prefix + suffix)
            sha1.Initialize();
            sha1.TransformBlock(prefix, 0, prefix.Length, null, 0);
            sha1.TransformFinalBlock(suffix, 0, suffix.Length);
            ReadOnlySpan<byte> md2 = sha1.Hash;

            // Read md2[12..20], interpret as big-endian long
            //long hashValue = ReadBigEndianLong(md2, 12);
            long hashValue = BinaryPrimitives.ReadInt64BigEndian(md2[12..20]);

            // Count trailing zeros
            //int trailingZeros = CountTrailingZeros(hashValue);
            int trailingZeros = CountTrailingZeros(hashValue);

            if (trailingZeros >= length)
            {
                dst = suffix;
                elapsedTime = DateTime.UtcNow - startTime;
                return true;
            }

            counter++;
        }
    }

    private static int CountTrailingZeros(long value)
    {
        ulong uvalue = (ulong)value;
        return BitOperations.TrailingZeroCount(uvalue);
    }
}