using System;
using System.IO;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.UI.Xaml;
using Wavee.Player;
using Wavee.Player.Context;
using Wavee.Player.Playback;

namespace Wavee.UI.WinUI.PlaybackSample
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            await WaveeCore.Player.Command(new PlayContextCommand(new DummyContext(), Option<int>.None, Option<TimeSpan>.None,
                Option<bool>.None));
        }

        private Window m_window;
    }

    public sealed class DummyContext : IPlayContext
    {
        public ValueTask<(IPlaybackStream Stream, int AbsoluteIndex)> GetStreamAt(Either<Shuffle, Option<int>> at)
        {
            const string dummyFile = "C:\\Users\\ckara\\Music\\ifeelyou.mp3";
            return new ValueTask<(IPlaybackStream Stream, int AbsoluteIndex)>((new FileStreamMaskedAsPlaybackStream(dummyFile), 0));
        }

        public ValueTask<Option<int>> Count()
        {
            return new ValueTask<Option<int>>(1);
        }
    }

    public class FileStreamMaskedAsPlaybackStream : IPlaybackStream
    {
        private readonly FileStream _fileStream;

        public FileStreamMaskedAsPlaybackStream(string filePath)
        {
            _fileStream = File.Open(filePath, FileMode.Open);
        }

        public IPlaybackItem Item => default;
        public Stream AsStream()
        {
            return _fileStream;
        }
    }
}