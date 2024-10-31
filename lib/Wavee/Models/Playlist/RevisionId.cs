namespace Wavee.Models.Playlist;

public struct RevisionId : IEquatable<RevisionId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionId"/> struct.
    /// </summary>
    /// <param name="base64Revision">The base64-encoded revision ID.</param>
    public RevisionId(string base64Revision)
    {
        if (string.IsNullOrWhiteSpace(base64Revision))
            throw new ArgumentException("Revision ID cannot be null or empty.", nameof(base64Revision));

        var original = base64Revision;

        // Decode the base64 string to bytes
        var bytes = Convert.FromBase64String(base64Revision);
        var hexDumped = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

        if (hexDumped.Length < 8)
        {
            throw new ArgumentException("Invalid revision ID length.", nameof(base64Revision));
        }

        // Extract the first 8 hex characters (4 bytes) as the Number
        var decimalPartHex = hexDumped.Substring(0, 8);
        Number = Convert.ToInt32(decimalPartHex, 16);

        // Extract the remaining hex characters as the Id
        Id = hexDumped.Substring(8);

        // Optional: Validate the Id length or format if necessary
    }

    public int Number { get; }

    public string Id { get; }

    /// <summary>
    /// Returns the revision key in "Number,Id" format.
    /// </summary>
    public override string ToString()
    {
        // //$"{Number},{Id}";
        // if (Number is 0)
        // {
        //     return Id;
        // }
        return $"{Number},{Id}";
    }

    public bool Equals(RevisionId other) => Number == other.Number && Id == other.Id;

    public override bool Equals(object obj) => obj is RevisionId other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Number, Id);
}