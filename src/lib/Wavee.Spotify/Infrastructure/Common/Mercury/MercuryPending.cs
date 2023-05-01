namespace Wavee.Spotify.Infrastructure.Common.Mercury;

internal record struct MercuryPending(
    Seq<ReadOnlyMemory<byte>> Parts,
    Option<ReadOnlyMemory<byte>> Partial,
    Option<TaskCompletionSource<MercuryResponse>> Callback,
    bool Flag)
{
    public MercuryPending WithPartial(Option<ReadOnlyMemory<byte>> newPartial)
    {
        return this with { Partial = newPartial };
    }

    public MercuryPending WithPart(ReadOnlyMemory<byte> newPart)
    {
        return new MercuryPending(Parts.Add(newPart), Partial, Callback, Flag);
    }

    public MercuryPending WithFlag(bool b)
    {
        return new MercuryPending(Parts, Partial, Callback, b);
    }
}