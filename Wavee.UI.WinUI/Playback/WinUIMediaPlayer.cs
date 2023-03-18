using System;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Storage;
using Wavee.UI.Playback.Player;

namespace Wavee.UI.WinUI.Playback
{
    public class WinUIMediaPlayer : ILocalFilePlayer
    {
        private readonly MediaPlayer _mediaPlayer;

        public WinUIMediaPlayer()
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.AutoPlay = true;
            _mediaPlayer.SeekCompleted += (sender, args) =>
            {
                TimeChanged?.Invoke(this, sender.Position);
            };
            _mediaPlayer.MediaOpened += (sender, args) =>
            {
                var test = sender.Source;
                TrackStarted?.Invoke(this, null);
            };
            _mediaPlayer.MediaEnded += (sender, args) =>
            {
                TrackEnded?.Invoke(this, EventArgs.Empty);
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
        public event EventHandler<string> TrackStarted;
        public event EventHandler TrackEnded;
        public event EventHandler<bool> PauseChanged;
        public event EventHandler<TimeSpan> TimeChanged;

        public void Seek(TimeSpan to)
        {
            _mediaPlayer.Position = to;
        }

        public void Resume() => _mediaPlayer.Play();

        public void Pause() => _mediaPlayer.Pause();

        public async Task LoadTrack(string file)
        {
            var storageFile =
                await StorageFile.GetFileFromPathAsync(file);
            _mediaPlayer.SetFileSource(storageFile);
        }

        public TimeSpan Position => _mediaPlayer.Position;
    }

}
