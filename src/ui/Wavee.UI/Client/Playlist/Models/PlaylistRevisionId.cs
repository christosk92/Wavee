using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using Google.Protobuf;

namespace Wavee.UI.Client.Playlist.Models;

public readonly record struct PlaylistRevisionId(BigInteger Revision)
{
    public string ToHex()
    {
        //get the bytes of the BigInteger
        Span<byte> bytes = Revision.ToByteArray();
        //Just converting to hex may give for example:
        //0000002357585c86ab3b29950dcfd4d7855e79c763843f85
        //We need the first 8 letters seperate, and then the rest 
        //8 chars = 4 bytes = 32 bits = 8 hex chars
        //so we take teh first 4 bytes, convert it to a number, then append the rest
        //to the string

        var sb = new StringBuilder();
        Span<byte> first4Bytes = bytes.Slice(0, 4);
        var first4BytesAsNumber = BinaryPrimitives.ReadUInt32BigEndian(first4Bytes);
        sb.Append(first4BytesAsNumber.ToString());
        sb.Append(",");
        Span<byte> restOfBytes = bytes[4..];
        foreach (var b in restOfBytes)
        {
            sb.Append(b.ToString("x2").ToLower());
        }
        return sb.ToString();
    }

    public static PlaylistRevisionId FromByteString(ByteString byteString)
    {
        return new PlaylistRevisionId(new BigInteger(byteString.Span));
    }

    public static PlaylistRevisionId FromBase64(string base64)
    {
        return FromByteString(ByteString.FromBase64(base64));
    }
}