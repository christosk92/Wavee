using Eum.UI.Helpers;

namespace Eum.UI.Crypto.Randomness;

public abstract class WasabiRandom 
{
    public abstract void GetBytes(byte[] output);

    public abstract void GetBytes(Span<byte> output);

    public virtual byte[] GetBytes(int length)
    {
        Guard.MinimumAndNotNull(nameof(length), length, 1);
        var buffer = new byte[length];
        GetBytes(buffer);
        return buffer;
    }

    public abstract int GetInt(int fromInclusive, int toExclusive);

    public string GetString(int length, string chars)
    {
        Guard.MinimumAndNotNull(nameof(length), length, 1);
        Guard.NotNullOrEmpty(nameof(chars), chars);

        var random = new string(Enumerable
            .Repeat(chars, length)
            .Select(s => s[GetInt(0, s.Length)])
            .ToArray());
        return random;
    }

}