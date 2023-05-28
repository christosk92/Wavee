using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.Core.Ids;
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
            if (args.IsContainerPrepared)
            {
                args.ItemContainer.IsEnabled = lt.Track.CanPlay;
            }
        }
    }
    private void MainLv_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Item is LibraryTrack lt)
        {
            lt.OriginalIndex = args.ItemIndex;
            if (args.ItemContainer is not null)
            {
                args.ItemContainer.IsEnabled = lt.Track.CanPlay;
            }
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

    public LibraryTrackSortType NextSortType(LibraryTrackSortType currentLibraryTrackSortType, string buttonType)
    {
        //sorting goes from asc -> desc -> default (date)
        switch (buttonType)
        {
            case "title":
                //if currentTrack is title asc -> title desc.
                //if title desc -> default
                //if somethin else, -> title asc
                return currentLibraryTrackSortType switch
                {
                    LibraryTrackSortType.Title_Asc => LibraryTrackSortType.Title_Desc,
                    LibraryTrackSortType.Title_Desc => LibraryTrackSortType.Added_Desc,
                    _ => LibraryTrackSortType.Title_Asc,
                };
                break;
            case "artists":
                return currentLibraryTrackSortType switch
                {
                    LibraryTrackSortType.Artist_Asc => LibraryTrackSortType.Artist_Desc,
                    LibraryTrackSortType.Artist_Desc => LibraryTrackSortType.Added_Desc,
                    _ => LibraryTrackSortType.Artist_Asc,
                };
                break;
            case "album":
                return currentLibraryTrackSortType switch
                {
                    LibraryTrackSortType.Album_Asc => LibraryTrackSortType.Album_Desc,
                    LibraryTrackSortType.Album_Desc => LibraryTrackSortType.Added_Desc,
                    _ => LibraryTrackSortType.Album_Asc,
                };
                break;
            case "date":
                //BAM! Default sort
                return currentLibraryTrackSortType switch
                {
                    LibraryTrackSortType.Added_Asc => LibraryTrackSortType.Added_Desc,
                    LibraryTrackSortType.Added_Desc => LibraryTrackSortType.Added_Asc,
                    _ => LibraryTrackSortType.Added_Desc,
                };
                break;
        }

        return LibraryTrackSortType.Added_Desc;
    }

    private async void SortButtonTapp(object sender, TappedRoutedEventArgs e)
    {
        //set scrollviewer to top
        await Task.Delay(10);
        MainLv.ScrollIntoView(MainLv.Items[0]);
    }

    private void NavigateToMetadata(object sender, TappedRoutedEventArgs e)
    {
        var hyperlnk = (HyperlinkButton)sender;
        var tg = (AudioId)hyperlnk.Tag;

        UICommands.NavigateTo.Execute(tg);
    }

}