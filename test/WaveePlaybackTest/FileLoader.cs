using System.Diagnostics;
using NAudio.Vorbis;
using NAudio.Wave;
using Wavee.Playback;
using Wavee.Playback.Item;
using Wavee.Playback.Normalisation;
using Wavee.Sinks.NAudio;
using Wavee.UI.Models.Local;

public class FileLoader : ITrackLoader
{
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

        var playbackitem = new PlaybackItem
        {
            Item = audioItem
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
        decoder.CurrentTime = position;
        if (decoder.CurrentTime != position)
        {
            throw new InvalidOperationException("Failed to seek to the starting position");
        }

        // Ensure streaming mode now that we are ready to play from the requested position.
        var streamLoaderController
            = new StreamLoaderController();

        streamLoaderController.SetStreamMode();

        var isExplicit = false;

        Debug.WriteLine("Loaded track: " + trackId);

        return Task.FromResult(new PlayerLoadedTrackData
        {
            Decoder = new NAudioDecoder(decoder),
            NormalisationData = normalisationData,
            StreamLoaderController = streamLoaderController,
            AudioItem = playbackitem,
            BytesPerSecond = bytesPerSecond,
            DurationMs = duration.TotalMilliseconds,
            IsExplicit = isExplicit,
            StreamPositionMs = decoder.CurrentTime.TotalMilliseconds
        })!;
    }

    private static WaveStream GetWaveStreamForAudioFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();

        if (extension == ".mp3")
        {
            return new Mp3FileReader(filePath);
        }
        else if (extension == ".wav")
        {
            return new WaveFileReader(filePath);
        }
        else if (extension == ".ogg")
        {
            return new VorbisWaveReader(filePath);
        }
        else if (extension == ".flac")
        {
            throw new NotSupportedException("Flac is not supported yet");
            //return new FlacReader(filePath);
        }
        else
        {
            throw new NotSupportedException("Unsupported audio file format");
        }
    }
}