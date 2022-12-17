using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Eum.Connections.Spotify.Playback.Audio.Streams;
using Eum.Connections.Spotify.Playback.Enums;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Logging;
using WinRTLibrary;

namespace Eum.UI.WinUI
{
    public class VorbisHolder : IDisposable
    {
        public MediaSource Source { get; set; }
        public MediaPlayer Player { get; set; }
        public PropertySet Properties { get; set; }

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

        public ValueTask InitStream(SuperAudioFormat codec, AbsChunkedInputStream audioStreamStream, float normalizationFactor,
            int duration, string playbackId, long playFrom)
        {
            try
            {
                var m = MediaSource.CreateFromStream(audioStreamStream.AsRandomAccessStream(),
                    "audio/ogg");
                m.CustomProperties.Add("stream", audioStreamStream);

                var newMediaPlayer = new MediaPlayer();
                newMediaPlayer.Source = m;

                newMediaPlayer.PlaybackSession.Position = TimeSpan.FromMilliseconds(playFrom);

                var echoProperties = new PropertySet
                {
                    {"Gain", 1f}
                };
                var newHolder = new VorbisHolder
                {
                    _CancellationToken = new CancellationTokenSource(),
                    Player = newMediaPlayer,
                    Source = m,
                    Properties = echoProperties,
                };
                // var echoEffectDefinition = new AudioEffectDefinition(typeof(GainAudioEffect).FullName, echoProperties);
                newMediaPlayer.AddAudioEffect(typeof(GainAudioEffect).FullName, false, echoProperties);

                newMediaPlayer.MediaFailed += (sender, args) =>
                {
                    var t = args.ExtendedErrorCode;
                    var j = args.Error;
                    var k = args.ErrorMessage;
                };
                _holders[playbackId] = newHolder;
                newMediaPlayer.Play();

                // wait here until playback stops or should stop
                Task.Run(async () =>
                {
                    while (newMediaPlayer.CurrentState != MediaPlayerState.Stopped &&
                           !newHolder._CancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(100, newHolder._CancellationToken.Token);
                            TimeChanged?.Invoke(this,
                                (playbackId, (int) newMediaPlayer.PlaybackSession.Position.TotalMilliseconds));
                        }
                        catch (Exception x)
                        {
                            S_Log.Instance.LogError(x);
                            if (newHolder._CancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                        }
                    }

                    TrackFinished?.Invoke(this, playbackId);
                });
                return new ValueTask();
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                throw;
            }
        }

        public int Time(string playbackId)
        {
            if (_holders.TryGetValue(playbackId, out var item))
            {
                return (int)item.Player.PlaybackSession.Position.TotalMilliseconds;
            }

            return -1;
        }

        public void Dispose(string playbackId)
        {
            if (_holders.TryRemove(playbackId, out var item))
            {
                item.Dispose();
                item._CancellationToken.Cancel();
                item._CancellationToken.Dispose();
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
    }
}
