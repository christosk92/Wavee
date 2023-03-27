using System.Diagnostics;
using Wavee.Playback;
using Wavee.Playback.Factories;
using Wavee.Playback.Item;
using Wavee.Playback.Models;
using Wavee.Playback.Normalisation;

public class FileLoader : ITrackLoader
{
    private readonly IAudioFormatLoader _audioFormatLoader;

    public FileLoader(IAudioFormatLoader audioFormatLoader)
    {
        _audioFormatLoader = audioFormatLoader;
    }

    public Task<PlayerLoadedTrackData?> LoadTrackAsync(string trackId, double positionMs)
    {
        //check the format
        using var file = TagLib.File.Create(trackId);
        var bitrate =
            file.Properties.AudioBitrate;

        var audioItem = new LocalTrack
        {
            Title = file.Tag.Title ?? file.Name,
        };


        file.Dispose();
        var bytesPerSecond = bitrate / 8;

        //assume no normalisation data for now

        //       file.Properties.AudioSampleRate,
        //file.Properties.AudioChannels,
        var normalisationData = NormalisationData.Default;
        var decoder = GetWaveStreamForAudioFile(trackId);

        var duration = TimeSpan.FromSeconds(decoder.TotalTime.TotalSeconds);

        // Don't try to seek past the track's duration.
        // If the position is invalid just start from
        // the beginning of the track.
        var position = TimeSpan.FromMilliseconds(positionMs);
        if (position > duration)
        {
            position = TimeSpan.Zero;
        }

        // Ensure the starting position. Even when we want to play from the beginning,
        // the cursor may have been moved by parsing normalisation data. This may not
        // matter for playback (but won't hurt either), but may be useful for the
        // passthrough decoder.
        decoder.Seek(position);
        if (decoder.CurrentTime != position)
        {
            throw new InvalidOperationException("Failed to seek to the starting position");
        }

        var isExplicit = false;

        Debug.WriteLine("Loaded track: " + trackId);

        return Task.FromResult(new PlayerLoadedTrackData
        {
            Format = decoder,
            NormalisationData = normalisationData,
            AudioItem = audioItem,
            BytesPerSecond = bytesPerSecond,
            DurationMs = duration.TotalMilliseconds,
            IsExplicit = isExplicit,
            StreamPositionMs = decoder.CurrentTime.TotalMilliseconds
        })!;
    }

    private IAudioFormat GetWaveStreamForAudioFile(string filePath)
    {
        //string extension = Path.GetExtension(filePath).ToLower();
        return _audioFormatLoader.Load(File.OpenRead(filePath));
    }
}