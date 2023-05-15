﻿namespace Wavee.Core;

public interface IAudioDecoder : IDisposable
{
    Span<float> ReadSamples(int samples);
    TimeSpan Position { get; }
    TimeSpan TotalTime { get; }
    void Seek(TimeSpan pPosition);
}