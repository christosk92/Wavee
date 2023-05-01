
namespace Wavee.Spotify.Helpers.Extensions;

internal static class SpanExtensions
{
    public static ReadOnlySpan<T> SplitTo<T>(this ref ReadOnlySpan<T> span, int index)
    {
        if (index < 0 || index > span.Length) throw new ArgumentOutOfRangeException(nameof(index));

        var first = span.Slice(0, index);
        var second = span.Slice(index);
        span = second;
        return first;
    }

    public static ReadOnlySpan<T> SplitAt<T>(this ref ReadOnlySpan<T> right, int index)
    {
        if (index < 0 || index > right.Length) throw new ArgumentOutOfRangeException(nameof(index));

        var left = right.Slice(0, index);
        right = right.Slice(index);
        return left;
    }

    public static ReadOnlySpan<T> IntoChunks<T>(this ref ReadOnlySpan<T> tail, int sizeOfT)
    {
        var chunks = tail.Length / sizeOfT;
        var tailPos = sizeOfT * chunks;
        var tailLen = tail.Length - tailPos;

        var head = tail.Slice(0, tailPos);
        tail = tail.Slice(tailPos, tailLen);
        return head;
    }
}