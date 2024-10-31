namespace Wavee.Models.Common;

public struct FileId : IEquatable<FileId>
{
    private const int SIZE = 20;
    private readonly byte[] _data;

    public FileId(byte[] data)
    {
        if (data == null || data.Length != SIZE)
            throw new ArgumentException($"FileId must be exactly {SIZE} bytes long", nameof(data));
        
        _data = (byte[])data.Clone();
    }

    public static FileId FromRaw(byte[] src)
    {
        return new FileId(src);
    }

    public string ToBase16()
    {
        return BitConverter.ToString(_data).Replace("-", "").ToLower();
    }

    public byte[] ToByteArray()
    {
        return (byte[])_data.Clone();
    }

    public override string ToString()
    {
        return ToBase16();
    }

    public override bool Equals(object obj)
    {
        return obj is FileId other && Equals(other);
    }

    public bool Equals(FileId other)
    {
        return _data.SequenceEqual(other._data);
    }

    public override int GetHashCode()
    {
        return BitConverter.ToInt32(_data, 0);
    }

    public static bool operator ==(FileId left, FileId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FileId left, FileId right)
    {
        return !(left == right);
    }

    // Additional methods that might be useful:

    public static FileId FromBase16(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length != SIZE * 2)
            throw new ArgumentException($"Invalid hex string. Must be {SIZE * 2} characters long", nameof(hex));

        byte[] data = new byte[SIZE];
        for (int i = 0; i < SIZE; i++)
        {
            data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return new FileId(data);
    }

    // This method mimics the From<&protocol::metadata::AudioFile> trait implementation
    public static FileId FromAudioFile(byte[] fileId)
    {
        return new FileId(fileId);
    }

    // This method mimics the From<&protocol::metadata::Image> trait implementation
    public static FileId FromImage(byte[] fileId)
    {
        return new FileId(fileId);
    }

    // This method mimics the From<&protocol::metadata::VideoFile> trait implementation
    public static FileId FromVideoFile(byte[] fileId)
    {
        return new FileId(fileId);
    }
}