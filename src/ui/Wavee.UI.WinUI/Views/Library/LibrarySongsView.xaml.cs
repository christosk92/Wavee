using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibrarySongsView : UserControl
{
    public LibrarySongsView()
    {
        ViewModel = new LibrarySongsViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public LibrarySongsViewModel<WaveeUIRuntime> ViewModel { get; }

    private void MainLv_OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    {
        if (args.Item is LibraryTrack lt)
        {
            lt.OriginalIndex = args.ItemIndex;
        }
    }

    public string FormatTrackCountStr(int i)
    {
        var pluralModifier = i == 1 ? "" : "s";
        return $"{i} track{pluralModifier}";
    }

    public string FormatTrackDuration(ReadOnlyObservableCollection<LibraryTrack> readOnlyObservableCollection)
    {
        //12 hours, 36 minutes and 31 seconds
        var totalDuration = readOnlyObservableCollection.Sum(x => x.Track.Duration.TotalMilliseconds);
        var totalDurationTimeSpan = TimeSpan.FromMilliseconds(totalDuration);
        var hours = totalDurationTimeSpan.Hours;
        var minutes = totalDurationTimeSpan.Minutes;
        var seconds = totalDurationTimeSpan.Seconds;
        var hoursStr = hours > 0 ? $"{hours} hour{(hours == 1 ? "" : "s")}, " : "";
        var minutesStr = minutes > 0 ? $"{minutes} minute{(minutes == 1 ? "" : "s")} and " : "";
        var secondsStr = seconds > 0 ? $"{seconds} second{(seconds == 1 ? "" : "s")}" : "";

        return $"{hoursStr}{minutesStr}{secondsStr}";
    }

    private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        ViewModel.SearchText = sender.Text;
    }
}