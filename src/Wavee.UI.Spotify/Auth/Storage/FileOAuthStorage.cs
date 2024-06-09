using System;
using System.IO;
using System.Threading.Tasks;

namespace Wavee.UI.Spotify.Auth.Storage;

public sealed class FileOAuthStorage : IOAuthStorage
{
    private readonly string _filePath;

    public FileOAuthStorage() : this(AppContext.BaseDirectory)
    {
    }

    public FileOAuthStorage(string rootPath)
    {
        _filePath = Path.Combine(rootPath, "credentials");
    }

    public ValueTask<byte[]?> Get()
    {
        if (!File.Exists(_filePath))
        {
            return new ValueTask<byte[]?>((byte[])null);
        }

        var data = File.ReadAllBytes(_filePath);
        return new ValueTask<byte[]>(data);
    }

    public ValueTask Store(byte[] data)
    {
        File.WriteAllBytes(_filePath, data);
        return new ValueTask();
    }
}