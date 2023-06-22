namespace NVorbis.Contracts
{
    interface IMdct
    {
        void Reverse(Span<float> samples, int sampleCount);
    }
}
