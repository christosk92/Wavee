using System.Text;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;

namespace Wavee.Spotify.Infrastructure.Cache;

internal readonly struct SpotifyCache : ISpotifyCache
{
    private readonly Option<string> _root;
    private readonly Option<string> _audioFilesRoot;
    public SpotifyCache(Option<string> root)
    {
        _root = root;
        _audioFilesRoot = _root.Map(r => Path.Combine(r, "audiofiles"));
    }

    public Option<Stream> AudioFile(AudioFile file)
    {
        //TODO:
        //files are stored in a folder structure like this:
        //audiofiles/spotify/{fileId}
        //where {fileId} is the id of the file
        var path = _audioFilesRoot.Map(r => Path.Combine(r, ToBase16(file)));
        if (path.IsSome && File.Exists(path.ValueUnsafe()))
        {
            return File.OpenRead(path.ValueUnsafe());
        }
        
        return Option<Stream>.None;
    }

    public Unit SaveAudioFile(AudioFile file, byte[] data)
    {
        var path = _audioFilesRoot.Map(r => Path.Combine(r, ToBase16(file)));
        if (path.IsSome)
        {
            //create subfolders if they don't exist
            Directory.CreateDirectory(Path.GetDirectoryName(path.ValueUnsafe())!);
            File.WriteAllBytes(path.ValueUnsafe(), data);
        }
        
        return Unit.Default;
    }

    static string ToBase16(AudioFile file)
    {
        var bytes = file.FileId.Span;
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        
        return hex.ToString();
    }
}