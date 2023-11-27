using TagLib;
using Wavee.Domain.Playback.Player;
using File = System.IO.File;

namespace Wavee.Application.Playback;

public sealed class LocalMediaSource : IWaveeMediaSource
{
    private readonly Properties _properties;
    private readonly Tag _tag;
    private readonly Stream _stream;

    private LocalMediaSource(Stream stream, Properties properties, Tag tag)
    {
        _stream = stream;
        _properties = properties;
        _tag = tag;
    }

    public static LocalMediaSource CreateFromFilePath(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", path);
        }

        var stream = File.OpenRead(path);
        var tagFile = TagLib.File.Create(path);
        tagFile.Dispose();
        
        return new LocalMediaSource(stream, tagFile.Properties, tagFile.Tag);
    }

    public ValueTask<Stream> CreateStream()
    {
        return new ValueTask<Stream>(_stream);
    }

    public TimeSpan Duration => _properties.Duration;
}