namespace Wavee.Spotify.Extensions;

internal static class StreamExtensions
{
    public static string ReadToEnd(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}