using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Text;
using System.Threading;
using CommunityToolkit.WinUI.Controls;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Playlists.ViewModel;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Views.Playlist;

public sealed partial class PlaylistPage : Page, INavigeablePage<PlaylistViewModel>
{
    public PlaylistPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is PlaylistViewModel vm)
        {
            DataContext = vm;
            UpdateBindings();
            vm.Initialize(CancellationToken.None);
        }
    }

    public PlaylistViewModel ViewModel
    {
        get
        {
            try
            {
                return DataContext is PlaylistViewModel vm ? vm : null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }

    public void UpdateBindings()
    {
        // this.Bindings.Update();
    }

    public string FormatPopCount(ulong? @ulong)
    {
        //for example 1000 -> 1,000 
        if (@ulong is null)
        {
            return "--";
        }
        return @ulong.Value.ToString("N0");
    }

    public string FormatTime(TimeSpan? timeSpan)
    {
        if (timeSpan is null)
        {
            return "--";
        }

        // 01:24:30 -> 1 hr 24 min 30 sec
        var sb = new StringBuilder();
        if (timeSpan.Value.Hours > 0)
        {
            sb.Append($"{timeSpan.Value.Hours} hr ");
        }

        if (timeSpan.Value.Minutes > 0)
        {
            sb.Append($"{timeSpan.Value.Minutes} min ");
        }

        if (timeSpan.Value.Seconds > 0)
        {
            sb.Append($"{timeSpan.Value.Seconds} sec");
        }

        return sb.ToString();
    }

    public bool Negate(bool b)
    {
        return !b;
    }

    private async void TokenBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.GeneralSearchTerm = sender.Text;

            await ViewModel.RefreshTracks();
        }
    }

    private async void TokenBox_OnTokenItemAdded(TokenizingTextBox sender, object args)
    {
        if (args is string x)
        {
            ViewModel.SearchTerms.Add(x);
            await ViewModel.RefreshTracks();
        }
    }

    private async void TokenBox_OnTokenItemRemoved(TokenizingTextBox sender, object args)
    {
        if (args is string x)
        {
            ViewModel.SearchTerms.Remove(x);
            if (ViewModel.SearchTerms.Count is 0)
            {
                ViewModel.GeneralSearchTerm = TokenBox.Text;
            }
            await ViewModel.RefreshTracks();
        }
    }
}