using System.Collections.Immutable;
using Google.Protobuf;
using LiteDB;
using Spotify.Metadata;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Infrastructure.Persistent;

internal sealed class SpotifyGenericRepository : ISpotifyGenericRepository
{
    private class GenericByteEntity
    {
        [BsonId]
        public required string Uri { get; set; }

        public required byte[] Data { get; set; }
    }

    private readonly ILiteCollection<GenericByteEntity> _dataCollection;

    public SpotifyGenericRepository(ILiteDatabase db)
    {
        _dataCollection = db.GetCollection<GenericByteEntity>("items");
    }

    public void AddTrack(Track track)
    {
        try
        {
            var spotifyId = SpotifyId.FromRaw(track.Gid.Span, SpotifyItemType.Track);
            _dataCollection.Insert(new GenericByteEntity
            {
                Uri = spotifyId.ToString(),
                Data = track.ToByteArray()
            });
        }
        catch (LiteException)
        {
            //If duplicate ignore

        }
    }

    public bool TryGetTrack(SpotifyId id, out Track o)
    {
        var item = _dataCollection.FindById(id.ToString());
        if (item is not null)
        {
            o = Track.Parser.ParseFrom(item.Data);
            return true;
        }

        o = null;
        return false;
    }

    public Dictionary<string, ByteString?> GetInBulk(IReadOnlyCollection<string> uris)
    {
        var allitems = _dataCollection
            .Find(x => uris.Contains(x.Uri))
            .ToDictionary(x => x.Uri, x => ByteString.CopyFrom(x.Data));
        foreach (var uri in uris)
        {
            if (!allitems.TryGetValue(uri, out var dt))
            {
                allitems[uri] = null;
            }
        }

        return allitems;
    }

    public void SaveBulk(IReadOnlyCollection<(string EntityUri, ByteString? Value)> bytesData)
    {
        _dataCollection.InsertBulk(bytesData.Where(f=> f.Value is not null)
            .Select(x => new GenericByteEntity
        {
            Uri = x.EntityUri,
            Data = x.Value.ToByteArray()
        }));
    }
}

public interface ISpotifyGenericRepository
{
    void AddTrack(Track track);
    bool TryGetTrack(SpotifyId id, out Track o);
    Dictionary<string, ByteString?> GetInBulk(IReadOnlyCollection<string> uris);
    void SaveBulk(IReadOnlyCollection<(string EntityUri, ByteString? Value)> bytesData);
}