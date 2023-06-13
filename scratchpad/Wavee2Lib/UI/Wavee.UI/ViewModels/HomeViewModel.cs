using Eum.Spotify.playlist4;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Wavee.Core.Ids;
using Wavee.UI.Client;
using Wavee.UI.Models.Common;
using Wavee.UI.Models.Home;

namespace Wavee.UI.ViewModels;

public sealed class HomeViewModel : ReactiveObject
{
    private static readonly ObservableCollection<HomeGroup> FakeShimmerData = new(
        Enumerable.Range(0, 4)
            .Select(i => new HomeGroup
            {
                Id = i.ToString(),
                Items = Enumerable.Range(0,
                        10)
                    .Select(f => new CardViewItem
                    {
                        Id = new AudioId(),
                        Title = ""
                    }).ToArray(),
                Title = ""
            }));

    private bool _isLoading;

    public HomeViewModel()
    {
        _isLoading = true;
        Items = new (FakeShimmerData);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ObservableCollection<HomeGroup> Items { get; set; }

    public void ClearData()
    {
        Items = null;
    }

    public Task FetchSongsOnly()
    {
        IsLoading = true;
        Items.Clear();
        foreach (var group in FakeShimmerData)
        {
            Items.Add(group);
        }
        return Task.CompletedTask;
    }

    public async Task FetchAll()
    {
        IsLoading = true;
        Items.Clear();
        foreach (var group in FakeShimmerData)
        {
            Items.Add(group);
        }

        var aff = await SpotifyView.GetHomeView(CancellationToken.None).Run();
        var groupResults = aff.Match(
            Succ: homeView => homeView,
            Fail: ex =>
            {
                Debug.WriteLine(ex);
                return new List<HomeGroup>();
            });
        Items.Clear();
        foreach (var group in groupResults)
        {
            Items.Add(group);
        }
        // Items = new ObservableCollection<HomeGroup>(groupResults);
        IsLoading = false;
    }
}