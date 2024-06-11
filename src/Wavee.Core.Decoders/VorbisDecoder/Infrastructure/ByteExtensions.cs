namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure;

internal static class ByteExtensions
{
    /// <summary>
    /// Divides one array segment into two at an index.
    /// The first will contain all indices from [0, mid) (excluding
    /// the index mid itself) and the second will contain all
    /// indices from [mid, len) (excluding the index len itself).
    /// </summary>
    /// <typeparam name="T">Type of the array elements.</typeparam>
    /// <param name="self">The array segment to split.</param>
    /// <param name="mid">The index to split at.</param>
    /// <returns>A tuple of two array segments.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if mid > self.Count.</exception>
    public static (ArraySegment<T>, ArraySegment<T>) SplitAt<T>(this ArraySegment<T> self, int mid)
    {
        if (mid > self.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(mid), "Mid must be less than or equal to the length of the segment.");
        }

        var firstSegment = new ArraySegment<T>(self.Array, self.Offset, mid);
        var secondSegment = new ArraySegment<T>(self.Array, self.Offset + mid, self.Count - mid);

        return (firstSegment, secondSegment);
    }
    
    
}