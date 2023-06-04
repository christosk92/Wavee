using System.Text;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;

namespace Wavee.Spotify.Infrastructure.Cache;

public readonly struct SpotifyCache : ISpotifyCache
{
    private readonly Option<string> _root;
    private readonly Option<string> _audioFilesRoot;
    public SpotifyCache(Option<string> root, string en)
    {
        _root = root;
        _audioFilesRoot = _root.Map(r => Path.Combine(r, "audiofiles"));
    }

    public static bool Initialized { get; set; }

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

    public Option<TrackOrEpisode> Get(AudioId audioId)
    {
        return Option<TrackOrEpisode>.None;
    }

    public Unit Save(TrackOrEpisode fetchedTrack)
    {
        return Unit.Default;
    }

    public Dictionary<AudioId, Option<TrackOrEpisode>> GetBulk(Seq<AudioId> request)
    {
        var result = new Dictionary<AudioId, Option<TrackOrEpisode>>();
        foreach (var audioId in request)
        {
            result.Add(audioId, Option<TrackOrEpisode>.None);
        }
        return result;
    }

    public Unit SaveBulk(Seq<TrackOrEpisode> result)
    {
        return Unit.Default;
    }

    public Option<ReadOnlyMemory<byte>> GetRawEntity(AudioId id)
    {
       return Option<ReadOnlyMemory<byte>>.None;
    }

    public Unit SaveRawEntity(AudioId Id, string title, ReadOnlyMemory<byte> data, DateTimeOffset expiration)
    {
      return Unit.Default;
    }

    public Option<SpotifyColors> GetColorFor(string imageUrl)
    {
        return Option<SpotifyColors>.None;
    }

    public Unit SaveColorFor(string imageUrl, SpotifyColors response)
    {
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