using System.Reactive.Subjects;
using LanguageExt;
using LibVLCSharp;

namespace Wavee.Player.Decoding;

internal static class AudioDecoderFactory
{
    public static IAudioDecoder CreateDecoder(Stream stream, TimeSpan duration)
    {
        var libVlc = new LibVLC();
        var mp = new MediaPlayer(libVlc);
        var streamMediaInput = new StreamMediaInput(stream);
        var media = new Media(streamMediaInput);
        return new VorbisDecoderWrapper(libVlc, mp, streamMediaInput, media, duration);
    }
}

internal class VorbisDecoderWrapper : IAudioDecoder
{
    private readonly Subject<Unit> _trackEnded;
    private readonly Subject<TimeSpan> _time;
    private readonly MediaPlayer _mediaPlayer;
    private readonly StreamMediaInput _streamMediaInput;
    private readonly Media _media;
    private TimeSpan _crossfadeDuration;
    private bool _crossfadingOut;
    private bool _crossfadingIn;
    private bool initialized;
    private readonly Timer _timer;
    private readonly LibVLC _libVlc;

    public VorbisDecoderWrapper(LibVLC libVlc, MediaPlayer mediaPlayer, StreamMediaInput streamMediaInput, Media media,
        TimeSpan totalTime)
    {
        _trackEnded = new Subject<Unit>();
        _time = new Subject<TimeSpan>();
        _libVlc = libVlc;
        _streamMediaInput = streamMediaInput;
        _media = media;
        _mediaPlayer = mediaPlayer;
        TotalTime = totalTime;
        _mediaPlayer.TimeChanged += MediaPlayerOnTimeChanged;
        _mediaPlayer.Stopped += MediaPlayerOnEndReached;
        _mediaPlayer.SetVolume(100);
        _timer = new Timer(CrossfadeCallback);
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void MediaPlayerOnEndReached(object sender, EventArgs e)
    {
        _trackEnded.OnNext(Unit.Default);
    }

    private void MediaPlayerOnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
    {
        _time.OnNext(TimeSpan.FromMilliseconds(e.Time));
    }

    public void Pause()
    {
        _mediaPlayer.Pause();
    }

    public void Resume()
    {
        if (initialized)
        {
            _mediaPlayer.Play();
        }
        else
        {
            _mediaPlayer.Play(_media);
            initialized = true;
        }
    }

    public bool IsMarkedForCrossfadeOut => _crossfadingOut;
    public TimeSpan CurrentTime => TimeSpan.FromMilliseconds(_mediaPlayer.Time);
    public TimeSpan TotalTime { get; }
    public IObservable<TimeSpan> TimeChanged => _time;
    public IObservable<Unit> TrackEnded => _trackEnded;

    // public int Read(Span<float> buffer)
    // {
    //     int read;
    //     //read until we have enough samples or read returns 0
    //     while ((read = _decoderActual.Read(buffer, 0, buffer.Length)) > 0)
    //     {
    //         //if we have enough samples, return
    //         if (read == buffer.Length)
    //         {
    //             var gain = CalculateGain(_decoderActual.CurrentTime);
    //             for (var i = 0; i < read; i++)
    //             {
    //                 buffer[i] *= gain;
    //             }
    //
    //             return read;
    //         }
    //         //if we don't have enough samples, read more
    //     }
    //
    //     return read;
    // }

    public Unit MarkForCrossfadeOut(TimeSpan duration)
    {
        _crossfadeDuration = duration;
        _crossfadingOut = true;
        _crossfadingIn = false;
        //start a timer for every 20ms to adjust the volume
        _timer.Change(0, 50);

        return Unit.Default;
    }

    private void CrossfadeCallback(object state)
    {
        var gain = CalculateGain(TimeSpan.FromMilliseconds(_mediaPlayer.Time));
        // _media.AddOption(":audio-filter=normvol");
        // _media.AddOption(":norm-buff-size=10");  // 10 milliseconds
        // _media.AddOption($":norm-max-level={gain}");  // Target volume level (1.0 = 100%)
        
        
        //Since setting the volume on 1 mediaplayer affects all of them
        //we cant just set the volume on the media player, we need to set it on the libvlc instance
        var equalizer = new Equalizer();
        equalizer.SetPreamp(gain * 100);
        _mediaPlayer.SetEqualizer(equalizer);
    }

    public Unit MarkForCrossfadeIn(TimeSpan duration)
    {
        _crossfadeDuration = duration;
        _crossfadingIn = true;
        _crossfadingOut = false;
        _timer.Change(0, 50);
        return Unit.Default;
    }

    private float CalculateGain(TimeSpan time)
    {
        if (_crossfadeDuration == TimeSpan.Zero)
        {
            return 1;
        }

        if (_crossfadingOut)
        {
            var diffrence = TotalTime - time;
            //if this approaches 0, then 0/(x) -> 0, 
            //if this approaches 10 seconds, and crossfadeDur = 10 seconds, then 10/10 -> 1
            var multiplier = (float)(diffrence.TotalSeconds / _crossfadeDuration.TotalSeconds);
            multiplier = multiplier.Clamp(0, 1);
            return multiplier;
        }

        if (_crossfadingIn)
        {
            var difference = _crossfadeDuration - time;
            var progress = (float)(difference.TotalSeconds / _crossfadeDuration.TotalSeconds);
            //if diff approaches 0, (meaning we have reached it) then this will result in 0/x -> 0
            //so we need to get the complement of this
            var multiplier = progress.Clamp(0, 1);
            multiplier = 1 - multiplier;
            return multiplier;
        }

        return 1;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _mediaPlayer.TimeChanged -= MediaPlayerOnTimeChanged;
        _mediaPlayer.Stopped -= MediaPlayerOnEndReached;
        _trackEnded.OnNext(Unit.Default);
        _mediaPlayer.Dispose();
        _streamMediaInput.Dispose();
        _media.Dispose();
        _time.Dispose();
        _libVlc.Dispose();
    }
}