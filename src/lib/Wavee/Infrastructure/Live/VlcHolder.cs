using LibVLCSharp.Shared;

namespace Wavee.Infrastructure.Live;

internal sealed class VlcHolder
{
    private readonly LibVLC _libVlc;

    private readonly AtomSeq<CancellationTokenSource> _cancellationTokens =
        LanguageExt.AtomSeq<CancellationTokenSource>.Empty;

    private readonly AtomSeq<MediaPlayer> _mediaPlayer =
        LanguageExt.AtomSeq<MediaPlayer>.Empty;

    private readonly AtomSeq<MediaInput> _mediaInput =
        LanguageExt.AtomSeq<MediaInput>.Empty;

    private readonly AtomSeq<Media> _media =
        LanguageExt.AtomSeq<Media>.Empty;

    public VlcHolder()
    {
        _libVlc = new LibVLC();
    }

    public void Resume()
    {
        _mediaPlayer.LastOrNone().IfSome(x => x.Play());
    }

    public void Pause()
    {
        _mediaPlayer.LastOrNone().IfSome(x => x.Pause());
    }

    public void Seek(TimeSpan position)
    {
        _mediaPlayer.LastOrNone().IfSome(x => x.Time = (long)position.TotalMilliseconds);
    }

    public Option<TimeSpan> Position => _mediaPlayer.LastOrNone().Map(x => TimeSpan.FromMilliseconds(x.Time));

    public void SetStream(Stream stream,
        double volumeFrac,
        bool closeOtherStreams, Action onEnded)
    {
        if (closeOtherStreams)
        {
            //just end all the streams so we can start a new one
            _cancellationTokens.Iter(x => x.Cancel());
            //the media player will be removed from the atomseqs when it ends as per bottom fnc.
        }

        var cts = new CancellationTokenSource();
        //create a new media input and media player
        var mediaInput = new StreamMediaInput(stream);
        var media = new Media(_libVlc, mediaInput);
        var mediaPlayer = new MediaPlayer(media);
        Task.Factory.StartNew(async () =>
        {
            try
            {
                bool initial = true;
                while (true)
                {
                    await Task.Delay(100, cts.Token);
                    if (mediaPlayer.State is VLCState.Ended
                        or VLCState.Stopped or VLCState.Error)
                    {
                        break;
                    }

                    if ((media.State is VLCState.Playing or VLCState.Paused)
                        && initial)
                    {
                        mediaPlayer.Volume = (int)(volumeFrac * 100);
                        initial = false;
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                onEnded();
                atomic(() =>
                {
                    _mediaInput.Swap(f => f.Filter(x => x != mediaInput));
                    _media.Swap(f => f.Filter(x => x != media));
                    _mediaPlayer.Swap(f => f.Filter(x => x != mediaPlayer));
                    _cancellationTokens.Swap(f => f.Filter(x => x != cts));
                });
                try
                {
                    cts.Dispose();
                }
                catch (Exception)
                {
                }

                mediaPlayer.Dispose();
                media.Dispose();
                mediaInput.Dispose();
            }
        }, cts.Token);

        mediaPlayer.Play();
        //add to the atomseqs
        atomic(() =>
        {
            _cancellationTokens.Swap(f => f.Add(cts));
            _mediaInput.Swap(f => f.Add(mediaInput));
            _media.Swap(f => f.Add(media));
            _mediaPlayer.Swap(f => f.Add(mediaPlayer));
        });
    }

    public void Stop()
    {
        _mediaPlayer.LastOrNone().IfSome(x => x.Stop());
        _cancellationTokens.LastOrNone().IfSome(x => x.Cancel());
    }

    public void SetVolume(double volumeFrac)
    {
        _mediaPlayer.LastOrNone().IfSome(x => x.Volume = (int)(volumeFrac * 100));
    }
}