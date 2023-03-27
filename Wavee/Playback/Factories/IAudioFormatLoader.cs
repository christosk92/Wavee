namespace Wavee.Playback.Factories;

public interface IAudioFormatLoader
{
    IAudioFormat Load(Stream stream);
}