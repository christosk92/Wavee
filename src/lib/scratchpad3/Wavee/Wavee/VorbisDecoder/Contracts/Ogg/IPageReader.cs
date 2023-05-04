namespace Wavee.VorbisDecoder.Contracts.Ogg
{
    interface IPageReader : IDisposable
    {
        void Lock();
        bool Release();

        long ContainerBits { get; }
        long WasteBits { get; }

        bool ReadNextPage();

        bool ReadPageAt(long offset);
    }
}
