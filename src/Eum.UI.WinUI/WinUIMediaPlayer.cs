using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Effects;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using CommunityToolkit.WinUI;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using WinRTLibrary;

namespace Eum.UI.WinUI
{
    public class VorbisHolder : IDisposable
    {
        public MediaPlayerElement MediaPlayerElement { get; init; }
        public MediaSource Source { get; set; }
        public MediaPlayer Player => MediaPlayerElement.MediaPlayer;
        public PropertySet Properties { get; set; }
        public AbsChunkedInputStream stream { get; set; }
        public IRandomAccessStream r { get; set; }

        public CancellationTokenSource _CancellationToken;

        public void Dispose()
        {
            Source?.Dispose();
            Player?.Dispose();
            Properties?.Clear();
        }
    }
    public class WinUIMediaPlayer : IAudioPlayer
    {
        private ConcurrentDictionary<string, VorbisHolder> _holders = new ConcurrentDictionary<string, VorbisHolder>();
        private DispatcherQueue dispatcherQueue;

        public WinUIMediaPlayer()
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }
        public ValueTask Pause(string playbackId, bool releaseResources)
        {
            if (_holders.TryGetValue(playbackId, out var item))
            {
                item.Player.Pause();
            }

            return new ValueTask();
        }

        public ValueTask Resume(string playbackId)
        {
            if (_holders.TryGetValue(playbackId, out var item))
            {
                item.Player.Play();
            }

            return new ValueTask();
        }

        public async ValueTask InitStream(SuperAudioFormat codec, AbsChunkedInputStream audioStreamStream, float normalizationFactor,
            int duration, string playbackId, long playFrom)
        {
            try
            {
                MediaPlayer mp = default;
                await dispatcherQueue.EnqueueAsync(async () =>
                {
                    var r = audioStreamStream.AsRandomAccessStream();
                    var m = MediaSource.CreateFromStream(r, "audio/ogg");
                  //  var m = MediaSource.CreateFromStorageFile(await StorageFile.GetFileFromPathAsync(Path.Combine(ApplicationData.Current.LocalFolder.Path, "test.mp3")));
                      m.CustomProperties.Add("stream", audioStreamStream);
                    var player = new MediaPlayerElement();
                    mp = new MediaPlayer();
                    player.SetMediaPlayer(mp);
                    // player.Source = m;


                    var item = new Windows.Media.Playback.MediaPlaybackItem(m!);
                    player.MediaPlayer.Source = item;
                    
                    player.MediaPlayer.PlaybackSession.Position = TimeSpan.FromMilliseconds(playFrom);

                    var echoProperties = new PropertySet
                    {
                        {"Gain", 1f}
                    };
                    var newHolder = new VorbisHolder
                    {
                        _CancellationToken = new CancellationTokenSource(),
                        MediaPlayerElement = player,
                        Source = m,
                        Properties = echoProperties,
                        stream = audioStreamStream,
                      //  r = r,
                    };
                    player.MediaPlayer.MediaFailed += NewMediaPlayerOnMediaFailed;
                    // var echoEffectDefinition = new AudioEffectDefinition(typeof(GainAudioEffect).FullName, echoProperties);
                    // newMediaPlayer.AddAudioEffect(typeof(GainAudioEffect).FullName, false, echoProperties);
                    //
                    //  newMediaPlayer.MediaFailed += (sender, args) =>
                    //  {
                    //      var t = args.ExtendedErrorCode;
                    //      var j = args.Error;
                    //      var k = args.ErrorMessage;
                    //  };
                    _holders[playbackId] = newHolder;
                    //   player.MediaPlayer.CurrentStateChanged += MediaPlayerOnCurrentStateChanged;

                    player.MediaPlayer.Play();

                }, DispatcherQueuePriority.High);
                // wait here until playback stops or should stop
                Task.Run(async () =>
                {

                    while (mp.CurrentState != MediaPlayerState.Stopped &&
                           !_holders[playbackId]._CancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(100, _holders[playbackId]._CancellationToken.Token);
                            TimeChanged?.Invoke(this,
                                (playbackId,
                                    (int)mp.PlaybackSession.Position.TotalMilliseconds));
                        }
                        catch (Exception x)
                        {
                            S_Log.Instance.LogError(x);
                            if (_holders[playbackId]._CancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                        }
                    }

                    TrackFinished?.Invoke(this, playbackId);
                });
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                throw;
            }
        }

        // private void MediaPlayerOnCurrentStateChanged(MediaPlayer sender, object args)
        // {
        //     var getPlayer = _holders.FirstOrDefault(a => a.Value.Player == sender);
        //     if (getPlayer.Key != null)
        //     {
        //         switch (getPlayer.Value.Player.CurrentState)
        //         {
        //             case MediaPlayerState.Closed:
        //                 break;
        //             case MediaPlayerState.Opening:
        //                 break;
        //             case MediaPlayerState.Buffering:
        //                 break;
        //             case MediaPlayerState.Playing:
        //                 break;
        //             case MediaPlayerState.Paused:
        //                 break;
        //             case MediaPlayerState.Stopped:
        //                 break;
        //             default:
        //                 throw new ArgumentOutOfRangeException();
        //         }
        //     }
        // }

        private void NewMediaPlayerOnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            S_Log.Instance.LogError(args.ExtendedErrorCode);
            S_Log.Instance.LogError(args.ErrorMessage);
            S_Log.Instance.LogError(args.Error.ToString());
        }

        public async ValueTask<int> Time(string playbackId)
        {
            if (_holders.TryGetValue(playbackId, out var item))
            {
               return await dispatcherQueue.EnqueueAsync(() => (int) item.Player.PlaybackSession.Position.TotalMilliseconds);
            }

            return -1;
        }

        public void Dispose(string playbackId)
        {
            if (_holders.TryRemove(playbackId, out var item))
            {
                item.Player.MediaFailed -= NewMediaPlayerOnMediaFailed;
                item.Dispose();
                item._CancellationToken.Cancel();
                item._CancellationToken.Dispose();
                item.stream.Dispose();
                item.r.Dispose();

            }
        }

        public void Seek(string playbackId, int posInMs)
        {
            if (_holders.TryGetValue(playbackId, out var item))
            {
                item.Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(posInMs);
            }
        }

        public event EventHandler<(string playbackId, int Time)> TimeChanged;
        public event EventHandler<string> TrackFinished;
        public void Gain(string playbackId, float getGain)
        {
            //TODO
            if (_holders.TryGetValue(playbackId, out var state))
            {
                state.Properties["Gain"] = getGain;
            }
        }

        public event EventHandler<(string PlaybackId, PlaybackStateType)> StateChanged;
        public void SetVolume(string playbackid, float volume)
        {
            if (_holders.TryGetValue(playbackid, out var state))
            {
                state.Player.Volume = volume;
            }
        }

        private bool _adjustingGain;
    }
}
