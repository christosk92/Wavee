using System;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Text;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.WinUI.Contracts;


namespace Wavee.UI.WinUI.Views.Libraries;

public sealed partial class SongLibraryPage : Page, INavigeablePage<LibrarySongsViewModel>
{
    public SongLibraryPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is LibrarySongsViewModel vm)
        {
            DataContext = vm;
            await vm.RefreshTracks();
        }
    }

    public void UpdateBindings()
    {
        this.Bindings.Update();
    }

    public LibrarySongsViewModel ViewModel => DataContext is LibrarySongsViewModel vm ? vm : null;
    public string FormatTime(TimeSpan? timeSpan)
    {
        if (timeSpan is null)
        {
            return "--";
        }

        // 01:24:30 -> 1 hr 24 min 30 sec
        var sb = new StringBuilder();

        if (timeSpan.Value.TotalHours >= 1)
        {
            sb.Append($"{(int)timeSpan.Value.TotalHours} hr ");
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