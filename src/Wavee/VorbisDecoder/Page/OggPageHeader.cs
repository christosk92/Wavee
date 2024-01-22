
using System.Buffers.Binary;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Vorbis.Infrastructure;
using Wavee.Vorbis.Infrastructure.Stream;

namespace Wavee.Vorbis.Page;

internal readonly record struct OggPageHeader(byte Version, ulong AbsGp, uint Serial, uint Sequence, uint Crc,
    byte NSegments, bool IsContinuation, bool IsFirstPage, bool IsLastPage)
{
    public static Result<OggPageHeader> ReadPageHeader<TB>(TB reader) where TB : IReadBytes
    {
        // The OggS marker should be present.
        try
        {
            var marker = reader.ReadQuadBytes();

            if (!marker.SequenceEqual(OggPage.OggPageMarker))
            {
                return new Result<OggPageHeader>(new DecodeError("ogg: missing ogg stream marker"));
            }

            var version = reader.ReadByte();

            //There is only one OGG version, which is 0
            if (version is not 0)
                return new Result<OggPageHeader>(new DecodeError("ogg: invalid ogg version"));

            var flags = reader.ReadByte();

            // Only the first 3 least-significant bits are used for flags.
            if ((flags & 0xf8) != 0)
            {
                return new Result<OggPageHeader>(new DecodeError("ogg: invalid flag bits set"));
            }

            var tsResult  = reader.ReadULong();
            if (tsResult.IsFaulted)
                return new Result<OggPageHeader>(tsResult.Error());
            var serialResult = reader.ReadUInt();
            if (serialResult.IsFaulted)
                return new Result<OggPageHeader>(serialResult.Error());
            var sequenceResult = reader.ReadUInt();
            if (sequenceResult.IsFaulted)
                return new Result<OggPageHeader>(sequenceResult.Error());
            var crcResult = reader.ReadUInt();
            if (crcResult.IsFaulted)
                return new Result<OggPageHeader>(crcResult.Error());
            var nSegmentsReadByte = reader.ReadByte();

            return new OggPageHeader(
                Version: version,
                AbsGp: tsResult.Success(),
                Serial: serialResult.Success(),
                Sequence: sequenceResult.Success(),
                Crc: crcResult.Success(),
                NSegments: nSegmentsReadByte,
                IsContinuation: (flags & 0x01) != 0,
                IsFirstPage: (flags & 0x02) != 0,
                IsLastPage: (flags & 0x04) != 0
            );
        }
        catch (IOException e)
        {
            return new Result<OggPageHeader>(e);
        }
    }


    /// <summary>
    /// Quickly synchronizes the provided reader to the next OGG page capture pattern, but does not
    /// perform any further verification.
    /// </summary>
    /// <param name="reader"></param>
    /// <typeparam name="TB"></typeparam>
    /// <returns></returns>
    public static Result<Unit> SyncPage<TB>(TB reader) where TB : IReadBytes
    {
        try
        {
            var marker = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadQuadBytes());

            while (!OggMarkerBytesEqualToPageMarker(marker))
            {
                marker <<= 8;
                marker |= reader.ReadByte();
            }

            return Unit.Default;

            static bool OggMarkerBytesEqualToPageMarker(uint marker)
            {
                Span<byte> markerBytes = stackalloc byte[sizeof(uint)];
                BinaryPrimitives.WriteUInt32BigEndian(markerBytes, marker);
                return markerBytes.SequenceEqual(OggPage.OggPageMarker);
            }
        }
        catch (Exception e)
        {
            return new Result<Unit>(e);
        }
    }
}