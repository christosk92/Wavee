using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CommunityToolkit.HighPerformance;

namespace Wavee;

internal static class StreamExtensions
{
    //ReadExactly
    public static void ReadExactly(this Stream stream, Span<byte> buffer) =>
        _ = ReadAtLeastCore(stream, buffer, buffer.Length, throwOnEndOfStream: true);

    public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out T? item))
            {
                yield return item;
            }
        }
    }

    private static int ReadAtLeastCore(this Stream stream, Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream)
    {
        Debug.Assert(minimumBytes <= buffer.Length);

        int totalRead = 0;
        while (totalRead < minimumBytes)
        {
            int read = stream.Read(buffer.Slice(totalRead));
            if (read == 0)
            {
                if (throwOnEndOfStream)
                {
                    throw new EndOfStreamException();
                }

                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }
}