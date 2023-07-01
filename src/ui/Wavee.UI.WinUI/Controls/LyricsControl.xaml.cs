using System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.ViewModel.Shell.Lyrics;
using ListViewExtensions = Eum.UI.WinUI.ListViewExtensions;

namespace Wavee.UI.WinUI.Controls;

public sealed partial class LyricsControl : UserControl
{
    private readonly DispatcherQueue _dispatcherQueue;
    public LyricsControl()
    {
        this.InitializeComponent();
        _dispatcherQueue = this.DispatcherQueue;
        var baseVm = ShellViewModel.Instance;

        ViewModel = new LyricsViewModel(
            lyricsProvider: baseVm.User.Client.Lyrics,
            playbackViewModel: baseVm.Playback,
            _invokeOnUiThread: InvokeOnUiThread
        );
    }

    private void InvokeOnUiThread(Action obj)
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            obj();
        });
    }

    public LyricsViewModel ViewModel { get; }


    private async void Lyrics_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (Lyrics.SelectedItem != null)
        {
            int iteration = 0;
            while (true)
            {
                try
                {
                    await ListViewExtensions.SmoothScrollIntoViewWithItemAsync(Lyrics, Lyrics.SelectedItem, ScrollItemPlacement.Top, false, true, 0, 0);
                    break;
                }
                catch (Exception x)
                {
                    await Task.Delay(10);
                    if (iteration > 10)
                        break;
                    iteration++;
                }
            }
        }
    }
    public bool Negate(bool b)
    {
        return !b;
    }
}