using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Wavee.UI.Playback;
using Wavee.UI.Playback.Player;
using System.Security.Cryptography;

namespace Wavee.UI.WinUI.Playback
{
    public class WinUIMediaPlayer : IMusicPlayer
    {
        private readonly MediaPlayer _mediaPlayer;
        private MediaSource? _previousTrack;
        public WinUIMediaPlayer()
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.AutoPlay = false;
            _mediaPlayer.SeekCompleted += (sender, args) =>
            {
                TimeChanged?.Invoke(this, sender.Position);
            };
            _mediaPlayer.MediaOpened += (sender, args) =>
            {
                var newSource = sender.Source;
                if (newSource is MediaSource mediaSource)
                {
                    if (_previousTrack != null)
                    {
                        var oldId = _previousTrack.CustomProperties["id"].ToString();
                        var sw = (Stopwatch)_previousTrack.CustomProperties["time"];
                        var startedAt = (DateTime)_previousTrack.CustomProperties["date"];
                        sw.Stop();
                        TrackStarted?.Invoke(this, new PreviousTrackData(oldId, startedAt, sw.Elapsed));
                        _previousTrack?.Dispose();
                    }
                    else
                    {
                        TrackStarted?.Invoke(this, null);
                    }

                    _previousTrack = mediaSource;
                }
                //  TrackStarted?.Invoke(this, null);
            };
            _mediaPlayer.MediaEnded += (sender, args) =>
            {
                TrackEnded?.Invoke(this, EventArgs.Empty);
            };
            _mediaPlayer.VolumeChanged += (sender, args) =>
            {
                VolumeChanged?.Invoke(this, _mediaPlayer.Volume);
            };
            _mediaPlayer.CurrentStateChanged += (sender, args) =>
            {
                switch (sender.CurrentState)
                {
                    case MediaPlayerState.Closed:
                        break;
                    case MediaPlayerState.Opening:
                        break;
                    case MediaPlayerState.Buffering:
                        break;
                    case MediaPlayerState.Playing:
                        PauseChanged?.Invoke(this, false);
                        break;
                    case MediaPlayerState.Paused:
                        PauseChanged?.Invoke(this, true);
                        break;
                    case MediaPlayerState.Stopped:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }
        public event EventHandler<PreviousTrackData?> TrackStarted;
        public event EventHandler TrackEnded;
        public event EventHandler<bool> PauseChanged;
        public event EventHandler<TimeSpan> TimeChanged;
        public event EventHandler<double>? VolumeChanged;

        public void Seek(TimeSpan to)
        {
            _mediaPlayer.Position = to;
        }

        public void Resume()
        {
            _mediaPlayer.Play();
            if (_mediaPlayer.Source is MediaSource s)
            {
                var time = (Stopwatch)s.CustomProperties["time"];
                time.Start();
                s.CustomProperties["time"] = time;
            }
        }

        public void Pause()
        {
            _mediaPlayer.Pause();
            if (_mediaPlayer.Source is MediaSource s)
            {
                var time = (Stopwatch)s.CustomProperties["time"];
                time.Stop();
                s.CustomProperties["time"] = time;
            }
        }

        public async Task LoadTrack(string file)
        {
            _mediaPlayer.Pause();
            var storageFile =
                await StorageFile.GetFileFromPathAsync(file);
            var source = MediaSource.CreateFromStorageFile(storageFile);
            source.CustomProperties.Add("id", file);
            var sw = Stopwatch.StartNew();
            source.CustomProperties.Add("time", sw);
            source.CustomProperties.Add("date", DateTime.UtcNow);
            _mediaPlayer.Source = source;
        }

        public TimeSpan Position => _mediaPlayer.Position;
        public bool Paused => _mediaPlayer.CurrentState == MediaPlayerState.Paused;
        public double Volume => _mediaPlayer.Volume;
        public void SetVolume(double d)
        {
            _mediaPlayer.Volume = d;
        }

    }

}
