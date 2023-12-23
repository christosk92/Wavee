using LiteDB;
using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Models.Credentials;

namespace ConsoleApp1;

public sealed class LiteDbCredentialsStorage
{
    private readonly ILiteCollection<IdByte>  _collection;

    public LiteDbCredentialsStorage(ILiteDatabase db)
    {
        _collection = db.GetCollection<IdByte>();
    }
    
    public void Store(string name, SpotifyCredentialsType type, byte[] data)
    {
        //first get all the credentials with the same name and type
        var credentials = _collection.Find(x => x.Name == name && x.Type == type);
        //then delete them
        foreach (var credential in credentials)
        {
            _collection.Delete(credential.Id);
        }
        
        //then add the new one
        _collection.Insert(new IdByte
        {
            Name = name,
            Type = type,
            Data = data,
            Id = ObjectId.NewObjectId(),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
    
    public string? GetDefaultUserName()
    {
        var defaultUser = _collection.Query()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (defaultUser is null)
        {
            return null;
        }
        
        return defaultUser.Name;
    }

    public byte[]? GetFor(string name, SpotifyCredentialsType type)
    {
        var credentials = _collection
            .Find(x => x.Name == name && x.Type == type).MaxBy(x => x.CreatedAt);
        if (credentials is null)
        {
            return null;
        }

        return credentials.Data;
    }
}
public sealed class IdByte
{
    [BsonId]
    public required ObjectId Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required SpotifyCredentialsType Type { get; init; }
    public required string Name { get; init; }
    public required byte[] Data { get; init; }
}