using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Home;

public sealed class HomeViewModel : ObservableObject
{
    private static readonly IReadOnlyList<ShimmerGroup> _shimmerItems;
    private readonly UserViewModel _user;
    private string? _greeting;
    private bool _loading;
    private string[] _filters;
    private string? _selectedFilter;

    static HomeViewModel()
    {
        _shimmerItems = Enumerable.Range(0, 10)
            .Select(i => new ShimmerGroup(Enumerable.Range(0, 10).Select(j => new ShimmerItem()).ToList())).ToList();
    }
    public HomeViewModel(UserViewModel user)
    {
        _user = user;
    }
    public IReadOnlyList<ShimmerGroup> ShimmerItems => _shimmerItems;

    public bool Loading
    {
        get => _loading;
        set => SetProperty(ref _loading, value);
    }
    public string? Greeting
    {
        get => _greeting;
        set => SetProperty(ref _greeting, value);
    }

    public string[] Filters
    {
        get => _filters;
        set => SetProperty(ref _filters, value);
    }

    public string? SelectedFilter
    {
        get => _selectedFilter;
        set => SetProperty(ref _selectedFilter, value);
    }
    public ObservableCollection<HomeGroupSectionViewModel> Sections { get; } = new();

    public async Task Fetch(CancellationToken ct = default)
    {
        try
        {
            Loading = true;
            Sections.Clear();
            var home = await _user.Client.Home.GetHome(SelectedFilter, ct);

            Greeting = home.Greeting;
            Filters = home.Filters;
            foreach (var item in home.Sections)
            {
                Sections.Add(item);
            }

            var oldFilter = SelectedFilter;
            SelectedFilter = string.Empty;
            SelectedFilter = oldFilter;

            Loading = false;
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Failed to fetch home view");
        }
        finally
        {
            Loading = false;
        }
    }
}
public class ShimmerGroup
{
    public ShimmerGroup(IReadOnlyList<ShimmerItem> items)
    {
        Items = items;
    }

    public IReadOnlyList<ShimmerItem> Items { get; }
}

public class ShimmerItem { }