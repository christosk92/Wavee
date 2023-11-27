using TagLib;
using Wavee.Domain.Playback.Player;
using File = System.IO.File;

namespace Wavee.Application.Playback;

public sealed class LocalMediaSource : IWaveeMediaSource, IAsyncDisposable
{
    private static IReadOnlyDictionary<string, string> _empty = new Dictionary<string, string>();

    private readonly Properties _properties;
    private readonly Tag _tag;
    private readonly Stream _stream;
    private IReadOnlyDictionary<string, string> _metadata;

    private LocalMediaSource(Stream stream,
        Properties properties,
        Tag tag,
        IReadOnlyDictionary<string, string>? metadata)
    {
        _stream = stream;
        _properties = properties;
        _tag = tag;

        var old = metadata ?? _empty;
        var finalMetadata = new Dictionary<string, string>();
        foreach (var (key, value) in old)
        {
            finalMetadata.Add(key, value);
        }

        //add tags
        finalMetadata.TryAdd("title", _tag.Title);
        finalMetadata.TryAdd("artist", _tag.FirstPerformer);
        finalMetadata.TryAdd("album", _tag.Album);
        finalMetadata.TryAdd("track", _tag.Track.ToString());
        finalMetadata.TryAdd("year", _tag.Year.ToString());
        finalMetadata.TryAdd("genre", _tag.FirstGenre);
        finalMetadata.TryAdd("comment", _tag.Comment);
        finalMetadata.TryAdd("disc", _tag.Disc.ToString());
        finalMetadata.TryAdd("albumartist", _tag.FirstAlbumArtist);
        finalMetadata.TryAdd("composer", _tag.FirstComposer);
        _metadata = finalMetadata;
    }

    public static LocalMediaSource CreateFromFilePath(string path,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", path);
        }

        var stream = File.OpenRead(path);
        var tagFile = TagLib.File.Create(path);
        tagFile.Dispose();

        return new LocalMediaSource(stream, tagFile.Properties, tagFile.Tag, metadata);
    }

    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    public ValueTask<Stream> CreateStream()
    {
        return new ValueTask<Stream>(_stream);
    }

    public TimeSpan Duration => _properties.Duration;

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}