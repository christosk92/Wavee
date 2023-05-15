using LanguageExt;
using LibVLCSharp.Shared;
using Wavee.Core;
using Wavee.Core.Contracts;
using Wavee.Core.Infrastructure.Traits;
using static LanguageExt.Prelude;

namespace Wavee.AudioOutput.LibVLC;

public sealed class LibVlcOutput : AudioOutputIO
{
    public Unit Start()
    {
        throw new NotImplementedException();
    }

    public Unit Pause()
    {
        throw new NotImplementedException();
    }

    public ValueTask<IAudioDecoder> OpenDecoder(IAudioStream stream)
    {
        throw new NotImplementedException();
    }

    public Unit DiscardSamples()
    {
        throw new NotImplementedException();
    }

    public Unit WriteSamples(ReadOnlySpan<float> sample)
    {
        throw new NotImplementedException();
    }
}