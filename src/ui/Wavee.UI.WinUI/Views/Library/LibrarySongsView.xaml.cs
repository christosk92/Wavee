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

    public TrackSortType NextSortType(TrackSortType currentTrackSortType, string buttonType)
    {
        //sorting goes from asc -> desc -> default (date)
        switch (buttonType)
        {
            case "title":
                //if currentTrack is title asc -> title desc.
                //if title desc -> default
                //if somethin else, -> title asc
                return currentTrackSortType switch
                {
                    TrackSortType.Title_Asc => TrackSortType.Title_Desc,
                    TrackSortType.Title_Desc => TrackSortType.OriginalIndex_Asc,
                    _ => TrackSortType.Title_Asc,
                };
                break;
            case "artists":
                return currentTrackSortType switch
                {
                    TrackSortType.Artist_Asc => TrackSortType.Artist_Desc,
                    TrackSortType.Artist_Desc => TrackSortType.OriginalIndex_Asc,
                    _ => TrackSortType.Artist_Asc,
                };
                break;
            case "album":
                return currentTrackSortType switch
                {
                    TrackSortType.Album_Asc => TrackSortType.Album_Desc,
                    TrackSortType.Album_Desc => TrackSortType.OriginalIndex_Asc,
                    _ => TrackSortType.Album_Asc,
                };
                break;
            case "date":
                //BAM! Default sort
                return currentTrackSortType switch
                {
                    TrackSortType.OriginalIndex_Asc => TrackSortType.OriginalIndex_Desc,
                    TrackSortType.OriginalIndex_Desc => TrackSortType.OriginalIndex_Asc,
                    _ => TrackSortType.OriginalIndex_Asc,
                };
                break;
        }

        return TrackSortType.OriginalIndex_Asc;
    }
}